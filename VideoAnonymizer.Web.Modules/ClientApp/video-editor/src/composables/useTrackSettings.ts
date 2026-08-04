import type { ComputedRef } from 'vue';
import type {
    AnonymizationSettings,
    AnalyzedFrameDto,
    DetectedObjectDto,
    VideoEditorProps
} from '../types';
import { getTrackOccurrences, buildAllSegments, findSegment } from './useConsecutiveTrackSegment';
import type { ConsecutiveSegment } from './useConsecutiveTrackSegment';
import { getChangedObjects } from '../utils/objectDiff';

function cloneObjects(objects: DetectedObjectDto[]): DetectedObjectDto[] {
    return JSON.parse(JSON.stringify(objects));
}

export type TrackSettings = {
    shape: string | null;
    blurSizePercentOverride: number | null;
};

export type SegmentBufferValues = {
    pre: number;
    preIsCustom: boolean;
    post: number;
    postIsCustom: boolean;
};

export type TrackTimeBufferState = {
    effective: number | null;
    isMixed: boolean;
};

export function useTrackSettings(
    state: VideoEditorProps,
    frames: ComputedRef<AnalyzedFrameDto[]>,
    anonymizationSettings: ComputedRef<AnonymizationSettings>
) {
    function getTrackSettings(trackId: number): TrackSettings {
        const occurrences = getTrackOccurrences(frames.value, trackId);
        return {
            shape: occurrences.find(obj => obj.blurShape)?.blurShape ?? null,
            blurSizePercentOverride: occurrences.find(obj => obj.blurSizePercentOverride != null)?.blurSizePercentOverride ?? null
        };
    }

    function getSegmentBufferValues(segment: ConsecutiveSegment): SegmentBufferValues {
        const global = anonymizationSettings.value.timeBufferMs;
        const preOverride = segment.first.preBufferMsOverride;
        const postOverride = segment.last.postBufferMsOverride;
        return {
            pre: preOverride ?? global,
            preIsCustom: preOverride != null,
            post: postOverride ?? global,
            postIsCustom: postOverride != null
        };
    }

    function applyTrackBlurShape(trackId: number, shape: string) {
        const occurrences = getTrackOccurrences(frames.value, trackId);
        if (occurrences.length === 0) return;

        const before = cloneObjects(occurrences);
        occurrences.forEach(obj => { obj.blurShape = shape; });
        state.onDetectedObjectsBulkUpdated?.(state.videoId, occurrences, 'track-settings', before);
    }

    function applyTrackBlurSize(trackId: number, percent: number) {
        const occurrences = getTrackOccurrences(frames.value, trackId);
        if (occurrences.length === 0) return;

        const before = cloneObjects(occurrences);
        const normalized = percent === anonymizationSettings.value.blurSizePercent ? null : percent;
        occurrences.forEach(obj => { obj.blurSizePercentOverride = normalized; });
        state.onDetectedObjectsBulkUpdated?.(state.videoId, occurrences, 'track-settings', before);
    }

    function resetTrackBlurSize(trackId: number) {
        const occurrences = getTrackOccurrences(frames.value, trackId);
        if (occurrences.length === 0) return;

        const before = cloneObjects(occurrences);
        occurrences.forEach(obj => { obj.blurSizePercentOverride = null; });
        state.onDetectedObjectsBulkUpdated?.(state.videoId, occurrences, 'track-settings', before);
    }

    function applyOccurrenceBlurSize(obj: DetectedObjectDto, percent: number) {
        const before = cloneObjects([obj]);
        const trackBlur = getTrackSettings(obj.trackId ?? -1).blurSizePercentOverride;
        const parent = trackBlur ?? anonymizationSettings.value.blurSizePercent;
        obj.occurrenceBlurSizePercentOverride = percent === parent ? null : percent;
        state.onDetectedObjectUpdated?.(state.videoId, obj.analyzedFrameId, obj, 'occurrence-blur', before);
    }

    function resetOccurrenceBlurSize(obj: DetectedObjectDto) {
        const before = cloneObjects([obj]);
        obj.occurrenceBlurSizePercentOverride = null;
        state.onDetectedObjectUpdated?.(state.videoId, obj.analyzedFrameId, obj, 'occurrence-blur', before);
    }

    function applySegmentPre(segment: ConsecutiveSegment, valueMs: number) {
        const normalized = valueMs === anonymizationSettings.value.timeBufferMs ? null : valueMs;
        const first = segment.first;
        const before = cloneObjects([first]);
        first.preBufferMsOverride = normalized;
        state.onDetectedObjectUpdated?.(state.videoId, first.analyzedFrameId, first, 'pre-buffer', before);
    }

    function applySegmentPost(segment: ConsecutiveSegment, valueMs: number) {
        const normalized = valueMs === anonymizationSettings.value.timeBufferMs ? null : valueMs;
        const last = segment.last;
        const before = cloneObjects([last]);
        last.postBufferMsOverride = normalized;
        state.onDetectedObjectUpdated?.(state.videoId, last.analyzedFrameId, last, 'post-buffer', before);
    }

    function resetSegmentPre(segment: ConsecutiveSegment) {
        const first = segment.first;
        const before = cloneObjects([first]);
        first.preBufferMsOverride = null;
        state.onDetectedObjectUpdated?.(state.videoId, first.analyzedFrameId, first, 'pre-buffer', before);
    }

    function resetSegmentPost(segment: ConsecutiveSegment) {
        const last = segment.last;
        const before = cloneObjects([last]);
        last.postBufferMsOverride = null;
        state.onDetectedObjectUpdated?.(state.videoId, last.analyzedFrameId, last, 'post-buffer', before);
    }

    /**
     * All consecutive segments belonging to a track, ordered by analyzed frame index.
     * An untracked occurrence is resolved as its own one-object segment.
     */
    function getScopeSegments(scope: { trackId: number | null; object: DetectedObjectDto }): ConsecutiveSegment[] {
        if (scope.trackId != null) {
            return buildAllSegments(frames.value).filter(segment => segment.first.trackId === scope.trackId);
        }
        const segment = findSegment(buildAllSegments(frames.value), scope.object);
        return segment ? [segment] : [];
    }

    /**
     * The track Time buffer control reports a common effective value only when every
     * current segment boundary (pre and post of every segment) matches; otherwise it
     * shows a Mixed state.
     */
    function getTimeBufferState(segments: ConsecutiveSegment[]): TrackTimeBufferState {
        const boundaryValues: number[] = [];
        for (const segment of segments) {
            const buffers = getSegmentBufferValues(segment);
            boundaryValues.push(buffers.pre, buffers.post);
        }
        if (boundaryValues.length === 0) {
            return { effective: null, isMixed: false };
        }
        const unique = new Set(boundaryValues);
        if (unique.size === 1) {
            return { effective: boundaryValues[0], isMixed: false };
        }
        return { effective: null, isMixed: true };
    }

    /**
     * Bulk operation over the current segments: writes the same symmetric value to
     * every segment's boundary fields (pre on each first occurrence, post on each last
     * occurrence). A value equal to the global buffer is normalized to null so the
     * boundary falls back directly to Video.TimeBufferMs. Dispatches only the changed
     * boundary occurrences with cloned before-state through the bulk action queue.
     */
    function applyTimeBufferToSegments(segments: ConsecutiveSegment[], valueMs: number) {
        if (segments.length === 0) return;

        const affected: DetectedObjectDto[] = [];
        const before: DetectedObjectDto[] = [];
        const seen = new Set<string>();
        const normalized = valueMs === anonymizationSettings.value.timeBufferMs ? null : valueMs;

        for (const segment of segments) {
            for (const boundary of [segment.first, segment.last]) {
                if (seen.has(boundary.id)) continue;
                seen.add(boundary.id);
                affected.push(boundary);
                before.push(JSON.parse(JSON.stringify(boundary)));
            }
        }

        for (const segment of segments) {
            segment.first.preBufferMsOverride = normalized;
            segment.last.postBufferMsOverride = normalized;
        }

        const { changed, beforeState } = getChangedObjects(affected, before);
        if (changed.length > 0) {
            state.onDetectedObjectsBulkUpdated?.(state.videoId, changed, 'track-settings', beforeState);
        }
    }

    function resetTimeBufferToSegments(segments: ConsecutiveSegment[]) {
        applyTimeBufferToSegments(segments, anonymizationSettings.value.timeBufferMs);
    }

    function resolveOccurrenceBlurSize(obj: DetectedObjectDto): number {
        const trackBlur = obj.trackId == null
            ? null
            : getTrackSettings(obj.trackId).blurSizePercentOverride;
        return obj.occurrenceBlurSizePercentOverride
            ?? trackBlur
            ?? anonymizationSettings.value.blurSizePercent;
    }

    return {
        getTrackSettings,
        getSegmentBufferValues,
        resolveOccurrenceBlurSize,
        applyTrackBlurShape,
        applyTrackBlurSize,
        resetTrackBlurSize,
        applyOccurrenceBlurSize,
        resetOccurrenceBlurSize,
        applySegmentPre,
        applySegmentPost,
        resetSegmentPre,
        resetSegmentPost,
        getScopeSegments,
        getTimeBufferState,
        applyTimeBufferToSegments,
        resetTimeBufferToSegments
    };
}

import type { ComputedRef } from 'vue';
import type {
    AnonymizationSettings,
    AnalyzedFrameDto,
    DetectedObjectDto,
    VideoEditorProps
} from '../types';
import {
    getTrackOccurrences,
    getPrecedingGapBoundary,
    hasFollowingGap,
    hasPrecedingGap,
} from './useConsecutiveTrackSegment';
import type { ConsecutiveSegment } from './useConsecutiveTrackSegment';
import { getChangedObjects } from '../utils/objectDiff';
import { GapHandlingModes, resolveGapHandlingMode, type GapHandlingMode } from '../utils/gapHandling';

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

export type SegmentGapState = {
    hasGapBefore: boolean;
    hasGapAfter: boolean;
    gapBeforeMode: GapHandlingMode | null;
    gapAfterMode: GapHandlingMode | null;
    /** True when After is inactive because Gap after is Interpolate. */
    postInactiveForGap: boolean;
    /** True when Before is inactive because Gap before is Interpolate. */
    preInactiveForGap: boolean;
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

    function getSegmentGapState(segment: ConsecutiveSegment): SegmentGapState {
        const hasGapBefore = hasPrecedingGap(frames.value, segment);
        const hasGapAfter = hasFollowingGap(frames.value, segment);
        const precedingBoundary = hasGapBefore ? getPrecedingGapBoundary(frames.value, segment) : null;
        const gapBeforeMode = precedingBoundary
            ? resolveGapHandlingMode(precedingBoundary.nextGapHandlingMode)
            : null;
        const gapAfterMode = hasGapAfter
            ? resolveGapHandlingMode(segment.last.nextGapHandlingMode)
            : null;

        return {
            hasGapBefore,
            hasGapAfter,
            gapBeforeMode,
            gapAfterMode,
            preInactiveForGap: gapBeforeMode === GapHandlingModes.Interpolate,
            postInactiveForGap: gapAfterMode === GapHandlingModes.Interpolate,
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

    /**
     * Editing Before at a segment that has a preceding gap switches that gap to
     * UseBuffers in the same undoable action. Track-start Before never changes a gap.
     */
    function applySegmentPre(segment: ConsecutiveSegment, valueMs: number) {
        const normalized = valueMs === anonymizationSettings.value.timeBufferMs ? null : valueMs;
        const first = segment.first;
        const precedingBoundary = getPrecedingGapBoundary(frames.value, segment);
        const affected: DetectedObjectDto[] = [first];
        if (precedingBoundary && precedingBoundary.id !== first.id) {
            affected.push(precedingBoundary);
        }
        const before = cloneObjects(affected);

        first.preBufferMsOverride = normalized;
        if (precedingBoundary) {
            precedingBoundary.nextGapHandlingMode = GapHandlingModes.UseBuffers;
        }

        if (affected.length === 1) {
            state.onDetectedObjectUpdated?.(state.videoId, first.analyzedFrameId, first, 'pre-buffer', before);
        } else {
            const { changed, beforeState } = getChangedObjects(affected, before);
            if (changed.length > 0) {
                state.onDetectedObjectsBulkUpdated?.(state.videoId, changed, 'pre-buffer', beforeState);
            }
        }
    }

    /**
     * Editing After at a segment that has a following gap switches that gap to
     * UseBuffers in the same undoable action. Track-end After never changes a gap.
     */
    function applySegmentPost(segment: ConsecutiveSegment, valueMs: number) {
        const normalized = valueMs === anonymizationSettings.value.timeBufferMs ? null : valueMs;
        const last = segment.last;
        const before = cloneObjects([last]);
        last.postBufferMsOverride = normalized;
        if (hasFollowingGap(frames.value, segment)) {
            last.nextGapHandlingMode = GapHandlingModes.UseBuffers;
        }
        state.onDetectedObjectUpdated?.(state.videoId, last.analyzedFrameId, last, 'post-buffer', before);
    }

    function resetSegmentPre(segment: ConsecutiveSegment) {
        const first = segment.first;
        const before = cloneObjects([first]);
        first.preBufferMsOverride = null;
        // Reset never changes gap mode.
        state.onDetectedObjectUpdated?.(state.videoId, first.analyzedFrameId, first, 'pre-buffer', before);
    }

    function resetSegmentPost(segment: ConsecutiveSegment) {
        const last = segment.last;
        const before = cloneObjects([last]);
        last.postBufferMsOverride = null;
        // Reset never changes gap mode.
        state.onDetectedObjectUpdated?.(state.videoId, last.analyzedFrameId, last, 'post-buffer', before);
    }

    function applyGapBeforeMode(segment: ConsecutiveSegment, mode: GapHandlingMode) {
        const boundary = getPrecedingGapBoundary(frames.value, segment);
        if (!boundary) return;
        const before = cloneObjects([boundary]);
        boundary.nextGapHandlingMode = mode === GapHandlingModes.Interpolate ? null : mode;
        state.onDetectedObjectUpdated?.(
            state.videoId,
            boundary.analyzedFrameId,
            boundary,
            'gap-handling',
            before
        );
    }

    function applyGapAfterMode(segment: ConsecutiveSegment, mode: GapHandlingMode) {
        if (!hasFollowingGap(frames.value, segment)) return;
        const last = segment.last;
        const before = cloneObjects([last]);
        last.nextGapHandlingMode = mode === GapHandlingModes.Interpolate ? null : mode;
        state.onDetectedObjectUpdated?.(
            state.videoId,
            last.analyzedFrameId,
            last,
            'gap-handling',
            before
        );
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
        getSegmentGapState,
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
        applyGapBeforeMode,
        applyGapAfterMode
    };
}

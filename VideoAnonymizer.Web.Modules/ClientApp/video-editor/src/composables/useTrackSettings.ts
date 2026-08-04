import type { ComputedRef } from 'vue';
import type {
    AnonymizationSettings,
    AnalyzedFrameDto,
    DetectedObjectDto,
    VideoEditorProps
} from '../types';
import { getTrackOccurrences } from './useConsecutiveTrackSegment';
import type { ConsecutiveSegment } from './useConsecutiveTrackSegment';

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
        resetSegmentPost
    };
}

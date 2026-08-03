import type { ComputedRef } from 'vue';
import type { AnalyzedFrameDto, DetectedObjectDto, TimelineObject, VideoEditorProps } from '../types';
import { applyBoundaryTransferOnAdd, getTrackOccurrences, normalizeSegmentBoundaries } from './useConsecutiveTrackSegment';
import { cloneObjects, getChangedObjects } from '../utils/objectDiff';

function dispatchChanges(
    state: VideoEditorProps,
    changed: DetectedObjectDto[],
    beforeState: DetectedObjectDto[],
    operationType: string
) {
    if (changed.length === 1) {
        state.onDetectedObjectUpdated?.(state.videoId, changed[0].analyzedFrameId, changed[0], operationType, beforeState);
    } else if (changed.length > 1) {
        state.onDetectedObjectsBulkUpdated?.(state.videoId, changed, operationType, beforeState);
    }
}

export function useDetectedObjectActions(
    state: VideoEditorProps,
    frames: ComputedRef<AnalyzedFrameDto[]>,
    currentFrame: ComputedRef<AnalyzedFrameDto | null>
) {
    function toggleObject(id: string, checked: boolean) {
        const matched = frames.value.flatMap(x => x.detectedObjects.filter(y => y.id === id));
        const before = cloneObjects(matched);
        matched.forEach(obj => { obj.selected = checked });
        if (matched.length === 1) {
            state.onDetectedObjectUpdated?.(state.videoId, matched[0].analyzedFrameId, matched[0], 'toggle', before);
        } else if (matched.length > 1) {
            state.onDetectedObjectsBulkUpdated?.(state.videoId, matched, 'toggle', before);
        }
    }

    function toggleTrackedObject(obj: TimelineObject, checked: boolean) {
        if (obj.type === 'single') {
            const before = cloneObjects([obj.detectedObj]);
            obj.detectedObj.selected = checked;
            state.onDetectedObjectUpdated?.(state.videoId, obj.detectedObj.analyzedFrameId, obj.detectedObj, 'toggle', before);
            return;
        }

        const changed: DetectedObjectDto[] = [];
        obj.occurences.forEach(([_, o]) => { changed.push(o); });
        const before = cloneObjects(changed);
        obj.occurences.forEach(([_, o]) => { o.selected = checked; });
        state.onDetectedObjectsBulkUpdated?.(state.videoId, changed, 'toggle', before);
    }

    function setTrackId(timelineObject: TimelineObject, trackId: number) {
        if (timelineObject.type === 'single') {
            const affected: DetectedObjectDto[] = [];
            for (const frame of state.frames) {
                for (const obj of frame.detectedObjects) {
                    if (obj.trackId === trackId) affected.push(obj);
                }
            }
            if (!affected.includes(timelineObject.detectedObj)) {
                affected.push(timelineObject.detectedObj);
            }
            const before = cloneObjects(affected);
            timelineObject.detectedObj.trackId = trackId;
            normalizeSegmentBoundaries(frames.value);
            const { changed, beforeState } = getChangedObjects(affected, before);
            dispatchChanges(state, changed, beforeState, 'reassign');
            return;
        }

        const oldTrackId = timelineObject.occurences[0]?.[1].trackId;
        if (oldTrackId == null) return;

        const affected: DetectedObjectDto[] = [];
        for (const frame of state.frames) {
            for (const obj of frame.detectedObjects) {
                if (obj.trackId === oldTrackId || obj.trackId === trackId) {
                    affected.push(obj);
                }
            }
        }

        const before = cloneObjects(affected);
        affected
            .filter(obj => obj.trackId === oldTrackId)
            .forEach(obj => { obj.trackId = trackId; });
        normalizeSegmentBoundaries(frames.value);
        const { changed, beforeState } = getChangedObjects(affected, before);
        dispatchChanges(state, changed, beforeState, 'reassign');
    }

    function deleteObject(obj: DetectedObjectDto) {
        const frame = state.frames.find(f => f.id === obj.analyzedFrameId);
        if (!frame) return;

        const idx = frame.detectedObjects.findIndex(o => o.id === obj.id);
        if (idx < 0) return;

        frame.detectedObjects.splice(idx, 1);
        normalizeSegmentBoundaries(frames.value);
        state.onDetectedObjectDeleted?.(state.videoId, obj.analyzedFrameId, obj);
    }

    function trackForward(obj: DetectedObjectDto) {
        state.onTrackForward?.(state.videoId, obj.analyzedFrameId, obj);
    }

    function getBlurShapeForTrack(trackId: number): string | null {
        const occurrences = getTrackOccurrences(frames.value, trackId);
        return occurrences.find(o => o.blurShape)?.blurShape ?? null;
    }

    function getBlurSizeOverrideForTrack(trackId: number): number | null {
        const occurrences = getTrackOccurrences(frames.value, trackId);
        return occurrences.find(o => o.blurSizePercentOverride != null)?.blurSizePercentOverride ?? null;
    }

    function addBox(x: number, y: number, width: number, height: number, className: string, trackId: 'new' | number) {
        if (!currentFrame.value) return;

        const frame = currentFrame.value;
        const nextTrackId = frames.value.flatMap(f => f.detectedObjects).reduce((max, o) => Math.max(max, o.trackId ?? 0), 0) + 1;
        const frameTrackIds = new Set(frame.detectedObjects.flatMap(o => o.trackId == null ? [] : [o.trackId]));
        const selectedExistingTrackId = trackId !== 'new' && !frameTrackIds.has(trackId)
            ? trackId
            : null;
        const resolvedTrackId = trackId === 'new' || frameTrackIds.has(trackId)
            ? nextTrackId
            : trackId;
        const blurShape = selectedExistingTrackId == null
            ? null
            : getBlurShapeForTrack(selectedExistingTrackId);
        const blurSizePercentOverride = selectedExistingTrackId == null
            ? null
            : getBlurSizeOverrideForTrack(selectedExistingTrackId);
        const newObj: DetectedObjectDto = {
            id: crypto.randomUUID(),
            confidence: 1,
            className: className || null,
            blurShape,
            blurSizePercentOverride,
            selected: true,
            trackId: resolvedTrackId,
            x,
            y,
            width,
            height,
            analyzedFrameId: frame.id,
        };

        frame.detectedObjects.push(newObj);
        const clearedNeighbors = applyBoundaryTransferOnAdd(frames.value, newObj);
        state.onDetectedObjectAdded?.(state.videoId, frame.id, newObj);
        for (const { obj: neighbor, before: neighborBefore } of clearedNeighbors) {
            state.onDetectedObjectUpdated?.(state.videoId, neighbor.analyzedFrameId, neighbor, 'track-settings', [neighborBefore]);
        }
    }

    return {
        toggleObject,
        toggleTrackedObject,
        setTrackId,
        deleteObject,
        trackForward,
        addBox,
    };
}

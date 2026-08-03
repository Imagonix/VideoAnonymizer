import { ref } from 'vue';
import type { TimelineObject, DetectedObjectDto, AnalyzedFrameDto } from '../types';
import { getTimelineKey } from '../utils/keys';
import { normalizeSegmentBoundaries } from './useConsecutiveTrackSegment';
import { cloneObjects, getChangedObjects } from '../utils/objectDiff';

export function useMerge() {
    const mergeSelectedKeys = ref(new Set<string>());

    function toggle(key: string) {
        const set = mergeSelectedKeys.value;
        if (set.has(key)) { set.delete(key); }
        else { set.add(key); }
        mergeSelectedKeys.value = new Set(set);
    }

    function execute(timelineObjects: TimelineObject[], frames: AnalyzedFrameDto[]): { changed: DetectedObjectDto[], beforeState: DetectedObjectDto[] } {
        const keys = [...mergeSelectedKeys.value];
        if (keys.length < 2) return { changed: [], beforeState: [] };

        const selectedTrackIds: number[] = [];
        const selectedObjIds = new Set<string>();

        for (const obj of timelineObjects) {
            const key = getTimelineKey(obj);
            if (!keys.includes(key)) continue;
            if (obj.type === 'single') {
                selectedObjIds.add(obj.detectedObj.id);
                if (obj.detectedObj.trackId != null) {
                    selectedTrackIds.push(obj.detectedObj.trackId);
                }
            } else {
                const tid = obj.occurences[0]?.[1].trackId;
                if (tid != null) selectedTrackIds.push(tid);
            }
        }

        // The first explicitly selected track is the destination for track id,
        // shape and blur size.
        const targetTrackId = selectedTrackIds.length > 0
            ? selectedTrackIds[0]
            : frames.flatMap(f => f.detectedObjects)
                .reduce((max, o) => Math.max(max, o.trackId ?? 0), 0) + 1;

        const allTrackIdsToMerge = new Set([targetTrackId, ...selectedTrackIds]);

        const affected: DetectedObjectDto[] = [];
        for (const frame of frames) {
            for (const obj of frame.detectedObjects) {
                if ((obj.trackId != null && allTrackIdsToMerge.has(obj.trackId)) || selectedObjIds.has(obj.id)) {
                    affected.push(obj);
                }
            }
        }
        const before = cloneObjects(affected);

        const sourceOccurrences = affected.filter(obj => obj.trackId === targetTrackId);
        const shape = sourceOccurrences.find(obj => obj.blurShape)?.blurShape ?? null;
        const blurSizePercentOverride = sourceOccurrences.find(obj => obj.blurSizePercentOverride != null)?.blurSizePercentOverride ?? null;
        const trackTimeBufferMsOverride = sourceOccurrences.find(obj => obj.trackTimeBufferMsOverride != null)?.trackTimeBufferMsOverride ?? null;

        for (const frame of frames) {
            let frameHasTarget = frame.detectedObjects.some(o => o.trackId === targetTrackId);
            for (const obj of frame.detectedObjects) {
                const shouldMerge = selectedObjIds.has(obj.id) ||
                    (obj.trackId != null && allTrackIdsToMerge.has(obj.trackId));
                if (!shouldMerge) continue;
                if (obj.trackId === targetTrackId) continue;
                if (frameHasTarget) continue;
                obj.trackId = targetTrackId;
                frameHasTarget = true;
            }
        }

        // The merged track adopts the destination track's shape and blur size
        // uniformly across every occurrence.
        for (const obj of affected) {
            if (obj.trackId === targetTrackId) {
                obj.blurShape = shape;
                obj.blurSizePercentOverride = blurSizePercentOverride;
                obj.trackTimeBufferMsOverride = trackTimeBufferMsOverride;
            }
        }

        normalizeSegmentBoundaries(frames);
        const { changed, beforeState } = getChangedObjects(affected, before);
        mergeSelectedKeys.value = new Set();
        return { changed, beforeState };
    }

    return { mergeSelectedKeys, toggle, execute };
}

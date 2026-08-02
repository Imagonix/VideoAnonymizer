import { ref } from 'vue';
import type { AnalyzedFrameDto, DetectedObjectDto } from '../types';
import { normalizeSegmentBoundaries } from './useConsecutiveTrackSegment';
import { cloneObjects, getChangedObjects } from '../utils/objectDiff';

export function useSplit() {
    const splitSourceKey = ref<string | null>(null);

    function execute(
        selectedOccurrences: Map<string, Set<number>>,
        frames: AnalyzedFrameDto[]
    ): { changed: DetectedObjectDto[], beforeState: DetectedObjectDto[] } {
        const sourceTrackIds = new Set<number>();
        for (const rowKey of selectedOccurrences.keys()) {
            const trackId = parseInt(rowKey.replace('track-', ''), 10);
            if (!isNaN(trackId)) sourceTrackIds.add(trackId);
        }

        const affected: DetectedObjectDto[] = [];
        for (const frame of frames) {
            for (const obj of frame.detectedObjects) {
                if (obj.trackId != null && sourceTrackIds.has(obj.trackId)) {
                    affected.push(obj);
                }
            }
        }
        const before = cloneObjects(affected);

        const maxTrackId = frames.flatMap(f => f.detectedObjects)
            .reduce((max, o) => Math.max(max, o.trackId ?? 0), 0);
        const newTrackId = maxTrackId + 1;

        for (const [rowKey, times] of selectedOccurrences) {
            if (times.size === 0) continue;
            const sourceTrackId = parseInt(rowKey.replace('track-', ''), 10);
            if (isNaN(sourceTrackId)) continue;

            for (const frame of frames) {
                if (!times.has(frame.timeSeconds)) continue;
                for (const obj of frame.detectedObjects) {
                    if (obj.trackId === sourceTrackId) {
                        obj.trackId = newTrackId;
                    }
                }
            }
        }

        normalizeSegmentBoundaries(frames);
        const { changed, beforeState } = getChangedObjects(affected, before);

        if (changed.length > 0) {
            splitSourceKey.value = null;
        }
        return { changed, beforeState };
    }

    return { splitSourceKey, execute };
}

import { ref } from 'vue';
import type { TimelineObject, DetectedObjectDto, AnalyzedFrameDto } from '../types';
import { getTimelineKey } from '../utils/keys';

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

        const selectedTrackIds = new Set<number>();
        const selectedObjIds = new Set<string>();

        for (const obj of timelineObjects) {
            const key = getTimelineKey(obj);
            if (!keys.includes(key)) continue;
            if (obj.type === 'single') {
                selectedObjIds.add(obj.detectedObj.id);
                if (obj.detectedObj.trackId != null) {
                    selectedTrackIds.add(obj.detectedObj.trackId);
                }
            } else {
                const tid = obj.occurences[0]?.[1].trackId;
                if (tid != null) selectedTrackIds.add(tid);
            }
        }

        const targetTrackId = selectedTrackIds.size > 0
            ? Math.min(...selectedTrackIds)
            : frames.flatMap(f => f.detectedObjects)
                .reduce((max, o) => Math.max(max, o.trackId ?? 0), 0) + 1;

        const allTrackIdsToMerge = new Set([targetTrackId, ...selectedTrackIds]);

        const changed: DetectedObjectDto[] = [];
        const beforeState: DetectedObjectDto[] = [];

        for (const frame of frames) {
            let frameHasTarget = frame.detectedObjects.some(o => o.trackId === targetTrackId);
            for (const obj of frame.detectedObjects) {
                const shouldMerge = selectedObjIds.has(obj.id) ||
                    (obj.trackId != null && allTrackIdsToMerge.has(obj.trackId));
                if (!shouldMerge) continue;
                if (obj.trackId === targetTrackId) continue;
                if (frameHasTarget) continue;
                beforeState.push(JSON.parse(JSON.stringify(obj)));
                obj.trackId = targetTrackId;
                changed.push(obj);
                frameHasTarget = true;
            }
        }

        mergeSelectedKeys.value = new Set();
        return { changed, beforeState };
    }

    return { mergeSelectedKeys, toggle, execute };
}

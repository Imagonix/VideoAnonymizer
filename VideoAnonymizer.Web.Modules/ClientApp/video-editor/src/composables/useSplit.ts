import { ref } from 'vue';
import type { AnalyzedFrameDto, DetectedObjectDto } from '../types';

export function useSplit() {
    const splitSourceKey = ref<string | null>(null);

    function execute(
        selectedOccurrences: Map<string, Set<number>>,
        frames: AnalyzedFrameDto[]
    ): DetectedObjectDto[] {
        const changed: DetectedObjectDto[] = [];

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
                        changed.push(obj);
                    }
                }
            }
        }

        if (changed.length > 0) {
            splitSourceKey.value = null;
        }
        return changed;
    }

    return { splitSourceKey, execute };
}

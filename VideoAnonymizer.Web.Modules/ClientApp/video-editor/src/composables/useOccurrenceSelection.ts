import { ref } from 'vue';
import type { TimelineObject } from '../types';
import { getTimelineKey } from '../utils/keys';

export function useOccurrenceSelection() {
    const selectedOccurrences = ref(new Map<string, Set<number>>());
    const lastClickedTimes = ref(new Map<string, number>());

    function toggle(rowKey: string, time: number, event: MouseEvent, timelineObjects: TimelineObject[]) {
        const map = selectedOccurrences.value;
        if (!map.has(rowKey)) {
            map.set(rowKey, new Set());
        }
        let times = map.get(rowKey)!;

        if (event.ctrlKey || event.metaKey) {
            if (times.has(time)) { times.delete(time); }
            else { times.add(time); }
        } else if (event.shiftKey) {
            const last = lastClickedTimes.value.get(rowKey);
            if (last != null) {
                const row = timelineObjects.find(o => getTimelineKey(o) === rowKey);
                if (row && row.type === 'tracked') {
                    const sortedTimes = row.occurences.map(([t]) => t).sort((a, b) => a - b);
                    const lastIdx = sortedTimes.indexOf(last);
                    const currIdx = sortedTimes.indexOf(time);
                    if (lastIdx !== -1 && currIdx !== -1) {
                        const start = Math.min(lastIdx, currIdx);
                        const end = Math.max(lastIdx, currIdx);
                        for (let i = start; i <= end; i++) {
                            times.add(sortedTimes[i]);
                        }
                    }
                }
            } else {
                times.add(time);
            }
        } else {
            map.clear();
            map.set(rowKey, new Set([time]));
            times = map.get(rowKey)!;
        }

        if (!times.has(time)) {
            lastClickedTimes.value.delete(rowKey);
        } else {
            lastClickedTimes.value.set(rowKey, time);
        }

        selectedOccurrences.value = new Map(map);
    }

    function totalCount(): number {
        let count = 0;
        for (const times of selectedOccurrences.value.values()) {
            count += times.size;
        }
        return count;
    }

    function hasAny(): boolean {
        for (const times of selectedOccurrences.value.values()) {
            if (times.size > 0) return true;
        }
        return false;
    }

    function hasOnlyTracked(): boolean {
        for (const rowKey of selectedOccurrences.value.keys()) {
            if (!rowKey.startsWith('track-')) return false;
        }
        return selectedOccurrences.value.size > 0;
    }

    function clear() {
        selectedOccurrences.value = new Map();
        lastClickedTimes.value = new Map();
    }

    return {
        selectedOccurrences,
        toggle,
        totalCount,
        hasAny,
        hasOnlyTracked,
        clear,
    };
}

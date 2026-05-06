import { computed, type ComputedRef, type Ref } from 'vue';
import type {
    AnalyzedFrameDto,
    DetectedObjectDto,
    SingleTimelineObject,
    TimelineObject,
    TimelineObjectCount,
    TrackedTimelineObject
} from '../types';
import { buildObjectKey } from '../utils/keys';

export function useTimelineObjects(
    frames: ComputedRef<AnalyzedFrameDto[]>,
    currentTime: Ref<number>
) {
    const currentFrame = computed(() => {
        if (frames.value.length === 0) return null;
        return [...frames.value].sort((a, b) =>
            Math.abs(a.timeSeconds - currentTime.value) - Math.abs(b.timeSeconds - currentTime.value)
        )[0];
    });

    const timelineObjects = computed<TimelineObject[]>(() => {
        const untracked = frames.value.flatMap(frame =>
            frame.detectedObjects
                .filter(x => x.trackId == null)
                .map((obj): SingleTimelineObject => ({
                    detectedObj: obj,
                    type: 'single',
                    timeSeconds: frame.timeSeconds,
                }))
        );

        const grouped = frames.value.flatMap(frame =>
            frame.detectedObjects.map(obj => ({ trackId: obj.trackId, detectedObj: obj, timeSeconds: frame.timeSeconds }))
        ).reduce((acc, x) => {
            if (x.trackId == null) return acc;
            if (!acc[x.trackId]) { acc[x.trackId] = { type: 'tracked', occurences: [] }; }
            acc[x.trackId].occurences.push([x.timeSeconds, x.detectedObj]);
            return acc;
        }, {} as Record<number, TrackedTimelineObject>);

        return [...Object.values(grouped), ...untracked];
    });

    const timelineObjectCounts = computed<TimelineObjectCount[]>(() =>
        frames.value.map(frame => ({ timeSeconds: frame.timeSeconds, count: frame.detectedObjects.length }))
            .sort((a, b) => a.timeSeconds - b.timeSeconds)
    );

    const orderedCurrentFrameObjects = computed<DetectedObjectDto[]>(() => {
        const frame = currentFrame.value;
        if (!frame) return [];

        const orderMap = new Map(timelineObjects.value.map((obj, index) => {
            if (obj.type === 'tracked') {
                const first = obj.occurences[0]?.[1];
                return [first?.trackId != null ? `track-${first.trackId}` : first?.id, index];
            }

            return [obj.detectedObj.id, index];
        }));

        return [...frame.detectedObjects].sort((a, b) => {
            const aOrder = orderMap.get(buildObjectKey(a)) ?? Number.MAX_SAFE_INTEGER;
            const bOrder = orderMap.get(buildObjectKey(b)) ?? Number.MAX_SAFE_INTEGER;
            return aOrder - bOrder;
        });
    });

    return {
        currentFrame,
        timelineObjects,
        timelineObjectCounts,
        orderedCurrentFrameObjects,
    };
}

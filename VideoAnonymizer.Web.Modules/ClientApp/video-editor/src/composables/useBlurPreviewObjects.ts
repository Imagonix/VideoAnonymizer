import { computed, type ComputedRef, type Ref } from 'vue';
import type { AnalyzedFrameDto, AnonymizationSettings, PreviewObject } from '../types';
import { getPredictedBlurPreviewObjects } from '../utils/motionPrediction';
import { buildObjectKey } from '../utils/keys';

export function useBlurPreviewObjects(
    frames: ComputedRef<AnalyzedFrameDto[]>,
    currentFrame: ComputedRef<AnalyzedFrameDto | null>,
    currentTime: Ref<number>,
    anonymizationSettings: ComputedRef<AnonymizationSettings>,
    isAdjust: ComputedRef<boolean>
) {
    return computed(() => {
        const bufferSeconds = anonymizationSettings.value.timeBufferMs / 1000;
        const result: PreviewObject[] = [];
        const current = currentFrame.value;
        if (!current) return [];

        for (const obj of current.detectedObjects) {
            if (!obj.selected) continue;
            result.push({ detectedObject: obj, activation: 'detected' });
        }

        if (isAdjust.value) return result;

        if (!anonymizationSettings.value.interpolateTrackedObjects) {
            return getBufferedBlurPreviewObjects(frames.value, current, bufferSeconds, result);
        }

        return getPredictedBlurPreviewObjects(
            frames.value,
            currentTime.value,
            bufferSeconds
        );
    });
}

function getBufferedBlurPreviewObjects(
    frames: AnalyzedFrameDto[],
    current: AnalyzedFrameDto,
    bufferSeconds: number,
    currentObjects: PreviewObject[]
): PreviewObject[] {
    const result = [...currentObjects];

    for (const frame of [...frames].sort((a, b) =>
        Math.abs(a.timeSeconds - current.timeSeconds) - Math.abs(b.timeSeconds - current.timeSeconds)
    )) {
        const delta = current.timeSeconds - frame.timeSeconds;
        if (Math.abs(delta) > bufferSeconds) continue;

        for (const obj of frame.detectedObjects) {
            if (!obj.selected) continue;

            const key = buildObjectKey(obj);
            if (result.some(r => buildObjectKey(r.detectedObject) === key)) continue;

            result.push({ detectedObject: obj, activation: delta < 0 ? 'pre' : 'post' });
        }
    }

    return result;
}

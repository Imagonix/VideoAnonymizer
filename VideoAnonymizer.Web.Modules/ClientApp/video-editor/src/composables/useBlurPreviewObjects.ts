import { computed, type ComputedRef, type Ref } from 'vue';
import type { AnalyzedFrameDto, AnonymizationSettings, PreviewObject } from '../types';
import { getPredictedBlurPreviewObjects } from '../utils/motionPrediction';

export function useBlurPreviewObjects(
    frames: ComputedRef<AnalyzedFrameDto[]>,
    currentFrame: ComputedRef<AnalyzedFrameDto | null>,
    currentTime: Ref<number>,
    anonymizationSettings: ComputedRef<AnonymizationSettings>,
    isMove: ComputedRef<boolean>
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

        if (!isMove.value) return getPredictedBlurPreviewObjects(
            frames.value,
            currentTime.value,
            bufferSeconds,
            anonymizationSettings.value.interpolateTrackedObjects
        );

        return result;
    });
}

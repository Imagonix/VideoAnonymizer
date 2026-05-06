import { computed, type ComputedRef } from 'vue';
import type { AnalyzedFrameDto, AnonymizationSettings, PreviewObject } from '../types';
import { buildObjectKey } from '../utils/keys';

export function useBlurPreviewObjects(
    frames: ComputedRef<AnalyzedFrameDto[]>,
    currentFrame: ComputedRef<AnalyzedFrameDto | null>,
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

        if (!isMove.value) for (const frame of [...frames.value].sort((a, b) =>
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
    });
}

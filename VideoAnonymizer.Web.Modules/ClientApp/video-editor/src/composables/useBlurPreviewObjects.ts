import { computed, type ComputedRef, type Ref } from 'vue';
import type { AnalyzedFrameDto, AnonymizationSettings, PreviewObject } from '../types';
import { getPredictedBlurPreviewObjects } from '../utils/motionPrediction';
import { isProjectedRegionFullyOutside } from '../utils/projectedRegion';
import { buildObjectKey } from '../utils/keys';

export function useBlurPreviewObjects(
    frames: ComputedRef<AnalyzedFrameDto[]>,
    currentFrame: ComputedRef<AnalyzedFrameDto | null>,
    currentTime: Ref<number>,
    anonymizationSettings: ComputedRef<AnonymizationSettings>,
    isAdjust: ComputedRef<boolean>,
    videoWidth: Ref<number> | ComputedRef<number> = computed(() => 0),
    videoHeight: Ref<number> | ComputedRef<number> = computed(() => 0)
) {
    return computed(() => {
        const bufferSeconds = anonymizationSettings.value.timeBufferMs / 1000;
        const current = currentFrame.value;
        if (!current) return [];

        const width = videoWidth.value;
        const height = videoHeight.value;

        // Included detections drive blur preview. Excluded detections stay visible as
        // selectable ghost outlines so inclusion never hides objects from the editor.
        // Keep raw geometry; the overlay clips via overflow:hidden.
        const includedCurrent: PreviewObject[] = [];
        const excludedCurrent: PreviewObject[] = [];
        for (const obj of current.detectedObjects) {
            if (isProjectedRegionFullyOutside(obj, width, height)) continue;
            if (obj.selected) {
                includedCurrent.push({ detectedObject: obj, activation: 'detected' });
            } else {
                excludedCurrent.push({ detectedObject: obj, activation: 'detected' });
            }
        }

        if (isAdjust.value) {
            return [...includedCurrent, ...excludedCurrent];
        }

        let blurPreview: PreviewObject[];
        if (!anonymizationSettings.value.interpolateTrackedObjects) {
            blurPreview = getBufferedBlurPreviewObjects(
                frames.value,
                current,
                bufferSeconds,
                includedCurrent,
                width,
                height);
        } else {
            blurPreview = getPredictedBlurPreviewObjects(
                frames.value,
                currentTime.value,
                bufferSeconds,
                width,
                height
            );
        }

        return mergeExcludedGhosts(blurPreview, excludedCurrent);
    });
}

/** Keep excluded current-frame detections visible even when blur prediction replaces the list. */
function mergeExcludedGhosts(
    blurPreview: PreviewObject[],
    excludedCurrent: PreviewObject[]
): PreviewObject[] {
    if (excludedCurrent.length === 0) return blurPreview;

    const result = [...blurPreview];
    for (const ghost of excludedCurrent) {
        const key = buildObjectKey(ghost.detectedObject);
        if (result.some(r => buildObjectKey(r.detectedObject) === key)) continue;
        result.push(ghost);
    }
    return result;
}

function getBufferedBlurPreviewObjects(
    frames: AnalyzedFrameDto[],
    current: AnalyzedFrameDto,
    bufferSeconds: number,
    currentObjects: PreviewObject[],
    videoWidth: number,
    videoHeight: number
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

            if (isProjectedRegionFullyOutside(obj, videoWidth, videoHeight)) continue;

            result.push({ detectedObject: obj, activation: delta < 0 ? 'pre' : 'post' });
        }
    }

    return result;
}

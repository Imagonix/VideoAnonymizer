import { computed, ref } from 'vue';
import { describe, expect, it } from 'vitest';
import type { AnalyzedFrameDto, DetectedObjectDto } from '../types';
import { useBlurPreviewObjects } from '../composables/useBlurPreviewObjects';

describe('useBlurPreviewObjects', () => {
    it('keeps disabled interpolation anchored to the current analyzed frame', () => {
        const frames = ref([
            createFrame('f1', 0, [createObject({ trackId: 7, x: 10 })]),
            createFrame('f2', 1, [createObject({ trackId: 7, x: 100 })]),
        ]);
        const currentFrame = computed(() => frames.value[1]);
        const currentTime = ref(0.6);
        const anonymizationSettings = computed(() => ({
            blurSizePercent: 100,
            timeBufferMs: 0,
            interpolateTrackedObjects: false,
        }));
        const isMove = computed(() => false);

        const result = useBlurPreviewObjects(
            computed(() => frames.value),
            currentFrame,
            currentTime,
            anonymizationSettings,
            isMove
        );

        expect(result.value).toHaveLength(1);
        expect(result.value[0].activation).toBe('detected');
        expect(result.value[0].detectedObject.x).toBe(100);

        currentTime.value = 0.9;

        expect(result.value).toHaveLength(1);
        expect(result.value[0].activation).toBe('detected');
        expect(result.value[0].detectedObject.x).toBe(100);
    });

    it('keeps excluded current-frame detections as selectable ghosts without blur interpolation', () => {
        const frames = ref([
            createFrame('f1', 0, [
                createObject({ id: 'included', trackId: 1, x: 10, selected: true }),
                createObject({ id: 'excluded', trackId: 2, x: 200, selected: false }),
            ]),
            createFrame('f2', 1, [
                createObject({ id: 'included-next', trackId: 1, x: 100, selected: true }),
            ]),
        ]);
        const currentFrame = computed(() => frames.value[0]);
        const currentTime = ref(0);
        const anonymizationSettings = computed(() => ({
            blurSizePercent: 100,
            timeBufferMs: 0,
            interpolateTrackedObjects: true,
        }));
        const isAdjust = computed(() => false);

        const result = useBlurPreviewObjects(
            computed(() => frames.value),
            currentFrame,
            currentTime,
            anonymizationSettings,
            isAdjust
        );

        const ids = result.value.map(o => o.detectedObject.id).sort();
        expect(ids).toContain('included');
        expect(ids).toContain('excluded');
        const ghost = result.value.find(o => o.detectedObject.id === 'excluded');
        expect(ghost?.detectedObject.selected).toBe(false);
        expect(ghost?.activation).toBe('detected');
    });
});

function createFrame(
    id: string,
    timeSeconds: number,
    detectedObjects: DetectedObjectDto[]
): AnalyzedFrameDto {
    return {
        id,
        frameIndex: Math.round(timeSeconds * 100),
        timeSeconds,
        videoId: 'video-1',
        detectedObjects: detectedObjects.map(obj => ({
            ...obj,
            analyzedFrameId: id,
        })),
    };
}

function createObject(overrides: Partial<DetectedObjectDto>): DetectedObjectDto {
    return {
        id: overrides.id ?? crypto.randomUUID(),
        confidence: overrides.confidence ?? 0.9,
        className: overrides.className ?? 'license_plate',
        blurShape: overrides.blurShape,
        selected: overrides.selected ?? true,
        trackId: overrides.trackId ?? null,
        x: overrides.x ?? 0,
        y: overrides.y ?? 20,
        width: overrides.width ?? 30,
        height: overrides.height ?? 40,
        analyzedFrameId: overrides.analyzedFrameId ?? '',
    };
}

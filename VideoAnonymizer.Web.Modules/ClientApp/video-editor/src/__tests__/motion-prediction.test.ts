import { describe, expect, it } from 'vitest';
import type { AnalyzedFrameDto, DetectedObjectDto } from '../types';
import { getPredictedBlurPreviewObjects } from '../utils/motionPrediction';

describe('motionPrediction', () => {
    it('interpolates tracked box center and size between analyzed frames', () => {
        const frames = [
            createFrame('f1', 0, [
                createObject({ trackId: 7, x: 10, y: 20, width: 30, height: 40, blurShape: 'rectangle' }),
            ]),
            createFrame('f2', 1, [
                createObject({ trackId: 7, x: 100, y: 60, width: 50, height: 20, blurShape: 'rectangle' }),
            ]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 0.5, 0);

        expect(result).toHaveLength(1);
        expect(result[0].activation).toBe('interpolated');
        expect(result[0].detectedObject).toMatchObject({
            trackId: 7,
            x: 55,
            y: 40,
            width: 40,
            height: 30,
            blurShape: 'rectangle',
        });
    });

    it('uses the exact analyzed box at analyzed frame time', () => {
        const frames = [
            createFrame('f1', 0, [createObject({ trackId: 7, x: 10 })]),
            createFrame('f2', 1, [createObject({ trackId: 7, x: 100 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 1, 0);

        expect(result).toHaveLength(1);
        expect(result[0].activation).toBe('detected');
        expect(result[0].detectedObject.x).toBe(100);
    });

    it('holds previous track through buffer when no matching next sample exists', () => {
        const frames = [
            createFrame('f1', 0, [createObject({ trackId: 1, x: 10 })]),
            createFrame('f2', 1, [createObject({ trackId: 2, x: 100 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 1.15, 0.25);

        expect(result.map(obj => obj.detectedObject.trackId).sort()).toEqual([1, 2]);
    });

    it('uses upcoming track within buffer before the first sample', () => {
        const frames = [
            createFrame('f1', 1, [createObject({ trackId: 1, x: 100, blurShape: 'rectangle' })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 0.85, 0.25);

        expect(result).toHaveLength(1);
        expect(result[0].activation).toBe('pre');
        expect(result[0].detectedObject).toMatchObject({
            trackId: 1,
            x: 100,
            blurShape: 'rectangle',
        });
    });

    it('does not use upcoming track outside buffer before the first sample', () => {
        const frames = [
            createFrame('f1', 1, [createObject({ trackId: 1, x: 100 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 0.7, 0.25);

        expect(result).toHaveLength(0);
    });

    it('does not use upcoming track when buffer is disabled', () => {
        const frames = [
            createFrame('f1', 1, [createObject({ trackId: 1, x: 100 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 0.85, 0);

        expect(result).toHaveLength(0);
    });

    it('keeps untracked objects on their analyzed box instead of interpolating', () => {
        const frames = [
            createFrame('f1', 0, [createObject({ id: 'o1', trackId: null, x: 10 })]),
            createFrame('f2', 1, [createObject({ id: 'o2', trackId: null, x: 100 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 0.5, 0);

        expect(result).toHaveLength(1);
        expect(result[0].detectedObject.x).toBe(10);
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

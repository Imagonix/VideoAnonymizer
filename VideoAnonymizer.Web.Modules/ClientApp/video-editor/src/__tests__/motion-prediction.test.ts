import { describe, expect, it } from 'vitest';
import type { AnalyzedFrameDto, DetectedObjectDto } from '../types';
import { buildAllSegments } from '../composables/useConsecutiveTrackSegment';
import { getPredictedBlurPreviewObjects } from '../utils/motionPrediction';
import { clipProjectedRegionToFrame } from '../utils/projectedRegion';

describe('motionPrediction', () => {
    it('interpolates tracked box center and size between analyzed frames', () => {
        const frames = [
            createFrame('f1', 0, 0, [
                createObject({ trackId: 7, x: 10, y: 20, width: 30, height: 40, blurShape: 'rectangle' }),
            ]),
            createFrame('f2', 1, 1, [
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
            createFrame('f1', 0, 0, [createObject({ trackId: 7, x: 10 })]),
            createFrame('f2', 1, 1, [createObject({ trackId: 7, x: 100 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 1, 0);

        expect(result).toHaveLength(1);
        expect(result[0].activation).toBe('detected');
        expect(result[0].detectedObject.x).toBe(100);
    });

    it('expires a track after its own post-buffer even when other tracks continue', () => {
        const frames = [
            createFrame('f1', 0, 0, [createObject({ trackId: 1, x: 10 })]),
            createFrame('f2', 1, 1, [createObject({ trackId: 2, x: 100 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 1.15, 0.25);

        expect(result.map(obj => obj.detectedObject.trackId).sort()).toEqual([2]);
    });

    it('uses upcoming track within buffer before the first sample', () => {
        const frames = [
            createFrame('f1', 0, 1, [createObject({ trackId: 1, x: 100, blurShape: 'rectangle' })]),
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

    it('extrapolates tracked boxes before the first sample through the buffer', () => {
        const frames = [
            createFrame('f1', 0, 1, [createObject({ trackId: 1, x: 100 })]),
            createFrame('f2', 1, 2, [createObject({ trackId: 1, x: 190 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 0.8, 0.25);

        expect(result).toHaveLength(1);
        expect(result[0].activation).toBe('pre');
        expect(result[0].detectedObject.x).toBe(82);
    });

    it('extrapolates tracked boxes after the last sample through the buffer', () => {
        const frames = [
            createFrame('f1', 0, 0, [createObject({ trackId: 7, x: 10 })]),
            createFrame('f2', 1, 1, [createObject({ trackId: 7, x: 100 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 1.2, 0.25);

        expect(result).toHaveLength(1);
        expect(result[0].activation).toBe('post');
        expect(result[0].detectedObject.x).toBe(118);
    });

    it('does not snap back to the last stored box after the post-buffer elapses', () => {
        const frames = [
            createFrame('f1', 0, 0, [createObject({ trackId: 7, x: 10 })]),
            createFrame('f2', 1, 1, [createObject({ trackId: 7, x: 100 })]),
            // Later unrelated frame used to expand the old coverage window.
            createFrame('f3', 2, 3, [createObject({ trackId: 9, x: 200 })]),
        ];

        const duringBuffer = getPredictedBlurPreviewObjects(frames, 1.2, 0.25);
        expect(duringBuffer).toHaveLength(1);
        expect(duringBuffer[0].detectedObject.trackId).toBe(7);
        expect(duringBuffer[0].detectedObject.x).toBe(118);

        const afterBuffer = getPredictedBlurPreviewObjects(frames, 1.4, 0.25);
        expect(afterBuffer.filter(obj => obj.detectedObject.trackId === 7)).toHaveLength(0);
        // Must not reappear at the historical stored x=100.
        expect(afterBuffer.some(obj => obj.detectedObject.x === 100 && obj.detectedObject.trackId === 7)).toBe(false);
    });

    it('continues post-buffer motion past the last sample without historical fallback', () => {
        const frames = [
            createFrame('f1', 0, 0, [createObject({ trackId: 7, x: 10, width: 30 })]),
            createFrame('f2', 1, 1, [createObject({ trackId: 7, x: 100, width: 30 })]),
        ];

        // Mid-buffer and late-buffer must keep moving outward, never freeze/snap to x=100.
        const mid = getPredictedBlurPreviewObjects(frames, 1.1, 0.5);
        const late = getPredictedBlurPreviewObjects(frames, 1.4, 0.5);
        expect(mid[0].detectedObject.x).toBe(109);
        expect(late[0].detectedObject.x).toBe(136);
        expect(late[0].detectedObject.x).toBeGreaterThan(mid[0].detectedObject.x);
        expect(late[0].detectedObject.x).not.toBe(100);
    });

    it('allows extrapolated tracked boxes to move partly outside the frame', () => {
        const frames = [
            createFrame('f1', 0, 0, [createObject({ trackId: 7, x: 20 })]),
            createFrame('f2', 1, 1, [createObject({ trackId: 7, x: -10 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 1.2, 0.25);

        expect(result).toHaveLength(1);
        expect(result[0].detectedObject.x).toBe(-16);
        expect(result[0].detectedObject.width).toBe(30);
    });

    it('keeps raw width on partial left-edge exit while clip helper shrinks the visible intersection', () => {
        const frames = [
            createFrame('f1', 0, 0, [createObject({ trackId: 7, x: 20, y: 20, width: 30, height: 40 })]),
            createFrame('f2', 1, 1, [createObject({ trackId: 7, x: -10, y: 20, width: 30, height: 40 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 1.2, 0.25, 200, 100);

        expect(result).toHaveLength(1);
        expect(result[0].detectedObject).toMatchObject({
            x: -16,
            y: 20,
            width: 30,
            height: 40,
        });

        const clipped = clipProjectedRegionToFrame(result[0].detectedObject, 200, 100);
        expect(clipped).toMatchObject({ x: 0, y: 20, width: 14, height: 40 });
    });

    it('keeps raw size on partial exits on every edge', () => {
        // Right edge: moving past width 100
        const right = getPredictedBlurPreviewObjects(
            [
                createFrame('f1', 0, 0, [createObject({ trackId: 1, x: 50, y: 20, width: 40, height: 30 })]),
                createFrame('f2', 1, 1, [createObject({ trackId: 1, x: 80, y: 20, width: 40, height: 30 })]),
            ],
            1.2,
            0.5,
            100,
            100
        );
        // projected: x moves 50→80 by 1.0s => +30/s, at 1.2 => 86, width 40 => right=126
        expect(right).toHaveLength(1);
        expect(right[0].detectedObject.width).toBe(40);
        expect(right[0].detectedObject.x + right[0].detectedObject.width).toBeGreaterThan(100);
        expect(clipProjectedRegionToFrame(right[0].detectedObject, 100, 100)!.width).toBeLessThan(40);

        // Top edge
        const top = getPredictedBlurPreviewObjects(
            [
                createFrame('f1', 0, 0, [createObject({ trackId: 2, x: 20, y: 20, width: 30, height: 40 })]),
                createFrame('f2', 1, 1, [createObject({ trackId: 2, x: 20, y: -10, width: 30, height: 40 })]),
            ],
            1.2,
            0.5,
            200,
            100
        );
        expect(top).toHaveLength(1);
        expect(top[0].detectedObject.height).toBe(40);
        expect(top[0].detectedObject.y).toBeLessThan(0);

        // Bottom edge
        const bottom = getPredictedBlurPreviewObjects(
            [
                createFrame('f1', 0, 0, [createObject({ trackId: 3, x: 20, y: 40, width: 30, height: 40 })]),
                createFrame('f2', 1, 1, [createObject({ trackId: 3, x: 20, y: 80, width: 30, height: 40 })]),
            ],
            1.2,
            0.5,
            200,
            100
        );
        expect(bottom).toHaveLength(1);
        expect(bottom[0].detectedObject.height).toBe(40);
        expect(bottom[0].detectedObject.y + bottom[0].detectedObject.height).toBeGreaterThan(100);
    });

    it('returns no region once the projected box is fully outside the frame', () => {
        const frames = [
            createFrame('f1', 0, 0, [createObject({ trackId: 7, x: 10, y: 20, width: 30, height: 40 })]),
            createFrame('f2', 1, 1, [createObject({ trackId: 7, x: -20, y: 20, width: 30, height: 40 })]),
        ];

        // Keep extrapolating left; at enough alpha the box is fully outside width.
        const result = getPredictedBlurPreviewObjects(frames, 1.5, 1.0, 100, 100);

        expect(result).toHaveLength(0);
    });

    it('disappears at buffer end at the final projected position without snapback', () => {
        const frames = [
            createFrame('f1', 0, 0, [createObject({ trackId: 7, x: 40, y: 20, width: 30, height: 40 })]),
            createFrame('f2', 1, 1, [createObject({ trackId: 7, x: 50, y: 20, width: 30, height: 40 })]),
        ];

        const justBeforeEnd = getPredictedBlurPreviewObjects(frames, 1.25, 0.25, 200, 100);
        expect(justBeforeEnd).toHaveLength(1);
        const finalX = justBeforeEnd[0].detectedObject.x;
        expect(finalX).toBeGreaterThan(50);

        const atEndBoundary = getPredictedBlurPreviewObjects(frames, 1.25, 0.25, 200, 100);
        expect(atEndBoundary[0].detectedObject.x).toBe(finalX);

        const afterEnd = getPredictedBlurPreviewObjects(frames, 1.26, 0.25, 200, 100);
        expect(afterEnd).toHaveLength(0);
    });

    it('default Interpolate bridges a missing-track gap in the analyzed-frame sequence', () => {
        const frames = [
            createFrame('f1', 0, 0, [createObject({ trackId: 7, x: 10 })]),
            createFrame('f2', 1, 1, [createObject({ trackId: 7, x: 100 })]),
            createFrame('f-gap', 2, 2, []), // intervening analyzed frame without track 7
            createFrame('f3', 3, 3, [createObject({ trackId: 7, x: 200 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 2.0, 0);
        expect(result).toHaveLength(1);
        expect(result[0].detectedObject.x).toBe(150);
        expect(result[0].activation).toBe('interpolated');
    });

    it('forms one segment across non-unit FrameIndex steps when the track is uninterrupted', () => {
        const frames = [
            createFrame('f0', 0, 0, [createObject({ trackId: 7, x: 10 })]),
            createFrame('f15', 15, 0.5, [createObject({ trackId: 7, x: 55 })]),
            createFrame('f30', 30, 1.0, [createObject({ trackId: 7, x: 100 })]),
        ];

        const mid = getPredictedBlurPreviewObjects(frames, 0.5, 0);
        expect(mid).toHaveLength(1);
        expect(mid[0].detectedObject.x).toBe(55);

        const between = getPredictedBlurPreviewObjects(frames, 0.75, 0.25);
        expect(between.filter(r => r.detectedObject.trackId === 7)).toHaveLength(1);
    });

    it('splits segments when an intervening analyzed frame lacks the track and bridges by default', () => {
        const frames = [
            createFrame('f0', 0, 0, [createObject({ trackId: 7, x: 10 })]),
            createFrame('f15', 15, 0.5, []),
            createFrame('f30', 30, 1.0, [createObject({ trackId: 7, x: 100 })]),
        ];

        const mid = getPredictedBlurPreviewObjects(frames, 0.5, 0);
        expect(mid).toHaveLength(1);
        expect(mid[0].detectedObject.x).toBe(55);
        // UseBuffers leaves the gap uncovered when buffers are zero.
        const useBuffersFrames = [
            createFrame('f0', 0, 0, [createObject({ trackId: 7, x: 10, nextGapHandlingMode: 'UseBuffers' })]),
            createFrame('f15', 15, 0.5, []),
            createFrame('f30', 30, 1.0, [createObject({ trackId: 7, x: 100 })]),
        ];
        expect(getPredictedBlurPreviewObjects(useBuffersFrames, 0.5, 0)).toHaveLength(0);
    });

    it('does not accumulate a history trail for an uninterrupted multi-sample track', () => {
        const frames = [
            createFrame('f0', 0, 0, [createObject({ trackId: 7, x: 10 })]),
            createFrame('f15', 15, 0.5, [createObject({ trackId: 7, x: 55 })]),
            createFrame('f30', 30, 1.0, [createObject({ trackId: 7, x: 100 })]),
            createFrame('f45', 45, 1.5, [createObject({ trackId: 7, x: 145 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 0.75, 0.25);
        expect(result).toHaveLength(1);
        expect(result[0].detectedObject.trackId).toBe(7);
    });

    it('does not use upcoming track outside buffer before the first sample', () => {
        const frames = [
            createFrame('f1', 0, 1, [createObject({ trackId: 1, x: 100 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 0.7, 0.25);

        expect(result).toHaveLength(0);
    });

    it('does not use upcoming track when buffer is disabled', () => {
        const frames = [
            createFrame('f1', 0, 1, [createObject({ trackId: 1, x: 100 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 0.85, 0);

        expect(result).toHaveLength(0);
    });

    it('keeps untracked objects on their analyzed box instead of interpolating', () => {
        const frames = [
            createFrame('f1', 0, 0, [createObject({ id: 'o1', trackId: null, x: 10 })]),
            createFrame('f2', 1, 1, [createObject({ id: 'o2', trackId: null, x: 100 })]),
        ];

        const result = getPredictedBlurPreviewObjects(frames, 0.5, 0);

        expect(result).toHaveLength(0);
    });
});

describe('buildAllSegments adjacency', () => {
    it('groups FrameIndex 0, 15, and 30 into one segment when the track is present on every analyzed frame', () => {
        const frames = [
            createFrame('f0', 0, 0, [createObject({ id: 'a', trackId: 7, x: 10 })]),
            createFrame('f15', 15, 0.5, [createObject({ id: 'b', trackId: 7, x: 55 })]),
            createFrame('f30', 30, 1.0, [createObject({ id: 'c', trackId: 7, x: 100 })]),
        ];

        const segments = buildAllSegments(frames).filter(s => s.first.trackId === 7);
        expect(segments).toHaveLength(1);
        expect(segments[0].occurrences.map(o => o.id)).toEqual(['a', 'b', 'c']);
    });

    it('splits FrameIndex 0 and 30 when the analyzed frame at 15 lacks the track', () => {
        const frames = [
            createFrame('f0', 0, 0, [createObject({ id: 'a', trackId: 7, x: 10 })]),
            createFrame('f15', 15, 0.5, []),
            createFrame('f30', 30, 1.0, [createObject({ id: 'c', trackId: 7, x: 100 })]),
        ];

        const segments = buildAllSegments(frames).filter(s => s.first.trackId === 7);
        expect(segments).toHaveLength(2);
        expect(segments[0].occurrences.map(o => o.id)).toEqual(['a']);
        expect(segments[1].occurrences.map(o => o.id)).toEqual(['c']);
    });
});

describe('clipProjectedRegionToFrame', () => {
    it('returns the box unchanged when video dimensions are unknown', () => {
        const obj = createObject({ x: -10, y: -5, width: 30, height: 40 });
        expect(clipProjectedRegionToFrame(obj, 0, 0)).toEqual(obj);
    });

    it('returns null when the box is fully outside', () => {
        const obj = createObject({ x: -50, y: 10, width: 20, height: 20 });
        expect(clipProjectedRegionToFrame(obj, 100, 100)).toBeNull();
    });

    it('returns the in-frame intersection for a partial exit', () => {
        const obj = createObject({ x: -10, y: 10, width: 30, height: 20 });
        expect(clipProjectedRegionToFrame(obj, 100, 100)).toMatchObject({
            x: 0,
            y: 10,
            width: 20,
            height: 20,
        });
    });
});

function createFrame(
    id: string,
    frameIndex: number,
    timeSeconds: number,
    detectedObjects: DetectedObjectDto[]
): AnalyzedFrameDto {
    return {
        id,
        frameIndex,
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
        blurSizePercentOverride: overrides.blurSizePercentOverride ?? null,
        occurrenceBlurSizePercentOverride: overrides.occurrenceBlurSizePercentOverride ?? null,
        preBufferMsOverride: overrides.preBufferMsOverride ?? null,
        postBufferMsOverride: overrides.postBufferMsOverride ?? null,
        nextGapHandlingMode: overrides.nextGapHandlingMode ?? null,
        selected: overrides.selected ?? true,
        trackId: overrides.trackId === undefined ? 7 : overrides.trackId,
        x: overrides.x ?? 0,
        y: overrides.y ?? 20,
        width: overrides.width ?? 30,
        height: overrides.height ?? 40,
        analyzedFrameId: overrides.analyzedFrameId ?? '',
    };
}

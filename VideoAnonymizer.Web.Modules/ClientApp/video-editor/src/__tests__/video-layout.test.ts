import { describe, it, expect } from 'vitest';
import { computeVideoFrameSize, centeredRect, computeInspectorPlacement } from '../utils/videoLayout';

describe('computeVideoFrameSize', () => {
    it('scales a landscape video to fit the container width', () => {
        const size = computeVideoFrameSize({
            containerWidth: 800,
            containerHeight: 600,
            videoWidth: 1600,
            videoHeight: 900,
        });
        expect(size.width).toBe(800);
        expect(size.height).toBe(450);
    });

    it('letterboxes a portrait video within the container height', () => {
        const size = computeVideoFrameSize({
            containerWidth: 800,
            containerHeight: 600,
            videoWidth: 900,
            videoHeight: 1600,
        });
        expect(size.height).toBe(600);
        expect(size.width).toBe(338);
    });

    it('honors the margin when scaling', () => {
        const size = computeVideoFrameSize({
            containerWidth: 1000,
            containerHeight: 600,
            videoWidth: 1600,
            videoHeight: 900,
            margin: 16,
        });
        expect(size.width).toBe(968);
        expect(size.height).toBe(545);
    });

    it('falls back to the container when video dimensions are unknown', () => {
        const size = computeVideoFrameSize({
            containerWidth: 400,
            containerHeight: 300,
            videoWidth: 0,
            videoHeight: 0,
        });
        expect(size).toEqual({ width: 400, height: 300 });
    });

    it('keeps the intrinsic aspect ratio', () => {
        const size = computeVideoFrameSize({
            containerWidth: 1000,
            containerHeight: 900,
            videoWidth: 1920,
            videoHeight: 1080,
        });
        expect(size.width / size.height).toBeCloseTo(1920 / 1080, 2);
    });
});

describe('centeredRect', () => {
    it('centers the frame in the container', () => {
        const rect = centeredRect(900, 600, { width: 800, height: 450 });
        expect(rect).toEqual({ left: 50, top: 75, width: 800, height: 450 });
    });
});

describe('computeInspectorPlacement', () => {
    const videoRect = { left: 0, top: 0, width: 900, height: 600 };
    const base = {
        stageWidth: 900,
        stageHeight: 600,
        videoRect,
        videoWidth: 1600,
        videoHeight: 900,
    };

    it('places the inspector on the right side when the box is on the left', () => {
        const placement = computeInspectorPlacement({
            ...base,
            box: { x: 50, y: 100, width: 200, height: 100 },
        });
        expect(placement.left).toBeGreaterThan(450);
        expect(placement.width).toBe(240);
    });

    it('places the inspector on the left side when the box is on the right', () => {
        const placement = computeInspectorPlacement({
            ...base,
            box: { x: 1400, y: 100, width: 200, height: 100 },
        });
        expect(placement.left).toBeLessThan(450);
    });

    it('stays clamped within the stage', () => {
        const placement = computeInspectorPlacement({
            ...base,
            box: { x: 50, y: 100, width: 200, height: 100 },
        });
        expect(placement.top).toBeGreaterThanOrEqual(0);
        expect(placement.left + placement.width).toBeLessThanOrEqual(900);
    });

    it('does not cover a box in the bottom half of a full-frame video', () => {
        const placement = computeInspectorPlacement({
            ...base,
            box: { x: 50, y: 400, width: 200, height: 150 },
        });
        expect(placement.top + 210).toBeLessThanOrEqual(400);
    });

    it('prefers the horizontal letterbox margin over overlaying the video', () => {
        const placement = computeInspectorPlacement({
            stageWidth: 1200,
            stageHeight: 600,
            videoRect: { left: 300, top: 75, width: 600, height: 450 },
            videoWidth: 800,
            videoHeight: 600,
            box: { x: 40, y: 200, width: 100, height: 100 },
        });
        expect(placement.left).toBeGreaterThan(300 + 600);
    });

    it('prefers the vertical letterbox margin opposite a bottom-half box', () => {
        const placement = computeInspectorPlacement({
            stageWidth: 900,
            stageHeight: 800,
            videoRect: { left: 0, top: 250, width: 900, height: 300 },
            videoWidth: 900,
            videoHeight: 300,
            box: { x: 100, y: 200, width: 100, height: 50 },
        });
        expect(placement.top + 210).toBeLessThanOrEqual(250);
        expect(placement.top).toBeGreaterThanOrEqual(8);
    });
});

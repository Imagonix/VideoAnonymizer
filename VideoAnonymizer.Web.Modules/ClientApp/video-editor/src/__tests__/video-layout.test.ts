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
        expect(placement.width).toBe(320);
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

    it('places the inspector vertically centered on the stage', () => {
        const placement = computeInspectorPlacement({
            ...base,
            box: { x: 50, y: 100, width: 200, height: 100 },
        });
        expect(placement.top).toBeCloseTo((600 - 210) / 2, 0);
    });

    it('only ever uses the left or right edge as the initial left position', () => {
        const margin = 8;
        const rightEdge = base.stageWidth - 320 - margin;
        for (const box of [
            { x: 50, y: 100, width: 200, height: 100 },
            { x: 1400, y: 100, width: 200, height: 100 },
            { x: 800, y: 400, width: 200, height: 150 },
        ]) {
            const placement = computeInspectorPlacement({ ...base, box });
            expect([margin, rightEdge]).toContain(placement.left);
        }
    });

    it('never automatically places the inspector above or below the video', () => {
        const stage = { stageWidth: 900, stageHeight: 600, videoWidth: 1600, videoHeight: 900 };
        const boxes = [
            { x: 50, y: 100, width: 200, height: 100 },
            { x: 1400, y: 100, width: 200, height: 100 },
            { x: 50, y: 400, width: 200, height: 150 },
            { x: 1400, y: 300, width: 200, height: 150 },
        ];
        for (const box of boxes) {
            const placement = computeInspectorPlacement({ ...stage, videoRect, box });
            expect(placement.top).toBeCloseTo((600 - 210) / 2, 0);
        }
    });

    it('places the inspector on the stage edge opposite the box', () => {
        const placement = computeInspectorPlacement({
            stageWidth: 1200,
            stageHeight: 600,
            videoRect: { left: 300, top: 75, width: 600, height: 450 },
            videoWidth: 800,
            videoHeight: 600,
            box: { x: 40, y: 200, width: 100, height: 100 },
        });
        // Default desktop width is 320 px on the right edge of the stage.
        expect(placement.left).toBe(1200 - 320 - 8);
        expect(placement.width).toBe(320);
    });

    it('clamps to the stage when the stage is smaller than the inspector', () => {
        // Callers shrink the effective width to fit; placement then clamps top/left.
        const placement = computeInspectorPlacement({
            stageWidth: 260,
            stageHeight: 220,
            videoRect: { left: 0, top: 0, width: 260, height: 220 },
            videoWidth: 900,
            videoHeight: 300,
            box: { x: 100, y: 100, width: 100, height: 50 },
            inspectorWidth: 260 - 16,
        });
        expect(placement.left).toBeGreaterThanOrEqual(0);
        expect(placement.top).toBeGreaterThanOrEqual(0);
        expect(placement.left + placement.width).toBeLessThanOrEqual(260);
        expect(placement.top + 210).toBeLessThanOrEqual(220);
    });
});

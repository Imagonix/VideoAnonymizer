import { describe, it, expect } from 'vitest';
import type { AnalyzedFrameDto, DetectedObjectDto, PreviewObject } from '../types';
import { getPredictedBlurPreviewObjects } from '../utils/motionPrediction';
import { clipProjectedRegionToFrame } from '../utils/projectedRegion';
import featureText from './projection-edge.feature?raw';

type StepHandler = (world: World, match: RegExpMatchArray) => void | Promise<void>;

type StepDefinition = {
    pattern: RegExp;
    handler: StepHandler;
};

type Scenario = {
    name: string;
    steps: string[];
};

type World = {
    frames: AnalyzedFrameDto[];
    during: PreviewObject[];
    after: PreviewObject[];
    partialResults: Record<string, PreviewObject[]>;
    finalInBuffer: PreviewObject[];
    gapResult: PreviewObject[];
};

function emptyWorld(): World {
    return {
        frames: [],
        during: [],
        after: [],
        partialResults: {},
        finalInBuffer: [],
        gapResult: [],
    };
}

function frame(
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
        detectedObjects: detectedObjects.map(o => ({ ...o, analyzedFrameId: id })),
    };
}

function obj(overrides: Partial<DetectedObjectDto>): DetectedObjectDto {
    return {
        id: overrides.id ?? crypto.randomUUID(),
        confidence: 0.9,
        className: 'license_plate',
        selected: true,
        trackId: overrides.trackId ?? 7,
        x: overrides.x ?? 0,
        y: overrides.y ?? 20,
        width: overrides.width ?? 30,
        height: overrides.height ?? 40,
        analyzedFrameId: '',
        nextGapHandlingMode: overrides.nextGapHandlingMode ?? null,
        ...overrides,
    };
}

const steps: StepDefinition[] = [
    {
        pattern: /^a consecutive track segment moving right across two analyzed frames$/,
        handler: world => {
            world.frames = [
                frame('f1', 0, 0, [obj({ trackId: 7, x: 10, width: 30 })]),
                frame('f2', 1, 1, [obj({ trackId: 7, x: 100, width: 30 })]),
            ];
        },
    },
    {
        pattern: /^the preview projects during the post-buffer$/,
        handler: world => {
            world.during = getPredictedBlurPreviewObjects(world.frames, 1.2, 0.5);
            world.after = getPredictedBlurPreviewObjects(world.frames, 1.6, 0.5);
        },
    },
    {
        pattern: /^the box keeps moving past the last stored position$/,
        handler: world => {
            expect(world.during).toHaveLength(1);
            expect(world.during[0].detectedObject.x).toBeGreaterThan(100);
        },
    },
    {
        pattern: /^the box never reappears at the last stored position after the buffer ends$/,
        handler: world => {
            expect(world.after).toHaveLength(0);
        },
    },
    {
        pattern: /^a consecutive track segment exiting each video edge$/,
        handler: world => {
            world.partialResults = {};
        },
    },
    {
        pattern: /^the preview projects a partially outside box with known video dimensions$/,
        handler: world => {
            world.partialResults.left = getPredictedBlurPreviewObjects(
                [
                    frame('l1', 0, 0, [obj({ trackId: 1, x: 20, y: 20, width: 30, height: 40 })]),
                    frame('l2', 1, 1, [obj({ trackId: 1, x: -10, y: 20, width: 30, height: 40 })]),
                ],
                1.2,
                0.25,
                200,
                100
            );
            world.partialResults.right = getPredictedBlurPreviewObjects(
                [
                    frame('r1', 0, 0, [obj({ trackId: 2, x: 50, y: 20, width: 40, height: 30 })]),
                    frame('r2', 1, 1, [obj({ trackId: 2, x: 80, y: 20, width: 40, height: 30 })]),
                ],
                1.2,
                0.5,
                100,
                100
            );
            world.partialResults.top = getPredictedBlurPreviewObjects(
                [
                    frame('t1', 0, 0, [obj({ trackId: 3, x: 20, y: 20, width: 30, height: 40 })]),
                    frame('t2', 1, 1, [obj({ trackId: 3, x: 20, y: -10, width: 30, height: 40 })]),
                ],
                1.2,
                0.5,
                200,
                100
            );
            world.partialResults.bottom = getPredictedBlurPreviewObjects(
                [
                    frame('b1', 0, 0, [obj({ trackId: 4, x: 20, y: 40, width: 30, height: 40 })]),
                    frame('b2', 1, 1, [obj({ trackId: 4, x: 20, y: 80, width: 30, height: 40 })]),
                ],
                1.2,
                0.5,
                200,
                100
            );
        },
    },
    {
        pattern: /^the raw boxes retain equal width or height and continue center velocity$/,
        handler: world => {
            const left = world.partialResults.left[0].detectedObject;
            expect(left.width).toBe(30);
            expect(left.x).toBeLessThan(0);

            const right = world.partialResults.right[0].detectedObject;
            expect(right.width).toBe(40);
            expect(right.x + right.width).toBeGreaterThan(100);

            const top = world.partialResults.top[0].detectedObject;
            expect(top.height).toBe(40);
            expect(top.y).toBeLessThan(0);

            const bottom = world.partialResults.bottom[0].detectedObject;
            expect(bottom.height).toBe(40);
            expect(bottom.y + bottom.height).toBeGreaterThan(100);
        },
    },
    {
        pattern: /^their visible intersections are smaller partial clips$/,
        handler: world => {
            const leftClip = clipProjectedRegionToFrame(world.partialResults.left[0].detectedObject, 200, 100);
            expect(leftClip).not.toBeNull();
            expect(leftClip!.width).toBeLessThan(30);
            expect(leftClip!.x).toBe(0);

            const rightClip = clipProjectedRegionToFrame(world.partialResults.right[0].detectedObject, 100, 100);
            expect(rightClip).not.toBeNull();
            expect(rightClip!.width).toBeLessThan(40);

            const topClip = clipProjectedRegionToFrame(world.partialResults.top[0].detectedObject, 200, 100);
            expect(topClip).not.toBeNull();
            expect(topClip!.height).toBeLessThan(40);
            expect(topClip!.y).toBe(0);

            const bottomClip = clipProjectedRegionToFrame(world.partialResults.bottom[0].detectedObject, 200, 100);
            expect(bottomClip).not.toBeNull();
            expect(bottomClip!.height).toBeLessThan(40);
        },
    },
    {
        pattern: /^a consecutive track segment that fully leaves the left edge during post-buffer$/,
        handler: world => {
            world.frames = [
                frame('f1', 0, 0, [obj({ trackId: 7, x: 10, y: 20, width: 30, height: 40 })]),
                frame('f2', 1, 1, [obj({ trackId: 7, x: -20, y: 20, width: 30, height: 40 })]),
            ];
        },
    },
    {
        pattern: /^the preview projects after the box is completely outside$/,
        handler: world => {
            world.after = getPredictedBlurPreviewObjects(world.frames, 1.5, 1.0, 100, 100);
        },
    },
    {
        pattern: /^no preview region is returned for that track$/,
        handler: world => {
            const result = world.after.length > 0
                ? world.after
                : world.gapResult;
            expect(result).toHaveLength(0);
        },
    },
    {
        pattern: /^a consecutive track segment that stays partly inside through its post-buffer$/,
        handler: world => {
            world.frames = [
                frame('f1', 0, 0, [obj({ trackId: 7, x: 40, y: 20, width: 30, height: 40 })]),
                frame('f2', 1, 1, [obj({ trackId: 7, x: 50, y: 20, width: 30, height: 40 })]),
            ];
        },
    },
    {
        pattern: /^the preview projects at the buffer end and just after$/,
        handler: world => {
            world.finalInBuffer = getPredictedBlurPreviewObjects(world.frames, 1.25, 0.25, 200, 100);
            world.after = getPredictedBlurPreviewObjects(world.frames, 1.26, 0.25, 200, 100);
        },
    },
    {
        pattern: /^the final in-buffer region is the continued projection$/,
        handler: world => {
            expect(world.finalInBuffer).toHaveLength(1);
            expect(world.finalInBuffer[0].detectedObject.x).toBeGreaterThan(50);
            expect(world.finalInBuffer[0].detectedObject.x).not.toBe(50);
        },
    },
    {
        pattern: /^no region is returned after the buffer ends$/,
        handler: world => {
            expect(world.after).toHaveLength(0);
        },
    },
    {
        pattern: /^a track with a missing analyzed frame between two segments$/,
        handler: world => {
            world.frames = [
                frame('f1', 0, 0, [obj({ trackId: 7, x: 10 })]),
                frame('f2', 1, 1, [obj({ trackId: 7, x: 100 })]),
                frame('f-gap', 2, 2, []),
                frame('f3', 3, 3, [obj({ trackId: 7, x: 200 })]),
            ];
        },
    },
    {
        pattern: /^the preview projects inside the gap with no buffer$/,
        handler: world => {
            world.gapResult = getPredictedBlurPreviewObjects(world.frames, 2.0, 0);
        },
    },
    {
        pattern: /^exactly one interpolated gap region is returned for that track$/,
        handler: world => {
            const regions = world.gapResult.filter(r => r.detectedObject.trackId === 7);
            expect(regions).toHaveLength(1);
            expect(regions[0].activation).toBe('interpolated');
        },
    },
    {
        pattern: /^an uninterrupted track sampled at FrameIndex 0, 15, and 30$/,
        handler: world => {
            world.frames = [
                frame('f0', 0, 0, [obj({ trackId: 7, x: 10 })]),
                frame('f15', 15, 0.5, [obj({ trackId: 7, x: 55 })]),
                frame('f30', 30, 1.0, [obj({ trackId: 7, x: 100 })]),
            ];
        },
    },
    {
        pattern: /^the preview projects at a time between those samples$/,
        handler: world => {
            world.during = getPredictedBlurPreviewObjects(world.frames, 0.75, 0.25);
        },
    },
    {
        pattern: /^exactly one preview region is returned for that track$/,
        handler: world => {
            const regions = world.during.filter(r => r.detectedObject.trackId === 7);
            expect(regions).toHaveLength(1);
        },
    },
    {
        pattern: /^a track present at FrameIndex 0 and 30 but missing at 15$/,
        handler: world => {
            world.frames = [
                frame('f0', 0, 0, [obj({ trackId: 7, x: 10 })]),
                frame('f15', 15, 0.5, []),
                frame('f30', 30, 1.0, [obj({ trackId: 7, x: 100 })]),
            ];
        },
    },
    {
        pattern: /^a track present at FrameIndex 0 and 30 but missing at 15 with UseBuffers gap mode$/,
        handler: world => {
            world.frames = [
                frame('f0', 0, 0, [obj({ trackId: 7, x: 10, nextGapHandlingMode: 'UseBuffers' })]),
                frame('f15', 15, 0.5, []),
                frame('f30', 30, 1.0, [obj({ trackId: 7, x: 100 })]),
            ];
        },
    },
    {
        pattern: /^the preview projects at the middle empty analyzed frame with no buffer$/,
        handler: world => {
            world.gapResult = getPredictedBlurPreviewObjects(world.frames, 0.5, 0);
            world.after = [];
        },
    },
];

function parseFeature(text: string): Scenario[] {
    const scenarios: Scenario[] = [];
    let current: Scenario | null = null;
    for (const rawLine of text.split(/\r?\n/)) {
        const line = rawLine.trim();
        if (line.startsWith('Scenario:')) {
            current = { name: line.slice('Scenario:'.length).trim(), steps: [] };
            scenarios.push(current);
            continue;
        }
        if (!current) continue;
        if (/^(Given|When|Then|And)\s+/.test(line)) {
            current.steps.push(line.replace(/^(Given|When|Then|And)\s+/, ''));
        }
    }
    return scenarios;
}

function runStep(world: World, step: string) {
    for (const def of steps) {
        const match = step.match(def.pattern);
        if (match) {
            def.handler(world, match);
            return;
        }
    }
    throw new Error(`No step definition for: ${step}`);
}

describe('projection-edge.feature', () => {
    for (const scenario of parseFeature(featureText)) {
        it(scenario.name, () => {
            const world = emptyWorld();
            for (const step of scenario.steps) {
                runStep(world, step);
            }
        });
    }
});

import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { nextTick, reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import type { AnalyzedFrameDto, DetectedObjectDto, VideoEditorProps } from '../types';
import featureText from './gap-handling.feature?raw';
import { applyBoundaryTransferOnAdd, normalizeSegmentBoundaries } from '../composables/useConsecutiveTrackSegment';

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
    wrapper?: ReturnType<typeof mount>;
    state?: VideoEditorProps;
    onDetectedObjectUpdated?: ReturnType<typeof vi.fn>;
    onDetectedObjectsBulkUpdated?: ReturnType<typeof vi.fn>;
};

function createFrame(
    id: string,
    frameIndex: number,
    timeSeconds: number,
    objects: Partial<DetectedObjectDto>[]
): AnalyzedFrameDto {
    return {
        id,
        frameIndex,
        timeSeconds,
        videoId: 'v1',
        detectedObjects: objects.map((o, i) => ({
            id: o.id ?? `obj-${id}-${i}`,
            confidence: o.confidence ?? 0.9,
            className: o.className ?? 'face',
            blurShape: o.blurShape ?? null,
            blurSizePercentOverride: o.blurSizePercentOverride ?? null,
            occurrenceBlurSizePercentOverride: o.occurrenceBlurSizePercentOverride ?? null,
            preBufferMsOverride: o.preBufferMsOverride ?? null,
            postBufferMsOverride: o.postBufferMsOverride ?? null,
            nextGapHandlingMode: o.nextGapHandlingMode ?? null,
            selected: o.selected ?? true,
            trackId: o.trackId ?? null,
            x: o.x ?? 0,
            y: o.y ?? 0,
            width: o.width ?? 20,
            height: o.height ?? 30,
            analyzedFrameId: id,
        })),
    };
}

/** Track 2 has occurrences at f1 and f3 with a real gap at f2. */
function gappedTrackFrames() {
    return [
        createFrame('f1', 0, 0, [
            { id: 'o1', trackId: 1 },
            { id: 'o4', trackId: 2 },
        ]),
        createFrame('f2', 1, 1, [
            { id: 'o2', trackId: 1 },
        ]),
        createFrame('f3', 2, 2, [
            { id: 'o3', trackId: 1 },
            { id: 'o5', trackId: 2 },
        ]),
    ];
}

function mountEditor(frames: AnalyzedFrameDto[], overrides: Partial<VideoEditorProps> = {}) {
    const state = reactive({
        videoId: 'v1',
        videoSourceUrl: 'http://example.com/v.mp4',
        anonymizationSettings: { blurSizePercent: 200, timeBufferMs: 300, interpolateTrackedObjects: true },
        frames,
        ...overrides,
    } as VideoEditorProps);
    const wrapper = mount(VideoEditorApp, {
        props: { state },
        global: {
            stubs: {
                VideoPlayer: {
                    template: '<div class="mock-video" />',
                    methods: { setVolume: () => {}, togglePlayback: () => {} },
                },
                Timeline: {
                    template: '<div class="mock-timeline"><slot /></div>',
                    props: ['duration', 'currentTime', 'isPlaying', 'volume', 'objectCounts'],
                },
                BoundingBoxOverlay: {
                    template: '<div class="mock-overlay" />',
                    props: ['objects', 'anonymizationSettings', 'videoDimensions', 'highlightedRowKey', 'splitSourceKey', 'alwaysShowKeys', 'selectedKey'],
                },
            },
        },
    });

    return { wrapper, state };
}

function openEditor(world: World, frames: AnalyzedFrameDto[] = gappedTrackFrames()) {
    world.onDetectedObjectUpdated = vi.fn();
    world.onDetectedObjectsBulkUpdated = vi.fn();

    const mounted = mountEditor(frames, {
        onDetectedObjectUpdated: world.onDetectedObjectUpdated,
        onDetectedObjectsBulkUpdated: world.onDetectedObjectsBulkUpdated,
    });

    world.wrapper = mounted.wrapper;
    world.state = mounted.state;
}

function findObject(world: World, objectId: string) {
    return world.state!.frames
        .flatMap(frame => frame.detectedObjects)
        .find(obj => obj.id === objectId)!;
}

async function selectById(world: World, objectId: string) {
    const vm = world.wrapper!.vm as any;
    vm.selectObject(findObject(world, objectId));
    // Seek to the occurrence so selectedOccurrence resolves to this segment boundary.
    const frame = world.state!.frames.find(f => f.detectedObjects.some(o => o.id === objectId));
    if (frame) {
        vm.currentTime = frame.timeSeconds;
    }
    await nextTick();
}

function settings(world: World) {
    return (world.wrapper!.vm as any).selectedTrackSettings;
}

const steps: StepDefinition[] = [
    {
        pattern: /^the editor is open with a track that has two segments separated by a real gap$/,
        handler: world => {
            openEditor(world);
        },
    },
    {
        pattern: /^the reviewer selects an occurrence in the first segment$/,
        handler: async world => {
            await selectById(world, 'o4');
        },
    },
    {
        pattern: /^Gap after is available and Gap before is hidden$/,
        handler: world => {
            expect(settings(world).hasGapAfter).toBe(true);
            expect(settings(world).hasGapBefore).toBe(false);
            expect(world.wrapper!.find('[data-testid="gap-after-control"]').exists()).toBe(true);
            expect(world.wrapper!.find('[data-testid="gap-before-control"]').exists()).toBe(false);
        },
    },
    {
        pattern: /^the reviewer selects an occurrence in the second segment$/,
        handler: async world => {
            await selectById(world, 'o5');
        },
    },
    {
        pattern: /^Gap before is available and Gap after is hidden$/,
        handler: world => {
            expect(settings(world).hasGapBefore).toBe(true);
            expect(settings(world).hasGapAfter).toBe(false);
            expect(world.wrapper!.find('[data-testid="gap-before-control"]').exists()).toBe(true);
            expect(world.wrapper!.find('[data-testid="gap-after-control"]').exists()).toBe(false);
        },
    },
    {
        pattern: /^Gap after shows Interpolate$/,
        handler: world => {
            expect(settings(world).gapAfterMode).toBe('Interpolate');
        },
    },
    {
        pattern: /^the last occurrence before the gap stores a null nextGapHandlingMode$/,
        handler: world => {
            expect(findObject(world, 'o4').nextGapHandlingMode ?? null).toBeNull();
        },
    },
    {
        pattern: /^the reviewer selects the first segment and sets After to (\d+)$/,
        handler: async (world, match) => {
            await selectById(world, 'o4');
            const vm = world.wrapper!.vm as any;
            vm.handleUpdatePost(Number(match[1]));
            await nextTick();
        },
    },
    {
        pattern: /^Gap after becomes UseBuffers$/,
        handler: world => {
            expect(settings(world).gapAfterMode).toBe('UseBuffers');
        },
    },
    {
        pattern: /^the last occurrence before the gap stores UseBuffers$/,
        handler: world => {
            expect(findObject(world, 'o4').nextGapHandlingMode).toBe('UseBuffers');
        },
    },
    {
        pattern: /^Vue sends one authoritative update for that boundary$/,
        handler: world => {
            expect(world.onDetectedObjectUpdated).toHaveBeenCalled();
            const call = world.onDetectedObjectUpdated!.mock.calls[0];
            expect(call[3]).toBe('post-buffer');
            expect(call[2].postBufferMsOverride).toBe(450);
            expect(call[2].nextGapHandlingMode).toBe('UseBuffers');
        },
    },
    {
        pattern: /^the reviewer selects the second segment and sets Before to (\d+)$/,
        handler: async (world, match) => {
            await selectById(world, 'o5');
            const vm = world.wrapper!.vm as any;
            vm.handleUpdatePre(Number(match[1]));
            await nextTick();
        },
    },
    {
        pattern: /^Gap before becomes UseBuffers$/,
        handler: world => {
            expect(settings(world).gapBeforeMode).toBe('UseBuffers');
        },
    },
    {
        pattern: /^the previous segment last occurrence stores UseBuffers$/,
        handler: world => {
            expect(findObject(world, 'o4').nextGapHandlingMode).toBe('UseBuffers');
        },
    },
    {
        pattern: /^the editor is open with a UseBuffers gap and custom After on the first segment$/,
        handler: async world => {
            openEditor(world);
            findObject(world, 'o4').postBufferMsOverride = 450;
            findObject(world, 'o4').nextGapHandlingMode = 'UseBuffers';
            await selectById(world, 'o4');
        },
    },
    {
        pattern: /^the reviewer resets After$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.handleResetPost();
            await nextTick();
        },
    },
    {
        pattern: /^the After override is cleared$/,
        handler: world => {
            expect(findObject(world, 'o4').postBufferMsOverride ?? null).toBeNull();
        },
    },
    {
        pattern: /^Gap after remains UseBuffers$/,
        handler: world => {
            expect(findObject(world, 'o4').nextGapHandlingMode).toBe('UseBuffers');
            expect(settings(world).gapAfterMode).toBe('UseBuffers');
        },
    },
    {
        pattern: /^the reviewer sets Gap after to Interpolate$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.handleUpdateGapAfter('Interpolate');
            await nextTick();
        },
    },
    {
        pattern: /^the custom After override remains stored$/,
        handler: world => {
            expect(findObject(world, 'o4').postBufferMsOverride).toBe(450);
        },
    },
    {
        pattern: /^the inspector explains that After is inactive while Interpolate is selected$/,
        handler: world => {
            expect(settings(world).postInactiveForGap).toBe(true);
            expect(world.wrapper!.find('[data-testid="post-inactive-hint"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the reviewer applies Time buffer (\d+) to the entire track$/,
        handler: async (world, match) => {
            await selectById(world, 'o4');
            const vm = world.wrapper!.vm as any;
            vm.handleUpdateTrackTimeBuffer(Number(match[1]));
            await nextTick();
        },
    },
    {
        pattern: /^every current segment boundary stores (\d+)$/,
        handler: (world, match) => {
            const value = Number(match[1]);
            expect(findObject(world, 'o4').preBufferMsOverride).toBe(value);
            expect(findObject(world, 'o4').postBufferMsOverride).toBe(value);
            expect(findObject(world, 'o5').preBufferMsOverride).toBe(value);
            expect(findObject(world, 'o5').postBufferMsOverride).toBe(value);
        },
    },
    {
        pattern: /^every internal gap is UseBuffers$/,
        handler: world => {
            expect(findObject(world, 'o4').nextGapHandlingMode).toBe('UseBuffers');
        },
    },
    {
        pattern: /^the editor is open with custom segment boundaries and UseBuffers gaps$/,
        handler: async world => {
            openEditor(world);
            findObject(world, 'o4').preBufferMsOverride = 450;
            findObject(world, 'o4').postBufferMsOverride = 450;
            findObject(world, 'o4').nextGapHandlingMode = 'UseBuffers';
            findObject(world, 'o5').preBufferMsOverride = 450;
            findObject(world, 'o5').postBufferMsOverride = 450;
            await selectById(world, 'o4');
        },
    },
    {
        pattern: /^the reviewer resets the track Time buffer$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.handleResetTrackTimeBuffer();
            await nextTick();
        },
    },
    {
        pattern: /^every segment boundary override is cleared$/,
        handler: world => {
            expect(findObject(world, 'o4').preBufferMsOverride ?? null).toBeNull();
            expect(findObject(world, 'o4').postBufferMsOverride ?? null).toBeNull();
            expect(findObject(world, 'o5').preBufferMsOverride ?? null).toBeNull();
            expect(findObject(world, 'o5').postBufferMsOverride ?? null).toBeNull();
        },
    },
    {
        pattern: /^every internal gap remains UseBuffers$/,
        handler: world => {
            expect(findObject(world, 'o4').nextGapHandlingMode).toBe('UseBuffers');
        },
    },
    {
        pattern: /^the editor is open with a UseBuffers gap between two segments$/,
        handler: world => {
            openEditor(world);
            findObject(world, 'o4').nextGapHandlingMode = 'UseBuffers';
            findObject(world, 'o4').postBufferMsOverride = 400;
        },
    },
    {
        pattern: /^the reviewer adds an occurrence that closes the gap$/,
        handler: world => {
            const added: DetectedObjectDto = {
                id: 'o-close',
                confidence: 0.9,
                className: 'face',
                selected: true,
                trackId: 2,
                x: 0,
                y: 0,
                width: 20,
                height: 30,
                analyzedFrameId: 'f2',
                nextGapHandlingMode: null,
                postBufferMsOverride: null,
                preBufferMsOverride: null,
            };
            const f2 = world.state!.frames.find(f => f.id === 'f2')!;
            f2.detectedObjects.push(added);
            applyBoundaryTransferOnAdd(world.state!.frames, added);
            normalizeSegmentBoundaries(world.state!.frames);
        },
    },
    {
        pattern: /^the resulting single segment has no nextGapHandlingMode$/,
        handler: world => {
            const track2 = world.state!.frames
                .flatMap(f => f.detectedObjects)
                .filter(o => o.trackId === 2);
            expect(track2.length).toBe(3);
            for (const obj of track2) {
                expect(obj.nextGapHandlingMode ?? null).toBeNull();
            }
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

async function runStep(world: World, step: string) {
    for (const def of steps) {
        const match = step.match(def.pattern);
        if (match) {
            await def.handler(world, match);
            return;
        }
    }
    throw new Error(`No step definition for: ${step}`);
}

describe('gap-handling.feature', () => {
    for (const scenario of parseFeature(featureText)) {
        it(scenario.name, async () => {
            const world: World = {};
            for (const step of scenario.steps) {
                await runStep(world, step);
            }
        });
    }
});

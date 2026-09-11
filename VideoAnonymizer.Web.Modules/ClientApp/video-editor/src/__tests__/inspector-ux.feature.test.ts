import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { nextTick, reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import { computeVideoFrameSize } from '../utils/videoLayout';
import type { AnalyzedFrameDto, DetectedObjectDto, VideoEditorProps } from '../types';
import featureText from './inspector-ux.feature?raw';

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
            x: o.x ?? 100,
            y: o.y ?? 50,
            width: o.width ?? 100,
            height: o.height ?? 50,
            analyzedFrameId: id,
        })),
    };
}

function continuousTrackFrames() {
    return [
        createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1 }]),
        createFrame('f2', 1, 1, [{ id: 'o2', trackId: 1 }]),
    ];
}

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

function openEditor(world: World, frames: AnalyzedFrameDto[] = continuousTrackFrames()) {
    world.onDetectedObjectUpdated = vi.fn();
    world.onDetectedObjectsBulkUpdated = vi.fn();

    const mounted = mountEditor(frames, {
        onDetectedObjectUpdated: world.onDetectedObjectUpdated,
        onDetectedObjectsBulkUpdated: world.onDetectedObjectsBulkUpdated,
    });

    world.wrapper = mounted.wrapper;
    world.state = mounted.state;
}

function setupStage(world: World, width: number, height: number) {
    const vm = world.wrapper!.vm as any;
    vm.workspaceSize = { width, height };
    vm.videoNaturalWidth = 1600;
    vm.videoNaturalHeight = 900;
    vm.videoFrameSize = computeVideoFrameSize({
        containerWidth: width,
        containerHeight: height,
        videoWidth: 1600,
        videoHeight: 900,
        margin: 16,
    });
}

function findObject(world: World, objectId: string) {
    return world.state!.frames
        .flatMap(frame => frame.detectedObjects)
        .find(obj => obj.id === objectId)!;
}

async function selectById(world: World, objectId: string) {
    const vm = world.wrapper!.vm as any;
    vm.selectObject(findObject(world, objectId));
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
        pattern: /^the editor is open with a selected tracked occurrence on a wide stage$/,
        handler: async world => {
            openEditor(world);
            setupStage(world, 900, 600);
            await selectById(world, 'o1');
        },
    },
    {
        pattern: /^the inspector group width is 320 pixels$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.INSPECTOR_WIDTH).toBe(320);
            expect(vm.effectiveInspectorWidth).toBe(320);
            expect(vm.inspectorPlacement.width).toBe(320);
        },
    },
    {
        pattern: /^the automatic placement uses the 320 px width on the opposite side$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            // Box near left -> inspector on right edge: 900 - 320 - 8
            expect(vm.inspectorPlacement.left).toBe(900 - 320 - 8);
            expect(vm.inspectorPlacement.width).toBe(320);
        },
    },
    {
        pattern: /^the editor is open with a selected tracked occurrence on a narrow stage$/,
        handler: async world => {
            openEditor(world);
            setupStage(world, 280, 400);
            await selectById(world, 'o1');
        },
    },
    {
        pattern: /^the inspector group width fits inside the stage margins$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.effectiveInspectorWidth).toBe(280 - 16);
            expect(vm.inspectorPlacement.width).toBe(280 - 16);
            expect(vm.inspectorPlacement.left + vm.inspectorPlacement.width)
                .toBeLessThanOrEqual(280);
        },
    },
    {
        pattern: /^inspector labels and gap captions do not use text-overflow ellipsis$/,
        handler: world => {
            const panel = world.wrapper!.find('[data-testid="object-details-panel"]');
            expect(panel.find('.scope-panel-title').exists()).toBe(true);
            // Wrapping rows keep labels/actions readable instead of a single truncated line.
            expect(panel.findAll('.scope-row--wrap').length).toBeGreaterThan(0);
            // No ellipsis utility classes on titles/labels.
            for (const el of panel.findAll('.scope-panel-title, .details-field-label, .details-title')) {
                expect(el.classes().join(' ')).not.toMatch(/ellipsis|truncate/);
            }
        },
    },
    {
        pattern: /^the editor is open with a continuous track and outer buffer controls$/,
        handler: async world => {
            openEditor(world, continuousTrackFrames());
            setupStage(world, 900, 600);
            await selectById(world, 'o1');
        },
    },
    {
        pattern: /^the segment Before and After inputs use step 100$/,
        handler: world => {
            expect(world.wrapper!.find('[data-testid="segment-pre-input"]').attributes('step')).toBe('100');
            expect(world.wrapper!.find('[data-testid="segment-post-input"]').attributes('step')).toBe('100');
        },
    },
    {
        pattern: /^the track Time buffer control is absent$/,
        handler: world => {
            expect(world.wrapper!.find('[data-testid="track-time-buffer-input"]').exists()).toBe(false);
            expect(world.wrapper!.find('[data-testid="badge-time-buffer"]').exists()).toBe(false);
        },
    },
    {
        pattern: /^the reviewer types segment Before as (\d+)$/,
        handler: async (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.handleUpdatePre(Number(match[1]));
            await nextTick();
        },
    },
    {
        pattern: /^the stored Before override is (\d+) without rounding to a step$/,
        handler: (world, match) => {
            expect(findObject(world, 'o1').preBufferMsOverride).toBe(Number(match[1]));
        },
    },
    {
        pattern: /^the occurrence and track blur inputs use step 10$/,
        handler: world => {
            expect(world.wrapper!.find('[data-testid="occurrence-blur-input"]').attributes('step')).toBe('10');
            expect(world.wrapper!.find('[data-testid="track-blur-input"]').attributes('step')).toBe('10');
        },
    },
    {
        pattern: /^the reviewer types occurrence blur as (\d+)$/,
        handler: async (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.handleUpdateOccurrenceBlurSize(Number(match[1]));
            await nextTick();
        },
    },
    {
        pattern: /^the stored occurrence blur override is (\d+) without rounding to a step$/,
        handler: (world, match) => {
            expect(findObject(world, 'o1').occurrenceBlurSizePercentOverride).toBe(Number(match[1]));
        },
    },
    {
        pattern: /^the editor is open with a track that has two segments separated by a real gap$/,
        handler: world => {
            openEditor(world, gappedTrackFrames());
            setupStage(world, 900, 600);
        },
    },
    {
        pattern: /^the reviewer unchecks Interpolate gap after on the first segment$/,
        handler: async world => {
            await selectById(world, 'o4');
            const vm = world.wrapper!.vm as any;
            vm.handleUpdateGapAfter('UseBuffers');
            await nextTick();
        },
    },
    {
        pattern: /^the second segment shows Interpolate gap before unchecked$/,
        handler: async world => {
            await selectById(world, 'o5');
            expect(settings(world).gapBeforeMode).toBe('UseBuffers');
            const control = world.wrapper!.find('[data-testid="gap-before-control"]');
            expect(control.exists()).toBe(true);
            expect(control.find('input[type="checkbox"]').element).toHaveProperty('checked', false);
            expect(world.wrapper!.find('[data-testid="gap-before-label"]').text())
                .toBe('Interpolate gap before');
        },
    },
    {
        pattern: /^the reviewer checks Interpolate gap before on the second segment$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.handleUpdateGapBefore('Interpolate');
            await nextTick();
        },
    },
    {
        pattern: /^the first segment shows Interpolate gap after checked$/,
        handler: async world => {
            await selectById(world, 'o4');
            expect(settings(world).gapAfterMode).toBe('Interpolate');
            const control = world.wrapper!.find('[data-testid="gap-after-control"]');
            expect(control.find('input[type="checkbox"]').element).toHaveProperty('checked', true);
        },
    },
    {
        pattern: /^the stored gap mode is null for default Interpolate$/,
        handler: world => {
            expect(findObject(world, 'o4').nextGapHandlingMode ?? null).toBeNull();
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
        const stepMatch = line.match(/^(Given|When|Then|And)\s+(.*)$/);
        if (stepMatch && current) {
            current.steps.push(stepMatch[2]);
        }
    }
    return scenarios;
}

function runFeature(text: string, stepDefinitions: StepDefinition[]) {
    for (const scenario of parseFeature(text)) {
        it(scenario.name, async () => {
            const world: World = {};
            for (const step of scenario.steps) {
                const definition = stepDefinitions
                    .map(candidate => ({ candidate, match: step.match(candidate.pattern) }))
                    .find(entry => entry.match);
                if (!definition?.match) {
                    throw new Error(`No step definition found for: ${step}`);
                }
                await definition.candidate.handler(world, definition.match);
            }
            world.wrapper?.unmount();
        });
    }
}

describe('Inspector UX refinements (Command 24)', () => {
    runFeature(featureText, steps);
});

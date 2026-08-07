import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import { computeVideoFrameSize } from '../utils/videoLayout';
import type { AnalyzedFrameDto, DetectedObjectDto, VideoEditorProps } from '../types';
import featureText from './inspector-placement.feature?raw';

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
    wrapper?: ReturnType<typeof mount>['wrapper'];
    state?: VideoEditorProps;
};

function createFrame(
    id: string,
    timeSeconds: number,
    objects: Partial<DetectedObjectDto>[]
): AnalyzedFrameDto {
    return {
        id,
        timeSeconds,
        videoId: 'v1',
        detectedObjects: objects.map((o, i) => ({
            id: o.id ?? `obj-${id}-${i}`,
            confidence: o.confidence ?? 0.9,
            className: o.className ?? 'face',
            blurShape: o.blurShape ?? null,
            blurSizePercentOverride: o.blurSizePercentOverride ?? null,
            preBufferMsOverride: o.preBufferMsOverride ?? null,
            postBufferMsOverride: o.postBufferMsOverride ?? null,
            selected: o.selected ?? true,
            trackId: o.trackId ?? null,
            x: o.x ?? 0,
            y: o.y ?? 0,
            width: o.width ?? 100,
            height: o.height ?? 50,
            analyzedFrameId: id,
        })),
    };
}

function singleBoxFrame(x: number, overrides: Partial<DetectedObjectDto> = {}) {
    return [
        createFrame('f1', 0, [
            { id: 'o1', trackId: 1, x, ...overrides },
        ]),
    ];
}

function twoOccurrenceTrackFrames() {
    return [
        createFrame('f1', 0, [
            { id: 'o1', trackId: 1, x: 100 },
        ]),
        createFrame('f2', 1, [
            { id: 'o2', trackId: 1, x: 1400 },
        ]),
    ];
}

function mountEditor(frames: AnalyzedFrameDto[], overrides: Partial<VideoEditorProps> = {}) {
    const state = reactive({
        videoId: 'v1',
        videoSourceUrl: 'http://example.com/v.mp4',
        anonymizationSettings: { blurSizePercent: 200, timeBufferMs: 300, interpolateTrackedObjects: false },
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
            },
        },
    });

    return { wrapper, state };
}

function openEditor(world: World, frames: AnalyzedFrameDto[] = singleBoxFrame(100)) {
    const mounted = mountEditor(frames, {
        onDetectedObjectUpdated: vi.fn(),
        onDetectedObjectsBulkUpdated: vi.fn(),
        onDetectedObjectAdded: vi.fn(),
        onDetectedObjectDeleted: vi.fn(),
    });

    world.wrapper = mounted.wrapper;
    world.state = mounted.state;
}

function setupWideStage(world: World) {
    const vm = world.wrapper!.vm as any;
    vm.workspaceSize = { width: 900, height: 600 };
    vm.videoNaturalWidth = 1600;
    vm.videoNaturalHeight = 900;
    vm.videoFrameSize = computeVideoFrameSize({
        containerWidth: 900,
        containerHeight: 600,
        videoWidth: 1600,
        videoHeight: 900,
        margin: 16
    });
}

function selectBox(world: World, objectId = 'o1') {
    const vm = world.wrapper!.vm as any;
    const object = vm.getFrames()
        .flatMap((frame: AnalyzedFrameDto) => frame.detectedObjects)
        .find((obj: DetectedObjectDto) => obj.id === objectId);
    vm.selectObject(object);
}

function dragHandle(world: World) {
    return world.wrapper!.find('.inspector-drag-handle');
}

async function dragInspector(
    world: World,
    start: { x: number; y: number },
    end: { x: number; y: number },
    pointerId: number
) {
    const handle = dragHandle(world);
    await handle.trigger('pointerdown', { clientX: start.x, clientY: start.y, pointerId });
    await handle.trigger('pointermove', { clientX: end.x, clientY: end.y, pointerId });
    await handle.trigger('pointerup', { clientX: end.x, clientY: end.y, pointerId });
}

function blurAreaStyle(world: World) {
    return world.wrapper!.get('[data-testid="blur-area-outline"]').attributes('style');
}

function expectBlurArea(world: World, width: number, height: number) {
    const style = blurAreaStyle(world);
    expect(style).toContain(`width: ${width}px`);
    expect(style).toContain(`height: ${height}px`);
}

const steps: StepDefinition[] = [
    {
        pattern: /^the editor is open with a box on the left of a wide stage$/,
        handler: world => {
            openEditor(world, singleBoxFrame(100));
            setupWideStage(world);
        },
    },
    {
        pattern: /^the editor is open with a box on the right of a wide stage$/,
        handler: world => {
            openEditor(world, singleBoxFrame(1400));
            setupWideStage(world);
        },
    },
    {
        pattern: /^the editor is open with an open inspector$/,
        handler: world => {
            openEditor(world, singleBoxFrame(100));
            setupWideStage(world);
            selectBox(world);
        },
    },
    {
        pattern: /^the editor is open with an open inspector on track 1$/,
        handler: world => {
            openEditor(world, twoOccurrenceTrackFrames());
            setupWideStage(world);
            selectBox(world);
        },
    },
    {
        pattern: /^the editor is open with a track without a blur override$/,
        handler: world => {
            openEditor(world, singleBoxFrame(100));
        },
    },
    {
        pattern: /^the editor is open with a track whose blur size is overridden$/,
        handler: world => {
            openEditor(world, singleBoxFrame(100, { blurSizePercentOverride: 150 }));
        },
    },
    {
        pattern: /^the reviewer selects that box$/,
        handler: world => {
            selectBox(world);
        },
    },
    {
        pattern: /^the inspector is vertically centered on the right side of the stage$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.inspectorPlacement.left).toBeGreaterThan(450);
            expect(vm.inspectorPlacement.top).toBeCloseTo((600 - vm.inspectorGroupHeight) / 2, 0);
        },
    },
    {
        pattern: /^the inspector is vertically centered on the left side of the stage$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.inspectorPlacement.left).toBeLessThan(450);
            expect(vm.inspectorPlacement.top).toBeCloseTo((600 - vm.inspectorGroupHeight) / 2, 0);
        },
    },
    {
        pattern: /^the inspector is not placed above or below the stage center$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.inspectorPlacement.top).toBeCloseTo((600 - vm.inspectorGroupHeight) / 2, 0);
        },
    },
    {
        pattern: /^the reviewer drags the inspector handle far beyond the stage edges$/,
        handler: world => {
            return dragInspector(world, { x: 100, y: 100 }, { x: -5000, y: -5000 }, 1);
        },
    },
    {
        pattern: /^the inspector stays fully inside the stage$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            const placement = vm.inspectorPlacement;
            expect(placement.left).toBeGreaterThanOrEqual(8);
            expect(placement.left + 320).toBeLessThanOrEqual(900);
            expect(placement.top).toBeGreaterThanOrEqual(8);
            expect(placement.top + vm.inspectorGroupHeight).toBeLessThanOrEqual(600);
        },
    },
    {
        pattern: /^the reviewer drags the inspector to a custom position and navigates to the next occurrence$/,
        handler: async world => {
            await dragInspector(world, { x: 400, y: 400 }, { x: 100, y: 300 }, 2);
            const vm = world.wrapper!.vm as any;
            vm.goToNextOccurrence();
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the inspector keeps the custom position$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            const expectedTop = Math.round((600 - vm.inspectorGroupHeight) / 2) - 100;
            expect(vm.selectedKey).toBe('track-1');
            // Desktop width 320: initial right-edge left is 900-320-8=572; drag dx=-300 -> 272.
            expect(vm.manualInspectorPosition).toEqual({ top: expectedTop, left: 272 });
            expect(vm.inspectorPlacement.left).toBe(272);
        },
    },
    {
        pattern: /^the reviewer drags the inspector to a custom position, deselects, and reselects the box$/,
        handler: async world => {
            await dragInspector(world, { x: 400, y: 400 }, { x: 100, y: 300 }, 3);
            const vm = world.wrapper!.vm as any;
            vm.clearSelection();
            await world.wrapper!.vm.$nextTick();
            selectBox(world);
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the inspector returns to its automatic placement$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.manualInspectorPosition).toBeNull();
            expect(vm.inspectorPlacement.left).toBe(900 - 320 - 8);
        },
    },
    {
        pattern: /^the reviewer presses and moves the inspector drag handle$/,
        handler: async world => {
            const handle = dragHandle(world);
            await handle.trigger('pointerdown', { clientX: 100, clientY: 100, pointerId: 4 });
            await handle.trigger('pointermove', { clientX: 150, clientY: 130, pointerId: 4 });
            await world.wrapper!.find('.workspace-main').trigger('click');
            await world.wrapper!.vm.$nextTick();
            await handle.trigger('pointerup', { clientX: 150, clientY: 130, pointerId: 4 });
        },
    },
    {
        pattern: /^the current selection is unchanged$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedKey).toBe('track-1');
        },
    },
    {
        pattern: /^the reviewer inspects the blur preview$/,
        handler: world => {
            expect(world.wrapper!.find('[data-testid="blur-area-outline"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the preview blur area is two times the box size$/,
        handler: world => {
            expectBlurArea(world, 200, 100);
        },
    },
    {
        pattern: /^the preview blur area is one and a half times the box size$/,
        handler: world => {
            expectBlurArea(world, 150, 75);
        },
    },
    {
        pattern: /^the reviewer sets the track blur size to (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.applyTrackBlurSize(1, Number(match[1]));
        },
    },
    {
        pattern: /^the reviewer resets the track blur size$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            vm.resetTrackBlurSize(1);
        },
    },
    {
        pattern: /^the reviewer receives an undo change that clears the override$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            const object = vm.getFrames()
                .flatMap((frame: AnalyzedFrameDto) => frame.detectedObjects)
                .find((obj: DetectedObjectDto) => obj.id === 'o1');
            vm.applyChanges({
                objectsToUpdate: [{ ...object, blurSizePercentOverride: null }],
                objectsToRemove: [],
                objectsToAdd: [],
            });
        },
    },
    {
        pattern: /^the reviewer receives a pushed detection change carrying an override$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            const object = vm.getFrames()
                .flatMap((frame: AnalyzedFrameDto) => frame.detectedObjects)
                .find((obj: DetectedObjectDto) => obj.id === 'o1');
            vm.applyChanges({
                objectsToUpdate: [{ ...object, blurSizePercentOverride: 150 }],
                objectsToRemove: [],
                objectsToAdd: [],
            });
        },
    },
];

runFeature(featureText, steps);

function runFeature(text: string, stepDefinitions: StepDefinition[]) {
    const { featureName, scenarios } = parseFeature(text);

    describe(featureName, () => {
        for (const scenario of scenarios) {
            it(scenario.name, async () => {
                const world: World = {};

                for (const step of scenario.steps) {
                    const definition = stepDefinitions
                        .map(candidate => ({ candidate, match: step.match(candidate.pattern) }))
                        .find(candidate => candidate.match);

                    if (!definition?.match) {
                        throw new Error(`No step definition found for: ${step}`);
                    }

                    await definition.candidate.handler(world, definition.match);
                }
            });
        }
    });
}

function parseFeature(text: string) {
    const lines = text.split(/\r?\n/);
    const featureLine = lines.find(line => line.trim().startsWith('Feature:'));
    const featureName = featureLine?.trim().replace(/^Feature:\s*/, '') ?? 'Gherkin feature';
    const scenarios: Scenario[] = [];
    let current: Scenario | undefined;

    for (const rawLine of lines) {
        const line = rawLine.trim();

        if (!line || line.startsWith('@') || line.startsWith('Feature:') || line.startsWith('As ') || line.startsWith('I want ') || line.startsWith('So that ')) {
            continue;
        }

        if (line.startsWith('Scenario:')) {
            current = { name: line.replace(/^Scenario:\s*/, ''), steps: [] };
            scenarios.push(current);
            continue;
        }

        const stepMatch = line.match(/^(Given|When|Then|And)\s+(.*)$/);
        if (stepMatch && current) {
            current.steps.push(stepMatch[2]);
        }
    }

    return { featureName, scenarios };
}

import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import { computeVideoFrameSize } from '../utils/videoLayout';
import type { AnalyzedFrameDto, DetectedObjectDto, VideoEditorProps } from '../types';
import featureText from './selection.feature?raw';

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
    onDetectedObjectUpdated?: ReturnType<typeof vi.fn>;
    onDetectedObjectsBulkUpdated?: ReturnType<typeof vi.fn>;
    onDetectedObjectAdded?: ReturnType<typeof vi.fn>;
    onDetectedObjectDeleted?: ReturnType<typeof vi.fn>;
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
            preBufferMsOverride: o.preBufferMsOverride ?? null,
            postBufferMsOverride: o.postBufferMsOverride ?? null,
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

function createWorkspaceFrames() {
    return [
        createFrame('f1', 0, 0, [
            { id: 'o1', trackId: 1 },
            { id: 'o2', trackId: 2 },
        ]),
        createFrame('f2', 1, 1, [
            { id: 'o3', trackId: 1 },
            { id: 'o4', trackId: 2 },
            { id: 'oX', trackId: null },
        ]),
        createFrame('f3', 2, 2, [
            { id: 'o6', trackId: 1 },
        ]),
    ];
}

function mountEditor(overrides: Partial<VideoEditorProps> = {}) {
    const state = reactive({
        videoId: 'v1',
        videoSourceUrl: 'http://example.com/v.mp4',
        anonymizationSettings: { blurSizePercent: 200, timeBufferMs: 300, interpolateTrackedObjects: true },
        frames: createWorkspaceFrames(),
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

function openEditor(world: World) {
    world.onDetectedObjectUpdated = vi.fn();
    world.onDetectedObjectsBulkUpdated = vi.fn();
    world.onDetectedObjectAdded = vi.fn();
    world.onDetectedObjectDeleted = vi.fn();

    const mounted = mountEditor({
        onDetectedObjectUpdated: world.onDetectedObjectUpdated,
        onDetectedObjectsBulkUpdated: world.onDetectedObjectsBulkUpdated,
        onDetectedObjectAdded: world.onDetectedObjectAdded,
        onDetectedObjectDeleted: world.onDetectedObjectDeleted,
    });

    world.wrapper = mounted.wrapper;
    world.state = mounted.state;
}

function findObject(world: World, objectId: string) {
    const vm = world.wrapper!.vm as any;
    return vm.getFrames()
        .flatMap((frame: AnalyzedFrameDto) => frame.detectedObjects)
        .find((face: DetectedObjectDto) => face.id === objectId);
}

function boxes(world: World) {
    return world.wrapper!.findAll('[data-testid="bounding-box"]');
}

function pressEscape(world: World) {
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
}

const steps: StepDefinition[] = [
    {
        pattern: /^the editor is open in the video-first workspace$/,
        handler: world => {
            openEditor(world);
        },
    },
    {
        pattern: /^the editor is open with a portrait video in a wide stage$/,
        handler: world => {
            openEditor(world);
            const vm = world.wrapper!.vm as any;
            vm.workspaceSize = { width: 1200, height: 600 };
            vm.videoNaturalWidth = 900;
            vm.videoNaturalHeight = 1600;
            vm.videoFrameSize = computeVideoFrameSize({
                containerWidth: 1200,
                containerHeight: 600,
                videoWidth: 900,
                videoHeight: 1600,
                margin: 16
            });
        },
    },
    {
        pattern: /^the reviewer clicks a box of track (\d+)$/,
        handler: async (world, match) => {
            const index = Number(match[1]) - 1;
            const box = boxes(world)[index];
            expect(box).toBeTruthy();
            await box!.trigger('click');
        },
    },
    {
        pattern: /^the reviewer clicks a box of track (\d+) after selecting track (\d+)$/,
        handler: async (world, match) => {
            const firstIndex = Number(match[2]) - 1;
            await boxes(world)[firstIndex]!.trigger('click');
            const index = Number(match[1]) - 1;
            await boxes(world)[index]!.trigger('click');
        },
    },
    {
        pattern: /^the reviewer clicks the untracked box$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.seekTo(1);
            await world.wrapper!.vm.$nextTick();
            const box = boxes(world)[2];
            expect(box).toBeTruthy();
            await box!.trigger('click');
        },
    },
    {
        pattern: /^the reviewer clicks empty video space after selecting a box$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o1'));
            await world.wrapper!.vm.$nextTick();
            await world.wrapper!.find('.workspace-main').trigger('click');
        },
    },
    {
        pattern: /^the reviewer presses Escape after selecting a box$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o1'));
            await world.wrapper!.vm.$nextTick();
            pressEscape(world);
        },
    },
    {
        pattern: /^the reviewer selects a box and activates an action mode and presses Escape$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o1'));
            await world.wrapper!.vm.$nextTick();
            vm.activate('merge');
            pressEscape(world);
        },
    },
    {
        pattern: /^the selection is keyed to track (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedKey).toBe(`track-${match[1]}`);
        },
    },
    {
        pattern: /^the selection is keyed to that object id$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedKey).toBe('obj-oX');
        },
    },
    {
        pattern: /^the object details panel is open$/,
        handler: world => {
            expect(world.wrapper!.find('[data-testid="object-details-panel"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the selection is cleared$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedKey).toBeNull();
        },
    },
    {
        pattern: /^the selection is still set$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedKey).toBe('track-1');
        },
    },
    {
        pattern: /^the reviewer selects a box on the left side of the stage$/,
        handler: world => {
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
            vm.selectObject(findObject(world, 'o1'));
        },
    },
    {
        pattern: /^the inspector is placed on the right side of the stage$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.inspectorPlacement.left).toBeGreaterThan(450);
        },
    },
    {
        pattern: /^the reviewer selects the leftmost box$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o1'));
        },
    },
    {
        pattern: /^the inspector is placed in the horizontal letterbox margin$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            const rect = vm.videoRect;
            expect(vm.inspectorPlacement.left).toBeGreaterThan(rect.left + rect.width);
        },
    },
    {
        pattern: /^the reviewer selects the first occurrence of track (\d+) and clicks next occurrence$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o1'));
            vm.goToNextOccurrence();
        },
    },
    {
        pattern: /^the reviewer selects the second occurrence of track (\d+) and clicks previous occurrence$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            vm.seekTo(1);
            vm.selectObject(findObject(world, 'o3'));
            vm.goToPreviousOccurrence();
        },
    },
    {
        pattern: /^the video seeks to the second occurrence time of track (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            expect(vm.currentTime).toBe(1);
        },
    },
    {
        pattern: /^the video seeks to the first occurrence time of track (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            expect(vm.currentTime).toBe(0);
        },
    },
    {
        pattern: /^the reviewer selects the last occurrence of track (\d+)$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            vm.seekTo(2);
            vm.selectObject(findObject(world, 'o6'));
        },
    },
    {
        pattern: /^the next occurrence button is disabled and previous is enabled$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.canGoNext).toBe(false);
            expect(vm.canGoPrevious).toBe(true);
            const buttons = world.wrapper!.findAll('[aria-label="Next occurrence"]');
            expect((buttons[0].element as HTMLButtonElement).disabled).toBe(true);
        },
    },
    {
        pattern: /^the reviewer selects track (\d+) and the playback time moves to its third occurrence$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o1'));
            vm.seekTo(2);
        },
    },
    {
        pattern: /^the selection is still track (\d+) and the inspector context is the third occurrence$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedKey).toBe(`track-${match[1]}`);
            expect(vm.selectedOccurrence.id).toBe('o6');
        },
    },
    {
        pattern: /^the reviewer focuses a bounding box and presses Enter$/,
        handler: async world => {
            const box = boxes(world)[0];
            expect(box).toBeTruthy();
            await box!.trigger('keydown', { key: 'Enter' });
        },
    },
    {
        pattern: /^the selection is keyed to that box's track$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedKey).toBe('track-1');
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

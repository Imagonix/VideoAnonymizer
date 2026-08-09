import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import type { AnalyzedFrameDto, DetectedObjectDto, VideoEditorProps } from '../types';
import featureText from './adjust-modes.feature?raw';

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
    onDetectedObjectAdded?: ReturnType<typeof vi.fn>;
    onTrackForward?: ReturnType<typeof vi.fn>;
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
    world.onDetectedObjectAdded = vi.fn();
    world.onTrackForward = vi.fn();

    const mounted = mountEditor({
        onDetectedObjectUpdated: world.onDetectedObjectUpdated,
        onDetectedObjectAdded: world.onDetectedObjectAdded,
        onTrackForward: world.onTrackForward,
    });

    world.wrapper = mounted.wrapper;
    world.state = mounted.state;
}

function findObject(world: World, objectId: string) {
    return world.state!.frames
        .flatMap(frame => frame.detectedObjects)
        .find(obj => obj.id === objectId)!;
}

async function selectFirstOccurrence(world: World) {
    const vm = world.wrapper!.vm as any;
    vm.selectObject(findObject(world, 'o1'));
    await world.wrapper!.vm.$nextTick();
}

async function enterAdjust(world: World) {
    const vm = world.wrapper!.vm as any;
    vm.handleAdjustDetection();
    await world.wrapper!.vm.$nextTick();
}

function dragSelectedBox(world: World, deltaX: number, deltaY = 0) {
    const wrapper = world.wrapper!;
    const box = wrapper.find('[data-testid="adjust-box"]');
    const startX = 10;
    const startY = 10;
    box.trigger('mousedown', { clientX: startX, clientY: startY });
    wrapper.find('.overlay').trigger('mousemove', { clientX: startX + deltaX, clientY: startY + deltaY });
    wrapper.find('.overlay').trigger('mouseup');
}

function pressEscape(world: World) {
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
}

const steps: StepDefinition[] = [
    {
        pattern: /^the editor is open in the review workspace$/,
        handler: world => {
            openEditor(world);
        },
    },
    {
        pattern: /^the reviewer selects a box and drags on it$/,
        handler: async world => {
            await selectFirstOccurrence(world);
            const wrapper = world.wrapper!;
            wrapper.find('[data-testid="bounding-box"]').trigger('mousedown', { clientX: 10, clientY: 10 });
            wrapper.find('.overlay').trigger('mousemove', { clientX: 50, clientY: 10 });
            wrapper.find('.overlay').trigger('mouseup');
        },
    },
    {
        pattern: /^the selected box geometry is unchanged$/,
        handler: world => {
            const obj = findObject(world, 'o1');
            expect(obj.x).toBe(0);
            expect(obj.y).toBe(0);
            expect(obj.width).toBe(20);
            expect(obj.height).toBe(30);
        },
    },
    {
        pattern: /^the reviewer clicks Adjust detection for the selected occurrence$/,
        handler: async world => {
            await selectFirstOccurrence(world);
            await world.wrapper!.findAll('button').find(b => b.text() === 'Adjust detection')!.trigger('click');
        },
    },
    {
        pattern: /^the editor is in adjust mode$/,
        handler: world => {
            expect((world.wrapper!.vm as any).activeMode).toBe('adjust');
        },
    },
    {
        pattern: /^the reviewer enters adjust mode and drags the selected box by (\d+) px$/,
        handler: async (world, match) => {
            await selectFirstOccurrence(world);
            await enterAdjust(world);
            dragSelectedBox(world, Number(match[1]));
        },
    },
    {
        pattern: /^the current occurrence is moved by (\d+) px$/,
        handler: (world, match) => {
            const obj = findObject(world, 'o1');
            expect(obj.x).toBe(Number(match[1]));
        },
    },
    {
        pattern: /^no other occurrence of the track moves$/,
        handler: world => {
            expect(findObject(world, 'o3').x).toBe(0);
            expect(findObject(world, 'o6').x).toBe(0);
        },
    },
    {
        pattern: /^the reviewer enters adjust mode and resizes the selected box from the east handle$/,
        handler: async world => {
            await selectFirstOccurrence(world);
            await enterAdjust(world);
            const wrapper = world.wrapper!;
            const handle = wrapper.find('.adjust-handle--e');
            handle.trigger('mousedown', { clientX: 40, clientY: 10 });
            wrapper.find('.overlay').trigger('mousemove', { clientX: 70, clientY: 10 });
            wrapper.find('.overlay').trigger('mouseup');
        },
    },
    {
        pattern: /^the current occurrence width changes$/,
        handler: world => {
            const obj = findObject(world, 'o1');
            expect(obj.width).toBeGreaterThan(20);
        },
    },
    {
        pattern: /^the reviewer enters adjust mode, moves the box, and clicks Discard$/,
        handler: async world => {
            await selectFirstOccurrence(world);
            await enterAdjust(world);
            dragSelectedBox(world, 50);
            await world.wrapper!.vm.$nextTick();
            await world.wrapper!.find('[data-testid="toolbar-discard"]')!.trigger('click');
        },
    },
    {
        pattern: /^the selected box is back at its original geometry$/,
        handler: world => {
            const obj = findObject(world, 'o1');
            expect(obj.x).toBe(0);
            expect(obj.y).toBe(0);
            expect(obj.width).toBe(20);
            expect(obj.height).toBe(30);
        },
    },
    {
        pattern: /^the reviewer enters adjust mode, moves the box, and clicks Confirm$/,
        handler: async world => {
            await selectFirstOccurrence(world);
            await enterAdjust(world);
            dragSelectedBox(world, 50);
            await world.wrapper!.vm.$nextTick();
            await world.wrapper!.find('[data-testid="toolbar-confirm"]')!.trigger('click');
        },
    },
    {
        pattern: /^Vue sends one "([^"]+)" update with the previous geometry$/,
        handler: (world, match) => {
            expect(world.onDetectedObjectUpdated).toHaveBeenCalledOnce();
            const [videoId, analyzedFrameId, dto, operationType, beforeState] = world.onDetectedObjectUpdated!.mock.calls[0];
            expect(videoId).toBe('v1');
            expect(analyzedFrameId).toBe('f1');
            expect(operationType).toBe(match[1]);
            expect(dto.id).toBe('o1');
            expect(dto.x).toBe(50);
            expect(beforeState).toHaveLength(1);
            expect(beforeState[0].x).toBe(0);
        },
    },
    {
        pattern: /^the editor returns to select mode$/,
        handler: world => {
            expect((world.wrapper!.vm as any).activeMode).toBe('select');
        },
    },
    {
        pattern: /^the reviewer enters adjust mode, moves the box, and presses Escape$/,
        handler: async world => {
            await selectFirstOccurrence(world);
            await enterAdjust(world);
            dragSelectedBox(world, 50);
            pressEscape(world);
        },
    },
    {
        pattern: /^Vue sends no update$/,
        handler: world => {
            expect(world.onDetectedObjectUpdated).not.toHaveBeenCalled();
        },
    },
    {
        pattern: /^the reviewer clicks Add Object, draws a box, and confirms from the toolbar$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.handleAddObject();
            await world.wrapper!.vm.$nextTick();
            vm.handleDrawComplete({ x: 10, y: 10, width: 50, height: 60 });
            await world.wrapper!.vm.$nextTick();

            const dialogSelect = world.wrapper!.find('.label-popup select');
            expect((dialogSelect.element as HTMLSelectElement).value).toBe('other');

            await world.wrapper!.find('[data-testid="toolbar-confirm"]')!.trigger('click');
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^a new object is added with class other on the current frame$/,
        handler: world => {
            const frame = world.state!.frames[0];
            const added = frame.detectedObjects[frame.detectedObjects.length - 1];
            expect(added.className).toBe('other');
            expect(added.x).toBe(10);
            expect(added.y).toBe(10);
            expect(added.width).toBe(50);
            expect(added.height).toBe(60);
        },
    },
    {
        pattern: /^the reviewer opens the Advanced menu and clicks Track forward$/,
        handler: async world => {
            await selectFirstOccurrence(world);
            await world.wrapper!.findAll('button').find(b => b.text() === 'Advanced')!.trigger('click');
            await world.wrapper!.findAll('button').find(b => b.text() === 'Track forward')!.trigger('click');
        },
    },
    {
        pattern: /^Vue is asked to track the selected occurrence forward$/,
        handler: world => {
            expect(world.onTrackForward).toHaveBeenCalledOnce();
            const [videoId, analyzedFrameId, dto] = world.onTrackForward!.mock.calls[0];
            expect(videoId).toBe('v1');
            expect(analyzedFrameId).toBe('f1');
            expect(dto.id).toBe('o1');
        },
    },
    {
        pattern: /^the reviewer activates adjust and then activates add$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            await selectFirstOccurrence(world);
            vm.handleAdjustDetection();
            expect(vm.activeMode).toBe('adjust');
            vm.handleAddObject();
        },
    },
    {
        pattern: /^the editor is in add mode only$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.activeMode).toBe('add');
            expect(vm.isAdjust).toBe(false);
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

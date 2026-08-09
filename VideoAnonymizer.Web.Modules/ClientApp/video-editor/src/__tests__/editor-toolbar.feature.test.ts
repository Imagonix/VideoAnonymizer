import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import type { AnalyzedFrameDto, DetectedObjectDto, VideoEditorProps } from '../types';
import featureText from './editor-toolbar.feature?raw';

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
    workspaceListener?: ReturnType<typeof vi.fn>;
    toolbarSnapshot?: { frameSize: { width: number; height: number }; sameToolbarNode: boolean };
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

function defaultFrames() {
    return [createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1 }])];
}

function rectangleShapeFrames() {
    return [createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1, blurShape: 'rectangle' }])];
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
                    data: () => ({ videoRef: { pause: vi.fn(), play: vi.fn() } }),
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

function openEditor(world: World, frames: AnalyzedFrameDto[] = defaultFrames()) {
    world.onDetectedObjectUpdated = vi.fn();
    world.onDetectedObjectAdded = vi.fn();

    const mounted = mountEditor(frames, {
        onDetectedObjectUpdated: world.onDetectedObjectUpdated,
        onDetectedObjectAdded: world.onDetectedObjectAdded,
    });

    world.wrapper = mounted.wrapper;
    world.state = mounted.state;
}

function findObject(world: World, objectId: string) {
    return world.state!.frames
        .flatMap(frame => frame.detectedObjects)
        .find(obj => obj.id === objectId)!;
}

async function selectBox(world: World, objectId = 'o1') {
    const vm = world.wrapper!.vm as any;
    vm.selectObject(findObject(world, objectId));
    await world.wrapper!.vm.$nextTick();
}

async function enterAdjust(world: World) {
    const vm = world.wrapper!.vm as any;
    vm.handleAdjustDetection();
    await world.wrapper!.vm.$nextTick();
}

async function clickTool(world: World, testid: string) {
    const btn = world.wrapper!.find(`[data-testid="${testid}"]`);
    if (!btn.exists()) throw new Error(`Toolbar button "${testid}" not found`);
    await btn.trigger('click');
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

function pressKey(key: string) {
    window.dispatchEvent(new KeyboardEvent('keydown', { key }));
}

const steps: StepDefinition[] = [
    {
        pattern: /^the editor is open in the review workspace$/,
        handler: world => {
            openEditor(world);
        },
    },
    {
        pattern: /^the toolbar is a fixed vertical control on the right side$/,
        handler: world => {
            const workspace = world.wrapper!.get('.workspace-main');
            const toolbar = world.wrapper!.get('[data-testid="editor-toolbar"]');
            expect(toolbar.element.parentElement).toBe(workspace.element);
            expect(toolbar.element.querySelector('.video-frame')).toBeNull();
            const addButton = toolbar.get('[data-testid="toolbar-add-object"]');
            expect(addButton.attributes('aria-label')).toBe('Add Object');
            expect(addButton.attributes('title')).toBe('Add Object');
        },
    },
    {
        pattern: /^the reviewer clicks the Add Object toolbar button$/,
        handler: async world => {
            await clickTool(world, 'toolbar-add-object');
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the reviewer clicks the Add Object toolbar button while a box is selected$/,
        handler: async world => {
            await selectBox(world, 'o1');
            const workspace = world.wrapper!.find('.workspace-main');
            world.workspaceListener = vi.fn();
            workspace.element.addEventListener('click', world.workspaceListener);
            await clickTool(world, 'toolbar-add-object');
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the editor is in add mode$/,
        handler: world => {
            expect((world.wrapper!.vm as any).activeMode).toBe('add');
        },
    },
    {
        pattern: /^playback is paused$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            const videoRef = vm.videoPlayerRef?.videoRef;
            expect(videoRef).toBeTruthy();
            expect(videoRef.pause).toHaveBeenCalled();
        },
    },
    {
        pattern: /^the toolbar shows a disabled Confirm and an enabled Discard$/,
        handler: world => {
            const confirm = world.wrapper!.get('[data-testid="toolbar-confirm"]');
            expect(confirm.attributes('disabled')).toBeDefined();
            const discard = world.wrapper!.get('[data-testid="toolbar-discard"]');
            expect(discard.attributes('disabled')).toBeUndefined();
        },
    },
    {
        pattern: /^the reviewer draws a box on the main video$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.handleDrawComplete({ x: 10, y: 10, width: 50, height: 60 });
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the Confirm becomes enabled$/,
        handler: world => {
            const confirm = world.wrapper!.get('[data-testid="toolbar-confirm"]');
            expect(confirm.attributes('disabled')).toBeUndefined();
        },
    },
    {
        pattern: /^the reviewer clicks the Add Object toolbar button, draws a box, and clicks Confirm$/,
        handler: async world => {
            await clickTool(world, 'toolbar-add-object');
            await world.wrapper!.vm.$nextTick();
            const vm = world.wrapper!.vm as any;
            vm.handleDrawComplete({ x: 10, y: 10, width: 50, height: 60 });
            await world.wrapper!.vm.$nextTick();
            await clickTool(world, 'toolbar-confirm');
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the reviewer clicks the Add Object toolbar button, draws a box, and clicks Discard$/,
        handler: async world => {
            await clickTool(world, 'toolbar-add-object');
            await world.wrapper!.vm.$nextTick();
            const vm = world.wrapper!.vm as any;
            vm.handleDrawComplete({ x: 10, y: 10, width: 50, height: 60 });
            await world.wrapper!.vm.$nextTick();
            await clickTool(world, 'toolbar-discard');
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
        pattern: /^no object is added$/,
        handler: world => {
            expect(world.onDetectedObjectAdded).not.toHaveBeenCalled();
        },
    },
    {
        pattern: /^the editor returns to select mode$/,
        handler: world => {
            expect((world.wrapper!.vm as any).activeMode).toBe('select');
        },
    },
    {
        pattern: /^the reviewer enters adjust mode, moves the box, and clicks Discard$/,
        handler: async world => {
            await selectBox(world, 'o1');
            await enterAdjust(world);
            dragSelectedBox(world, 50);
            await world.wrapper!.vm.$nextTick();
            await clickTool(world, 'toolbar-discard');
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the box is back at its original geometry$/,
        handler: world => {
            const obj = findObject(world, 'o1');
            expect(obj.x).toBe(0);
            expect(obj.y).toBe(0);
            expect(obj.width).toBe(20);
            expect(obj.height).toBe(30);
        },
    },
    {
        pattern: /^Vue sends no update$/,
        handler: world => {
            expect(world.onDetectedObjectUpdated).not.toHaveBeenCalled();
        },
    },
    {
        pattern: /^the reviewer enters adjust mode, moves the box, and presses Enter$/,
        handler: async world => {
            await selectBox(world, 'o1');
            await enterAdjust(world);
            dragSelectedBox(world, 50);
            await world.wrapper!.vm.$nextTick();
            pressKey('Enter');
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the reviewer enters adjust mode, moves the box, and presses Escape$/,
        handler: async world => {
            await selectBox(world, 'o1');
            await enterAdjust(world);
            dragSelectedBox(world, 50);
            await world.wrapper!.vm.$nextTick();
            pressKey('Escape');
            await world.wrapper!.vm.$nextTick();
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
        pattern: /^the reviewer cycles through select, add, and adjust modes$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            const toolbarElement = world.wrapper!.find('[data-testid="editor-toolbar"]').element;
            const frameSizeBefore = { ...vm.videoFrameSize };

            await clickTool(world, 'toolbar-add-object');
            await world.wrapper!.vm.$nextTick();
            await clickTool(world, 'toolbar-discard');
            await world.wrapper!.vm.$nextTick();
            await selectBox(world, 'o1');
            await enterAdjust(world);
            vm.handleDiscard();
            await world.wrapper!.vm.$nextTick();

            world.toolbarSnapshot = {
                frameSize: frameSizeBefore,
                sameToolbarNode: world.wrapper!.find('[data-testid="editor-toolbar"]').element === toolbarElement,
            };
        },
    },
    {
        pattern: /^the video frame size is unchanged$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.videoFrameSize).toEqual(world.toolbarSnapshot!.frameSize);
        },
    },
    {
        pattern: /^the toolbar stays in its fixed position$/,
        handler: world => {
            const toolbar = world.wrapper!.find('[data-testid="editor-toolbar"]');
            expect(toolbar.exists()).toBe(true);
            expect(world.toolbarSnapshot!.sameToolbarNode).toBe(true);
            expect(toolbar.element.parentElement).toBe(world.wrapper!.get('.workspace-main').element);
        },
    },
    {
        pattern: /^the selection is unchanged and the workspace click handler does not fire$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedKey).toBe('track-1');
            expect(vm.activeMode).toBe('add');
            expect(world.workspaceListener).not.toHaveBeenCalled();
        },
    },
    {
        pattern: /^the reviewer enters adjust mode on a rectangle-shaped box$/,
        handler: async world => {
            openEditor(world, rectangleShapeFrames());
            await selectBox(world, 'o1');
            await enterAdjust(world);
        },
    },
    {
        pattern: /^the adjust box has square corners and square handles$/,
        handler: world => {
            const box = world.wrapper!.get('[data-testid="adjust-box"]');
            expect(box.attributes('style') ?? '').not.toContain('border-radius');
            const handle = world.wrapper!.get('.adjust-handle--se');
            expect(handle.attributes('style')).toContain('width: 10px');
            expect(handle.attributes('style')).toContain('height: 10px');
        },
    },
    {
        pattern: /^the blur preview region is rectangular$/,
        handler: world => {
            const blur = world.wrapper!.get('.adjust-blur');
            expect(blur.classes()).toContain('adjust-blur--rectangle');
        },
    },
    {
        pattern: /^the reviewer enters adjust mode on an ellipse-shaped box and resizes from the east handle$/,
        handler: async world => {
            openEditor(world, defaultFrames());
            await selectBox(world, 'o1');
            await enterAdjust(world);
            const wrapper = world.wrapper!;
            const handle = wrapper.find('.adjust-handle--e');
            handle.trigger('mousedown', { clientX: 40, clientY: 10 });
            wrapper.find('.overlay').trigger('mousemove', { clientX: 70, clientY: 10 });
            wrapper.find('.overlay').trigger('mouseup');
        },
    },
    {
        pattern: /^the manipulation frame stays rectangular$/,
        handler: world => {
            const box = world.wrapper!.get('[data-testid="adjust-box"]');
            expect(box.attributes('style') ?? '').not.toContain('border-radius');
        },
    },
    {
        pattern: /^the blur preview region is elliptical$/,
        handler: world => {
            const blur = world.wrapper!.get('.adjust-blur');
            expect(blur.classes()).toContain('adjust-blur--ellipse');
            expect(blur.classes()).not.toContain('adjust-blur--rectangle');
        },
    },
    {
        pattern: /^the stored geometry changes as a rectangle$/,
        handler: world => {
            const obj = findObject(world, 'o1');
            expect(obj.width).toBeGreaterThan(20);
            expect(obj.height).toBe(30);
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

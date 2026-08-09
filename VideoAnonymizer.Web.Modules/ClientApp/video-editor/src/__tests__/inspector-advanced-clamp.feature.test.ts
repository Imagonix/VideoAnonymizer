import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { nextTick, reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import { computeVideoFrameSize } from '../utils/videoLayout';
import type { AnalyzedFrameDto, DetectedObjectDto, VideoEditorProps } from '../types';
import featureText from './inspector-advanced-clamp.feature?raw';

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
    positionBeforeAdvanced?: { top: number; left: number };
    heightBeforeAdvanced?: number;
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

function mountEditor(frames: AnalyzedFrameDto[] = [createFrame('f1', 0, [{ id: 'o1', trackId: 1 }])]) {
    const state = reactive({
        videoId: 'v1',
        videoSourceUrl: 'http://example.com/v.mp4',
        anonymizationSettings: { blurSizePercent: 200, timeBufferMs: 300, interpolateTrackedObjects: true },
        frames,
        onDetectedObjectUpdated: vi.fn(),
        onDetectedObjectsBulkUpdated: vi.fn(),
    } as VideoEditorProps);

    const wrapper = mount(VideoEditorApp, {
        props: { state },
        attachTo: document.body,
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
                    props: [
                        'objects',
                        'anonymizationSettings',
                        'videoDimensions',
                        'highlightedRowKey',
                        'splitSourceKey',
                        'alwaysShowKeys',
                        'selectedKey',
                        'mode',
                        'adjustObject',
                    ],
                },
            },
        },
    });

    return { wrapper, state };
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

async function selectAndEnsureInspector(world: World) {
    const vm = world.wrapper!.vm as any;
    const obj = world.state!.frames[0].detectedObjects[0];
    vm.selectObject(obj);
    await nextTick();
    if (typeof vm.ensureInitialInspectorPosition === 'function') {
        vm.ensureInitialInspectorPosition();
    }
    await nextTick();
    // Simulate painted height for collapsed inspector (no Advanced).
    if (typeof vm.measuredInspectorHeight !== 'undefined') {
        vm.measuredInspectorHeight = 340;
    }
    if (typeof vm.reclampRetainedInspectorPosition === 'function') {
        vm.reclampRetainedInspectorPosition();
    }
    await nextTick();
}

async function openAdvanced(world: World) {
    const vm = world.wrapper!.vm as any;
    world.positionBeforeAdvanced = vm.retainedInspectorPosition
        ? { ...vm.retainedInspectorPosition }
        : null as any;
    world.heightBeforeAdvanced = vm.measuredInspectorHeight ?? 340;

    const advancedBtn = world.wrapper!
        .findAll('button')
        .find(b => b.text().trim() === 'Advanced');
    expect(advancedBtn).toBeTruthy();
    await advancedBtn!.trigger('click');
    await nextTick();

    // Simulate post-render expanded height (three panels + Advanced menu).
    const expandedHeight = (world.heightBeforeAdvanced ?? 340) + 160;
    if (typeof vm.measuredInspectorHeight !== 'undefined') {
        vm.measuredInspectorHeight = expandedHeight;
    }
    // Mimic host layout-changed path after paint.
    if (typeof vm.onInspectorLayoutChanged === 'function') {
        // Bypass rAF delay: measure path already simulated above.
        if (typeof vm.reclampRetainedInspectorPosition === 'function') {
            vm.reclampRetainedInspectorPosition();
        }
    } else if (typeof vm.reclampRetainedInspectorPosition === 'function') {
        vm.reclampRetainedInspectorPosition();
    }
    await nextTick();
}

async function closeAdvanced(world: World) {
    const vm = world.wrapper!.vm as any;
    const advancedBtn = world.wrapper!
        .findAll('button')
        .find(b => b.text().trim() === 'Advanced');
    await advancedBtn!.trigger('click');
    await nextTick();
    // Collapsed height after close — position must not jump back.
    if (typeof vm.measuredInspectorHeight !== 'undefined') {
        vm.measuredInspectorHeight = world.heightBeforeAdvanced ?? 340;
    }
    if (typeof vm.reclampRetainedInspectorPosition === 'function') {
        vm.reclampRetainedInspectorPosition();
    }
    await nextTick();
}

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

const steps: StepDefinition[] = [
    {
        pattern: /^the editor is open with the inspector near the bottom of a short stage$/,
        handler: async world => {
            const mounted = mountEditor();
            world.wrapper = mounted.wrapper;
            world.state = mounted.state;
            // Short stage: expanded height will overflow without correction.
            setupStage(world, 900, 420);
            await selectAndEnsureInspector(world);
            const vm = world.wrapper.vm as any;
            // Place near bottom using collapsed height first.
            vm.measuredInspectorHeight = 340;
            vm.retainedInspectorPosition = { top: 420 - 340 - 8, left: 200 }; // top = 72
            vm.reclampRetainedInspectorPosition();
            await nextTick();
        },
    },
    {
        pattern: /^the editor is open with a manually placed inspector in the middle of a tall stage$/,
        handler: async world => {
            const mounted = mountEditor();
            world.wrapper = mounted.wrapper;
            world.state = mounted.state;
            setupStage(world, 900, 900);
            await selectAndEnsureInspector(world);
            const vm = world.wrapper.vm as any;
            vm.measuredInspectorHeight = 340;
            vm.retainedInspectorPosition = { top: 200, left: 150 };
            await nextTick();
        },
    },
    {
        pattern: /^the editor is open with the inspector on a very short stage$/,
        handler: async world => {
            const mounted = mountEditor();
            world.wrapper = mounted.wrapper;
            world.state = mounted.state;
            setupStage(world, 900, 200);
            await selectAndEnsureInspector(world);
            const vm = world.wrapper.vm as any;
            vm.measuredInspectorHeight = 340;
            vm.retainedInspectorPosition = { top: 8, left: 100 };
            await nextTick();
        },
    },
    {
        pattern: /^the reviewer opens Advanced$/,
        handler: async world => {
            await openAdvanced(world);
        },
    },
    {
        pattern: /^the inspector height is remeasured after Advanced has rendered$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.measuredInspectorHeight).toBeGreaterThan(world.heightBeforeAdvanced ?? 0);
            expect(world.wrapper!.find('[data-testid="advanced-menu"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the inspector is moved up only enough to keep the expanded group inside the stage$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            const stageH = vm.workspaceSize.height;
            const h = vm.measuredInspectorHeight;
            const top = vm.retainedInspectorPosition.top;
            const before = world.positionBeforeAdvanced!;
            // Corrected upward (or equal if already ok).
            expect(top).toBeLessThanOrEqual(before.top);
            // Fully inside stage (or pinned to margin when taller than stage).
            expect(top).toBeGreaterThanOrEqual(8);
            expect(top + Math.min(h, stageH - 16)).toBeLessThanOrEqual(stageH);
            // Minimal: uses clamp formula maxTop = stageH - h - margin.
            const maxTop = Math.max(8, stageH - h - 8);
            expect(top).toBe(Math.min(maxTop, Math.max(8, before.top)));
            // Horizontal position unchanged (no side flip).
            expect(vm.retainedInspectorPosition.left).toBe(before.left);
        },
    },
    {
        pattern: /^Advanced options remain reachable$/,
        handler: world => {
            const menu = world.wrapper!.find('[data-testid="advanced-menu"]');
            expect(menu.exists()).toBe(true);
            const labels = menu.findAll('button').map(b => b.text());
            expect(labels).toEqual(
                expect.arrayContaining(['Track forward', 'Merge', 'Split', 'Delete'])
            );
        },
    },
    {
        pattern: /^the inspector keeps its manual horizontal position$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.retainedInspectorPosition.left).toBe(world.positionBeforeAdvanced!.left);
        },
    },
    {
        pattern: /^the inspector does not recompute opposite-side placement$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            // Mid-stage left 150 must stay 150 (not auto right-edge 900-320-8).
            expect(vm.retainedInspectorPosition.left).toBe(150);
            expect(vm.retainedInspectorPosition.left).not.toBe(900 - 320 - 8);
        },
    },
    {
        pattern: /^the inspector group uses whole-group vertical scrolling$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            const scroll = vm.inspectorScrollStyle;
            expect(scroll).not.toBeNull();
            expect(scroll.overflowY).toBe('auto');
            expect(Number.parseFloat(scroll.maxHeight)).toBeLessThanOrEqual(
                vm.workspaceSize.height - 16
            );
        },
    },
    {
        pattern: /^the inspector is corrected for the expanded height$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            world.positionBeforeAdvanced = { ...vm.retainedInspectorPosition };
        },
    },
    {
        pattern: /^the reviewer closes Advanced$/,
        handler: async world => {
            await closeAdvanced(world);
        },
    },
    {
        pattern: /^the inspector does not jump back to the pre-expansion coordinates$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            // After close, height shrinks but top stays at the corrected value
            // (not moved back down to the original bottom-edge placement).
            expect(vm.retainedInspectorPosition.top).toBe(world.positionBeforeAdvanced!.top);
            expect(vm.retainedInspectorPosition.left).toBe(world.positionBeforeAdvanced!.left);
            expect(world.wrapper!.find('[data-testid="advanced-menu"]').exists()).toBe(false);
        },
    },
];

function runFeature(text: string, stepDefinitions: StepDefinition[]) {
    for (const scenario of parseFeature(text)) {
        it(scenario.name, async () => {
            const world: World = {};
            try {
                for (const step of scenario.steps) {
                    const definition = stepDefinitions
                        .map(candidate => ({ candidate, match: step.match(candidate.pattern) }))
                        .find(entry => entry.match);
                    if (!definition?.match) {
                        throw new Error(`No step definition found for: ${step}`);
                    }
                    await definition.candidate.handler(world, definition.match);
                }
            } finally {
                world.wrapper?.unmount();
            }
        });
    }
}

describe('Inspector Advanced expand clamp (Command 28)', () => {
    runFeature(featureText, steps);
});

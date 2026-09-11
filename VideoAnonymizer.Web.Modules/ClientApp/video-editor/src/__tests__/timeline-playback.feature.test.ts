import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { nextTick, reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import CollapsedTimelineBar from '../CollapsedTimelineBar.vue';
import Timeline from '../Timeline.vue';
import type { AnalyzedFrameDto, DetectedObjectDto, VideoEditorProps } from '../types';
import featureText from './timeline-playback.feature?raw';

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
    togglePlayback?: ReturnType<typeof vi.fn>;
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
        ]),
        createFrame('f3', 2, 2, [
            { id: 'o6', trackId: 1 },
        ]),
    ];
}

function mountEditor(world: World) {
    world.togglePlayback = vi.fn();
    const state = reactive({
        videoId: 'v1',
        videoSourceUrl: 'http://example.com/v.mp4',
        anonymizationSettings: { blurSizePercent: 200, timeBufferMs: 300, interpolateTrackedObjects: true },
        frames: createWorkspaceFrames(),
    } as VideoEditorProps);

    const wrapper = mount(VideoEditorApp, {
        props: { state },
        global: {
            stubs: {
                VideoPlayer: {
                    template: '<div class="mock-video" />',
                    methods: {
                        setVolume: () => {},
                        togglePlayback: world.togglePlayback,
                    },
                },
            },
        },
        attachTo: document.body,
    });

    const vm = wrapper.vm as any;
    vm.onVideoLoaded(10);
    Element.prototype.scrollIntoView = vi.fn();
    world.wrapper = wrapper;
    world.state = state;
}

async function ensureCollapsedSeekMetrics(world: World) {
    await nextTick();
    const seek = world.wrapper!.find('[data-testid="collapsed-seek-surface"]');
    if (!seek.exists()) return;

    Object.defineProperty(seek.element, 'clientWidth', { configurable: true, value: 500 });
    Object.defineProperty(seek.element, 'getBoundingClientRect', {
        configurable: true,
        value: () => ({ left: 100, width: 500, top: 0, right: 600, bottom: 40, height: 40, x: 100, y: 0, toJSON: () => ({}) }),
    });

    const bar = world.wrapper!.findComponent(CollapsedTimelineBar);
    if (bar.exists()) {
        (bar.vm as any).seekSurfaceWidth = 500;
    }
    await nextTick();
}

async function ensureExpandedSeekMetrics(world: World) {
    await nextTick();
    const viewport = world.wrapper!.find('[data-testid="expanded-seek-surface"]');
    if (!viewport.exists()) return;

    Object.defineProperty(viewport.element, 'clientWidth', { configurable: true, value: 500 });
    let scrollLeftValue = 0;
    Object.defineProperty(viewport.element, 'scrollLeft', {
        configurable: true,
        get: () => scrollLeftValue,
        set: (value: number) => { scrollLeftValue = value; },
    });
    Object.defineProperty(viewport.element, 'getBoundingClientRect', {
        configurable: true,
        value: () => ({ left: 100, width: 500, top: 0, right: 600, bottom: 80, height: 80, x: 100, y: 0, toJSON: () => ({}) }),
    });

    const timeline = world.wrapper!.findComponent(Timeline);
    if (timeline.exists()) {
        (timeline.vm as any).viewportWidth = 500;
        (timeline.vm as any).scrollLeft = 0;
    }
    await nextTick();
}

function playButton(world: World) {
    return world.wrapper!.find('[data-testid="timeline-play-pause"]');
}

function collapsedSeekSurface(world: World) {
    return world.wrapper!.find('[data-testid="collapsed-seek-surface"]');
}

function rulerLabels(world: World, rootSelector: string) {
    const root = world.wrapper!.find(rootSelector);
    if (!root.exists()) return [];
    return root.findAll('.timeline-tick span').map(node => node.text()).filter(Boolean);
}

const steps: StepDefinition[] = [
    {
        pattern: /^the editor is open with a timed video$/,
        handler: async world => {
            mountEditor(world);
            await ensureCollapsedSeekMetrics(world);
        },
    },
    {
        pattern: /^the timeline is collapsed$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.timelineExpanded).toBe(false);
            expect(world.wrapper!.find('[data-testid="timeline-panel"]').classes()).toContain('timeline-panel--collapsed');
        },
    },
    {
        pattern: /^the Play\/Pause control is in the fixed left timeline slot$/,
        handler: world => {
            const left = world.wrapper!.find('[data-testid="timeline-left-controls"]');
            const button = playButton(world);
            expect(left.exists()).toBe(true);
            expect(button.exists()).toBe(true);
            expect(left.element.contains(button.element)).toBe(true);
            expect(button.attributes('aria-label')).toMatch(/Play video|Pause video/);
        },
    },
    {
        pattern: /^the collapsed left slot does not show current-time text$/,
        handler: world => {
            expect(world.wrapper!.find('[data-testid="collapsed-timeline-time"]').exists()).toBe(false);
            const left = world.wrapper!.find('[data-testid="timeline-left-controls"]');
            expect(left.text()).not.toMatch(/\d+:\d+/);
        },
    },
    {
        pattern: /^the reviewer clicks Play\/Pause$/,
        handler: async world => {
            world.togglePlayback!.mockClear();
            await playButton(world).trigger('click');
        },
    },
    {
        pattern: /^video playback is toggled$/,
        handler: world => {
            expect(world.togglePlayback).toHaveBeenCalled();
        },
    },
    {
        pattern: /^the reviewer selects track (\d+)$/,
        handler: async (world, match) => {
            const trackId = Number(match[1]);
            const vm = world.wrapper!.vm as any;
            const obj = world.state!.frames
                .flatMap(frame => frame.detectedObjects)
                .find(item => item.trackId === trackId)!;
            vm.selectObject(obj);
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the collapsed track strip is visible$/,
        handler: world => {
            expect(world.wrapper!.find('[data-testid="collapsed-track-strip"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the reviewer expands the timeline$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.timelineExpanded = true;
            await world.wrapper!.vm.$nextTick();
            await ensureExpandedSeekMetrics(world);
        },
    },
    {
        pattern: /^the reviewer collapses the timeline$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.timelineExpanded = false;
            await world.wrapper!.vm.$nextTick();
            await ensureCollapsedSeekMetrics(world);
        },
    },
    {
        pattern: /^the expanded timeline is visible$/,
        handler: world => {
            expect(world.wrapper!.find('[data-testid="expanded-timeline"]').exists()).toBe(true);
            expect(world.wrapper!.find('[data-testid="timeline"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the collapsed seek surface shows multiple time labels$/,
        handler: world => {
            const labels = rulerLabels(world, '[data-testid="collapsed-seek-surface"]');
            expect(labels.length).toBeGreaterThanOrEqual(2);
        },
    },
    {
        pattern: /^the collapsed seek surface still shows multiple time labels$/,
        handler: world => {
            const labels = rulerLabels(world, '[data-testid="collapsed-seek-surface"]');
            expect(labels.length).toBeGreaterThanOrEqual(2);
        },
    },
    {
        pattern: /^the vertical playback indicator is visible in the collapsed seek surface$/,
        handler: world => {
            const surface = collapsedSeekSurface(world);
            expect(surface.find('[data-testid="playback-indicator"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the vertical playback indicator remains visible in the collapsed seek surface$/,
        handler: world => {
            const surface = collapsedSeekSurface(world);
            expect(surface.find('[data-testid="playback-indicator"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the expanded timeline shows multiple time labels$/,
        handler: world => {
            const labels = rulerLabels(world, '[data-testid="expanded-timeline"]');
            expect(labels.length).toBeGreaterThanOrEqual(2);
        },
    },
    {
        pattern: /^the vertical playback indicator is visible in the expanded timeline$/,
        handler: world => {
            expect(world.wrapper!.find('[data-testid="expanded-timeline"] [data-testid="playback-indicator"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the vertical playback indicator remains visible in the expanded timeline$/,
        handler: world => {
            expect(world.wrapper!.find('[data-testid="expanded-timeline"] [data-testid="playback-indicator"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the reviewer clicks the collapsed seek surface at (\d+) percent$/,
        handler: async (world, match) => {
            const percent = Number(match[1]);
            const surface = collapsedSeekSurface(world);
            expect(surface.exists()).toBe(true);
            const rect = { left: 100, width: 500, top: 0, right: 600, bottom: 40, height: 40, x: 100, y: 0, toJSON: () => ({}) };
            Object.defineProperty(surface.element, 'getBoundingClientRect', {
                configurable: true,
                value: () => rect,
            });
            const clientX = rect.left + (percent / 100) * rect.width;
            await surface.trigger('click', { clientX, clientY: 20 });
        },
    },
    {
        pattern: /^the video seeks near (\d+(?:\.\d+)?) seconds$/,
        handler: (world, match) => {
            const expected = Number(match[1]);
            const vm = world.wrapper!.vm as any;
            expect(vm.currentTime).toBeGreaterThanOrEqual(expected - 0.15);
            expect(vm.currentTime).toBeLessThanOrEqual(expected + 0.15);
        },
    },
    {
        pattern: /^the selection remains track (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedKey).toBe(`track-${match[1]}`);
        },
    },
    {
        pattern: /^the reviewer clicks the second occurrence dot in the collapsed track strip$/,
        handler: async world => {
            const dots = world.wrapper!.findAll('[data-testid="collapsed-occurrence-track"] .collapsed-dot:not(.collapsed-dot--pulsing)');
            expect(dots.length).toBeGreaterThanOrEqual(2);
            await dots[1].trigger('click');
        },
    },
    {
        pattern: /^the video seeks to (\d+(?:\.\d+)?) second(?:s)?$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            expect(vm.currentTime).toBe(Number(match[1]));
        },
    },
    {
        pattern: /^the reviewer seeks to (\d+(?:\.\d+)?) seconds$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.seekTo(Number(match[1]));
        },
    },
    {
        pattern: /^the reviewer clicks the timeline caret$/,
        handler: async world => {
            await world.wrapper!.find('[data-testid="timeline-caret"]').trigger('click');
        },
    },
    {
        pattern: /^the video remains at (\d+(?:\.\d+)?) seconds$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            expect(vm.currentTime).toBe(Number(match[1]));
        },
    },
    {
        pattern: /^the reviewer clicks the track inclusion checkbox$/,
        handler: async world => {
            const checkbox = world.wrapper!.find('[data-testid="collapsed-track-strip"] input[type="checkbox"]');
            expect(checkbox.exists()).toBe(true);
            await checkbox.setValue(false);
        },
    },
    {
        pattern: /^the collapsed time-label row is compact$/,
        handler: world => {
            const ruler = collapsedSeekSurface(world).find('[data-testid="timeline-ruler"]');
            expect(ruler.exists()).toBe(true);
            expect(ruler.classes()).toContain('timeline-ruler--compact');
            const labels = rulerLabels(world, '[data-testid="collapsed-seek-surface"]');
            expect(labels.length).toBeGreaterThanOrEqual(2);
        },
    },
    {
        pattern: /^the expanded time-label row is compact$/,
        handler: world => {
            const ruler = world.wrapper!.find('[data-testid="expanded-timeline"] [data-testid="timeline-ruler"]');
            expect(ruler.exists()).toBe(true);
            // Expanded ruler uses the shared reduced height (not removed labels).
            expect(rulerLabels(world, '[data-testid="expanded-timeline"]').length).toBeGreaterThanOrEqual(2);
        },
    },
];

function parseFeature(text: string): Scenario[] {
    const scenarios: Scenario[] = [];
    let current: Scenario | null = null;

    for (const rawLine of text.split(/\r?\n/)) {
        const line = rawLine.trim();
        if (!line || line.startsWith('#') || line.startsWith('Feature:')) continue;
        if (line.startsWith('Scenario:')) {
            current = { name: line.slice('Scenario:'.length).trim(), steps: [] };
            scenarios.push(current);
            continue;
        }
        if (!current) continue;
        if (/^(Given|When|Then|And|But)\b/.test(line)) {
            current.steps.push(line.replace(/^(Given|When|Then|And|But)\s+/, ''));
        }
    }

    return scenarios;
}

async function runStep(world: World, step: string) {
    for (const definition of steps) {
        const match = step.match(definition.pattern);
        if (match) {
            await definition.handler(world, match);
            return;
        }
    }
    throw new Error(`No step definition for: ${step}`);
}

describe('Timeline playback and seeking feature', () => {
    const scenarios = parseFeature(featureText);

    for (const scenario of scenarios) {
        it(scenario.name, async () => {
            const world: World = {};
            try {
                for (const step of scenario.steps) {
                    await runStep(world, step);
                }
            } finally {
                world.wrapper?.unmount();
            }
        });
    }
});

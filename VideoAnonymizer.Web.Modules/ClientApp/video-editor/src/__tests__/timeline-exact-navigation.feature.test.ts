import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { nextTick, reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import CollapsedTimelineBar from '../CollapsedTimelineBar.vue';
import type { AnalyzedFrameDto, DetectedObjectDto, VideoEditorProps } from '../types';
import featureText from './timeline-exact-navigation.feature?raw';

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

function consecutiveFrames() {
    return [
        createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1 }]),
        createFrame('f2', 1, 1, [{ id: 'o3', trackId: 1 }]),
        createFrame('f3', 2, 2, [{ id: 'o6', trackId: 1 }]),
    ];
}

function gapFrames() {
    return [
        createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1 }]),
        createFrame('f2', 1, 1, [{ id: 'oX', trackId: null }]),
        createFrame('f3', 2, 2, [{ id: 'o3', trackId: 1 }]),
    ];
}

function closeFrames() {
    return [
        createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1 }]),
        createFrame('f2', 1, 0.04, [{ id: 'o2', trackId: 1 }]),
    ];
}

function mountEditor(world: World, frames: AnalyzedFrameDto[]) {
    const state = reactive({
        videoId: 'v1',
        videoSourceUrl: 'http://example.com/v.mp4',
        anonymizationSettings: { blurSizePercent: 200, timeBufferMs: 300, interpolateTrackedObjects: true },
        frames,
    } as VideoEditorProps);

    const wrapper = mount(VideoEditorApp, {
        props: { state },
        global: {
            stubs: {
                VideoPlayer: {
                    template: '<div class="mock-video" />',
                    methods: { setVolume: () => {}, togglePlayback: () => {} },
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

async function openEditor(world: World, frames: AnalyzedFrameDto[] = consecutiveFrames()) {
    mountEditor(world, frames);
    await ensureCollapsedSeekMetrics(world);
}

async function ensureCollapsedSeekMetrics(world: World) {
    await nextTick();
    const seek = world.wrapper!.find('[data-testid="collapsed-seek-surface"]');
    if (seek.exists()) {
        Object.defineProperty(seek.element, 'clientWidth', { configurable: true, value: 500 });
        Object.defineProperty(seek.element, 'getBoundingClientRect', {
            configurable: true,
            value: () => ({ left: 100, width: 500, top: 0, right: 600, bottom: 40, height: 40, x: 100, y: 0, toJSON: () => ({}) }),
        });
        const bar = world.wrapper!.findComponent(CollapsedTimelineBar);
        if (bar.exists()) {
            (bar.vm as any).seekSurfaceWidth = 500;
        }
    }
    await nextTick();
}

function trackOccurrences(world: World, trackId: number): DetectedObjectDto[] {
    const vm = world.wrapper!.vm as any;
    return vm.getFrames()
        .flatMap((frame: AnalyzedFrameDto) => frame.detectedObjects)
        .filter((obj: DetectedObjectDto) => obj.trackId === trackId);
}

async function selectTrackOccurrence(world: World, trackId: number, index: number) {
    const occurrences = trackOccurrences(world, trackId);
    const vm = world.wrapper!.vm as any;
    vm.selectObject(occurrences[index]);
    await world.wrapper!.vm.$nextTick();
}

async function navigateNext(world: World) {
    const vm = world.wrapper!.vm as any;
    vm.goToNextOccurrence();
    await world.wrapper!.vm.$nextTick();
}

async function navigatePrevious(world: World) {
    const vm = world.wrapper!.vm as any;
    vm.goToPreviousOccurrence();
    await world.wrapper!.vm.$nextTick();
}

function rulerLabels(world: World) {
    const surface = world.wrapper!.find('[data-testid="collapsed-seek-surface"]');
    if (!surface.exists()) return [];
    return surface.findAll('.timeline-tick span').map(node => node.text()).filter(Boolean);
}

const steps: StepDefinition[] = [
    {
        pattern: /^the editor is open in the video-first workspace$/,
        handler: async world => {
            await openEditor(world, consecutiveFrames());
        },
    },
    {
        pattern: /^the editor is open with a timed video$/,
        handler: async world => {
            await openEditor(world, consecutiveFrames());
        },
    },
    {
        pattern: /^the editor is open with consecutive track occurrences at 0, 1, and 2 seconds$/,
        handler: async world => {
            await openEditor(world, consecutiveFrames());
        },
    },
    {
        pattern: /^the editor is open with track occurrences at 0 and 2 seconds and a missing frame between$/,
        handler: async world => {
            await openEditor(world, gapFrames());
        },
    },
    {
        pattern: /^the editor is open with track occurrences at 0 and 0.04 seconds$/,
        handler: async world => {
            await openEditor(world, closeFrames());
        },
    },
    {
        pattern: /^the collapsed timeline shows one compact row without track metadata$/,
        handler: world => {
            expect(world.wrapper!.findAll('[data-testid="collapsed-row"]')).toHaveLength(1);
            expect(world.wrapper!.find('[data-testid="collapsed-track-strip"]').exists()).toBe(false);
            expect(world.wrapper!.find('[data-testid="collapsed-seek-surface"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the reviewer selects track (\d+)$/,
        handler: async (world, match) => {
            await selectTrackOccurrence(world, Number(match[1]), 0);
        },
    },
    {
        pattern: /^the track metadata sits inline in the same single row$/,
        handler: world => {
            const meta = world.wrapper!.find('[data-testid="collapsed-track-strip"]');
            const surface = world.wrapper!.find('[data-testid="collapsed-seek-surface"]');
            expect(meta.exists()).toBe(true);
            expect(surface.exists()).toBe(true);
            expect(meta.element.parentElement).toBe(surface.element.parentElement);
            expect(meta.element.parentElement!.matches('[data-testid="collapsed-row"]')).toBe(true);
        },
    },
    {
        pattern: /^the collapsed timeline keeps its fixed height$/,
        handler: world => {
            expect(world.wrapper!.findAll('[data-testid="collapsed-row"]')).toHaveLength(1);
            const row = world.wrapper!.find('[data-testid="collapsed-row"]');
            const meta = world.wrapper!.find('[data-testid="collapsed-track-strip"]');
            const surface = world.wrapper!.find('[data-testid="collapsed-seek-surface"]');
            expect(meta.element.parentElement).toBe(row.element);
            expect(surface.element.parentElement).toBe(row.element);
        },
    },
    {
        pattern: /^the collapsed track strip shows a small thumbnail, inclusion checkbox, color marker, and an ellipsized non-wrapping label$/,
        handler: world => {
            const strip = world.wrapper!.find('[data-testid="collapsed-track-strip"]');
            const thumbnail = strip.find('[data-testid="track-thumbnail"]');
            expect(thumbnail.exists()).toBe(true);
            expect(thumbnail.attributes('style')).toContain('width: 20px');
            expect(thumbnail.attributes('style')).toContain('height: 20px');
            expect(strip.find('input[type="checkbox"]').exists()).toBe(true);
            expect(strip.find('.track-color-dot').exists()).toBe(true);
            const label = strip.find('[data-testid="collapsed-track-label"]');
            expect(label.exists()).toBe(true);
            expect(label.classes()).toContain('track-label');
        },
    },
    {
        pattern: /^the collapsed seek surface shows multiple time labels$/,
        handler: world => {
            expect(rulerLabels(world).length).toBeGreaterThanOrEqual(2);
        },
    },
    {
        pattern: /^the vertical playback indicator remains visible in the collapsed seek surface$/,
        handler: world => {
            const surface = world.wrapper!.find('[data-testid="collapsed-seek-surface"]');
            expect(surface.find('[data-testid="playback-indicator"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the collapsed track metadata is inline beside the seek surface$/,
        handler: world => {
            const meta = world.wrapper!.find('[data-testid="collapsed-track-strip"]');
            const surface = world.wrapper!.find('[data-testid="collapsed-seek-surface"]');
            expect(meta.element.parentElement).toBe(surface.element.parentElement);
        },
    },
    {
        pattern: /^the reviewer clicks the collapsed seek surface at (\d+) percent$/,
        handler: async (world, match) => {
            const percent = Number(match[1]);
            const surface = world.wrapper!.find('[data-testid="collapsed-seek-surface"]');
            const clientX = 100 + (percent / 100) * 500;
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
        pattern: /^the reviewer seeks to (\d+(?:\.\d+)?) seconds$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.seekTo(Number(match[1]));
        },
    },
    {
        pattern: /^the reviewer clicks the collapsed track label$/,
        handler: async world => {
            await world.wrapper!.find('[data-testid="collapsed-track-label"]').trigger('click');
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
        pattern: /^the reviewer selects the first occurrence of track (\d+)$/,
        handler: async (world, match) => {
            await selectTrackOccurrence(world, Number(match[1]), 0);
        },
    },
    {
        pattern: /^the reviewer navigates to the last occurrence of track (\d+)$/,
        handler: async (world, match) => {
            const trackId = Number(match[1]);
            const occurrences = trackOccurrences(world, trackId);
            await selectTrackOccurrence(world, trackId, 0);
            const vm = world.wrapper!.vm as any;
            let guard = 0;
            while (vm.canGoNext && guard < 100) {
                await navigateNext(world);
                guard++;
            }
            expect(vm.selectedOccurrence?.id).toBe(occurrences[occurrences.length - 1].id);
        },
    },
    {
        pattern: /^the reviewer navigates to the next occurrence$/,
        handler: async world => {
            await navigateNext(world);
        },
    },
    {
        pattern: /^the reviewer navigates to the previous occurrence$/,
        handler: async world => {
            await navigatePrevious(world);
        },
    },
    {
        pattern: /^the video time matches the adjacent stored occurrence at (\d+(?:\.\d+)?) second(?:s)?$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            expect(vm.currentTime).toBe(Number(match[1]));
        },
    },
    {
        pattern: /^Previous is disabled and Next is enabled$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.canGoPrevious).toBe(false);
            expect(vm.canGoNext).toBe(true);
        },
    },
    {
        pattern: /^Previous is enabled and Next is disabled$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.canGoPrevious).toBe(true);
            expect(vm.canGoNext).toBe(false);
        },
    },
    {
        pattern: /^the inspector occurrence context matches the stored occurrence at (\d+(?:\.\d+)?) second(?:s)?$/,
        handler: (world, match) => {
            const expected = Number(match[1]);
            const vm = world.wrapper!.vm as any;
            const occurrences = trackOccurrences(world, 1);
            const target = occurrences.find(obj => {
                const frame = world.state!.frames.find(f => f.id === obj.analyzedFrameId);
                return frame?.timeSeconds === expected;
            });
            expect(target).toBeTruthy();
            expect(vm.selectedOccurrence?.id).toBe(target!.id);
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

describe('Exact occurrence navigation and compact collapsed timeline', () => {
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

import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import type { AnalyzedFrameDto, DetectedObjectDto, VideoEditorProps } from '../types';
import featureText from './timeline-collapse.feature?raw';

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
            },
        },
        attachTo: document.body,
    });

    const vm = wrapper.vm as any;
    vm.onVideoLoaded(10);
    Element.prototype.scrollIntoView = vi.fn();

    return { wrapper, state };
}

function openEditor(world: World) {
    const mounted = mountEditor();
    world.wrapper = mounted.wrapper;
    world.state = mounted.state;
}

const steps: StepDefinition[] = [
    {
        pattern: /^the editor is open in the video-first workspace$/,
        handler: world => openEditor(world),
    },
    {
        pattern: /^the timeline is collapsed$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.timelineExpanded).toBe(false);
            expect(world.wrapper!.find('[data-testid="timeline-panel"]').classes()).toContain('timeline-panel--collapsed');
            expect(world.wrapper!.find('[data-testid="expanded-timeline"]').exists()).toBe(false);
        },
    },
    {
        pattern: /^the timeline is expanded$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.timelineExpanded).toBe(true);
            expect(world.wrapper!.find('[data-testid="timeline-panel"]').classes()).toContain('timeline-panel--expanded');
            expect(world.wrapper!.find('[data-testid="expanded-timeline"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the caret is labeled Expand timeline with aria-expanded false$/,
        handler: world => {
            const caret = world.wrapper!.find('[data-testid="timeline-caret"]');
            expect(caret.exists()).toBe(true);
            expect(caret.attributes('aria-expanded')).toBe('false');
            expect(caret.attributes('aria-label')).toBe('Expand timeline');
            expect(caret.attributes('title')).toBe('Expand timeline');
        },
    },
    {
        pattern: /^the caret is labeled Collapse timeline with aria-expanded true$/,
        handler: world => {
            const caret = world.wrapper!.find('[data-testid="timeline-caret"]');
            expect(caret.exists()).toBe(true);
            expect(caret.attributes('aria-expanded')).toBe('true');
            expect(caret.attributes('aria-label')).toBe('Collapse timeline');
            expect(caret.attributes('title')).toBe('Collapse timeline');
        },
    },
    {
        pattern: /^the reviewer clicks the timeline caret$/,
        handler: async world => {
            await world.wrapper!.find('[data-testid="timeline-caret"]').trigger('click');
        },
    },
    {
        pattern: /^the collapsed overview is visible$/,
        handler: world => {
            expect(world.wrapper!.find('[data-testid="collapsed-overview"]').exists()).toBe(true);
            expect(world.wrapper!.find('[data-testid="timeline-overview"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the collapsed overview is not visible$/,
        handler: world => {
            expect(world.wrapper!.find('[data-testid="collapsed-overview"]').exists()).toBe(false);
        },
    },
    {
        pattern: /^the collapsed track strip is visible$/,
        handler: world => {
            expect(world.wrapper!.find('[data-testid="collapsed-track-strip"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the collapsed track strip is not visible$/,
        handler: world => {
            expect(world.wrapper!.find('[data-testid="collapsed-track-strip"]').exists()).toBe(false);
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
        pattern: /^the collapsed track strip shows the track label and inclusion control$/,
        handler: world => {
            const strip = world.wrapper!.find('[data-testid="collapsed-track-strip"]');
            expect(strip.exists()).toBe(true);
            expect(strip.text()).toMatch(/face\s+1|face 1/i);
            expect(strip.find('input[type="checkbox"]').exists()).toBe(true);
            expect(strip.find('[data-testid="track-thumb-placeholder"]').exists()).toBe(true);
            // Occurrence dots live on the shared seek surface under the track meta row.
            expect(world.wrapper!.find('[data-testid="collapsed-occurrence-track"]').exists()).toBe(true);
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
        pattern: /^the video seeks to the second occurrence time of track 1$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.currentTime).toBe(1);
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
        pattern: /^the reviewer expands the timeline$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.timelineExpanded = true;
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the reviewer collapses the timeline$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.timelineExpanded = false;
            await world.wrapper!.vm.$nextTick();
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
        pattern: /^the selected track row is highlighted for track (\d+)$/,
        handler: (world, match) => {
            const key = `track-${match[1]}`;
            const labels = world.wrapper!.findAll(`.label-container[data-timeline-key="${key}"]`);
            const rows = world.wrapper!.findAll(`.timeline-row-wrapper[data-timeline-key="${key}"]`);
            expect(labels.length).toBeGreaterThan(0);
            expect(rows.length).toBeGreaterThan(0);
            expect(labels[0].attributes('data-selected')).toBe('true');
            expect(rows[0].attributes('data-selected')).toBe('true');
            expect(labels[0].classes()).toContain('label-container--selected');
            expect(rows[0].classes()).toContain('timeline-row-wrapper--selected');
            expect(Element.prototype.scrollIntoView).toHaveBeenCalled();
        },
    },
    {
        pattern: /^the reviewer clicks the expanded label for track (\d+)$/,
        handler: async (world, match) => {
            const key = `track-${match[1]}`;
            const label = world.wrapper!.find(`.label-container[data-timeline-key="${key}"]`);
            expect(label.exists()).toBe(true);
            await label.trigger('click');
        },
    },
    {
        pattern: /^the object details panel is open$/,
        handler: world => {
            expect(world.wrapper!.find('[data-testid="object-details-panel"]').exists()).toBe(true);
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

describe('Timeline collapse feature', () => {
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

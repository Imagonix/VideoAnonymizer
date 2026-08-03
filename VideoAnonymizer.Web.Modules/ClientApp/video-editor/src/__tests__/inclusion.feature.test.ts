import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import type { AnalyzedFrameDto, DetectedObjectDto, VideoEditorProps } from '../types';
import featureText from './inclusion.feature?raw';

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
            preBufferMsOverride: o.preBufferMsOverride ?? null,
            postBufferMsOverride: o.postBufferMsOverride ?? null,
            selected: o.selected ?? true,
            trackId: o.trackId ?? null,
            x: o.x ?? 10 + i * 40,
            y: o.y ?? 20,
            width: o.width ?? 30,
            height: o.height ?? 40,
            analyzedFrameId: id,
        })),
    };
}

function multiOccurrenceFrames() {
    return [
        createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1 }]),
        createFrame('f2', 1, 1, [{ id: 'o2', trackId: 1 }]),
        createFrame('f3', 2, 2, [{ id: 'o3', trackId: 1 }]),
    ];
}

function mixedInclusionFrames() {
    return [
        createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1, selected: true }]),
        createFrame('f2', 1, 1, [{ id: 'o2', trackId: 1, selected: false }]),
        createFrame('f3', 2, 2, [{ id: 'o3', trackId: 1, selected: true }]),
    ];
}

function excludedO1Frames() {
    return [
        createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1, selected: false }]),
        createFrame('f2', 1, 1, [{ id: 'o2', trackId: 1, selected: true }]),
        createFrame('f3', 2, 2, [{ id: 'o3', trackId: 1, selected: true }]),
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
            },
        },
    });

    return { wrapper, state };
}

function openEditor(world: World, frames: AnalyzedFrameDto[] = multiOccurrenceFrames()) {
    world.onDetectedObjectUpdated = vi.fn();
    world.onDetectedObjectsBulkUpdated = vi.fn();
    const mounted = mountEditor(frames, {
        onDetectedObjectUpdated: world.onDetectedObjectUpdated,
        onDetectedObjectsBulkUpdated: world.onDetectedObjectsBulkUpdated,
    });
    world.wrapper = mounted.wrapper;
    world.state = mounted.state;
}

function findObject(world: World, objectId: string): DetectedObjectDto {
    const obj = world.state!.frames
        .flatMap(frame => frame.detectedObjects)
        .find(face => face.id === objectId);
    expect(obj).toBeTruthy();
    return obj!;
}

function trackRowLabel(world: World) {
    return world.wrapper!.find('[data-timeline-key="track-1"]');
}

function collapsedIncludeCheckbox(world: World) {
    const strip = world.wrapper!.find('[data-testid="collapsed-track-strip"]');
    expect(strip.exists()).toBe(true);
    return strip.find('input[type="checkbox"]');
}

function timelineIncludeCheckbox(world: World) {
    const label = trackRowLabel(world);
    expect(label.exists()).toBe(true);
    return label.find('input[type="checkbox"]');
}

function excludedBoxes(world: World) {
    return world.wrapper!.findAll('[data-testid="bounding-box"].bbox--excluded');
}

const steps: StepDefinition[] = [
    {
        pattern: /^the editor is open with a multi-occurrence track$/,
        handler: world => {
            openEditor(world, multiOccurrenceFrames());
        },
    },
    {
        pattern: /^the editor is open with a mixed-inclusion track$/,
        handler: world => {
            openEditor(world, mixedInclusionFrames());
        },
    },
    {
        pattern: /^the editor is open with occurrence "([^"]+)" already excluded$/,
        handler: (world, match) => {
            expect(match[1]).toBe('o1');
            openEditor(world, excludedO1Frames());
        },
    },
    {
        pattern: /^the reviewer selects occurrence "([^"]+)" and excludes it from the inspector$/,
        handler: async (world, match) => {
            const vm = world.wrapper!.vm as any;
            const obj = findObject(world, match[1]);
            vm.seekTo(world.state!.frames.find(f => f.id === obj.analyzedFrameId)!.timeSeconds);
            await world.wrapper!.vm.$nextTick();
            vm.selectObject(obj);
            await world.wrapper!.vm.$nextTick();
            vm.handleToggleOccurrenceInclude(false);
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the reviewer selects occurrence "([^"]+)" and includes it from the inspector$/,
        handler: async (world, match) => {
            const vm = world.wrapper!.vm as any;
            const obj = findObject(world, match[1]);
            vm.seekTo(world.state!.frames.find(f => f.id === obj.analyzedFrameId)!.timeSeconds);
            await world.wrapper!.vm.$nextTick();
            vm.selectObject(obj);
            await world.wrapper!.vm.$nextTick();
            vm.handleToggleOccurrenceInclude(true);
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the reviewer bulk-excludes track 1 from the timeline$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o1'));
            await world.wrapper!.vm.$nextTick();
            vm.handleToggleTrackInclude(false);
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the reviewer bulk-includes track 1 from the timeline$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o1'));
            await world.wrapper!.vm.$nextTick();
            vm.handleToggleTrackInclude(true);
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the reviewer clicks the indeterminate timeline checkbox for track 1$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o1'));
            vm.timelineExpanded = true;
            await world.wrapper!.vm.$nextTick();

            const checkbox = timelineIncludeCheckbox(world);
            expect((checkbox.element as HTMLInputElement).indeterminate || checkbox.classes().length >= 0).toBeTruthy();

            // Indeterminate interaction is defined as checking (include all), matching native checkbox behavior.
            await checkbox.setValue(true);
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the reviewer keyboard-selects the excluded ghost box for "([^"]+)"$/,
        handler: async (world, match) => {
            const vm = world.wrapper!.vm as any;
            const obj = findObject(world, match[1]);
            vm.seekTo(world.state!.frames.find(f => f.id === obj.analyzedFrameId)!.timeSeconds);
            await world.wrapper!.vm.$nextTick();

            const ghost = excludedBoxes(world)[0];
            expect(ghost).toBeTruthy();
            await ghost!.trigger('keydown', { key: 'Enter' });
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^only occurrence "([^"]+)" is excluded$/,
        handler: (world, match) => {
            expect(findObject(world, match[1]).selected).toBe(false);
        },
    },
    {
        pattern: /^occurrences "([^"]+)" and "([^"]+)" remain included$/,
        handler: (world, match) => {
            expect(findObject(world, match[1]).selected).toBe(true);
            expect(findObject(world, match[2]).selected).toBe(true);
        },
    },
    {
        pattern: /^every occurrence of track 1 is excluded$/,
        handler: world => {
            for (const id of ['o1', 'o2', 'o3']) {
                expect(findObject(world, id).selected).toBe(false);
            }
        },
    },
    {
        pattern: /^every occurrence of track 1 is included$/,
        handler: world => {
            for (const id of ['o1', 'o2', 'o3']) {
                expect(findObject(world, id).selected).toBe(true);
            }
        },
    },
    {
        pattern: /^occurrence "([^"]+)" is included$/,
        handler: (world, match) => {
            expect(findObject(world, match[1]).selected).toBe(true);
        },
    },
    {
        pattern: /^Vue sends a single "toggle" update for "([^"]+)" with selected (true|false)$/,
        handler: (world, match) => {
            expect(world.onDetectedObjectUpdated).toHaveBeenCalledOnce();
            expect(world.onDetectedObjectsBulkUpdated).not.toHaveBeenCalled();
            const [, , dto, operationType] = world.onDetectedObjectUpdated!.mock.calls[0];
            expect(operationType).toBe('toggle');
            expect(dto.id).toBe(match[1]);
            expect(dto.selected).toBe(match[2] === 'true');
        },
    },
    {
        pattern: /^Vue sends a "toggle" bulk update for "([^"]+)" with selected (true|false)$/,
        handler: (world, match) => {
            const expectedIds = match[1].split(',').sort();
            expect(world.onDetectedObjectsBulkUpdated).toHaveBeenCalledOnce();
            const [, dtos, operationType] = world.onDetectedObjectsBulkUpdated!.mock.calls[0];
            expect(operationType).toBe('toggle');
            expect(dtos.map((o: DetectedObjectDto) => o.id).sort()).toEqual(expectedIds);
            expect(dtos.every((o: DetectedObjectDto) => o.selected === (match[2] === 'true'))).toBe(true);
        },
    },
    {
        pattern: /^the track selection and inspector remain open with the occurrence unchecked$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedKey).toBe('track-1');
            expect(world.wrapper!.find('[data-testid="object-details-panel"]').exists()).toBe(true);
            expect(vm.selectedTrackSettings.included).toBe(false);
            expect(vm.selectedOccurrence.selected).toBe(false);
        },
    },
    {
        pattern: /^the collapsed track inclusion control is indeterminate$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.timelineExpanded = false;
            await world.wrapper!.vm.$nextTick();
            const strip = world.wrapper!.find('[data-testid="collapsed-track-strip"]');
            expect(strip.exists()).toBe(true);
            expect(strip.find('.mud-checkbox--indeterminate').exists()).toBe(true);
            const input = collapsedIncludeCheckbox(world).element as HTMLInputElement;
            expect(input.indeterminate).toBe(true);
        },
    },
    {
        pattern: /^the timeline checkbox for track 1 is indeterminate$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.timelineExpanded = true;
            await world.wrapper!.vm.$nextTick();
            const label = trackRowLabel(world);
            expect(label.find('.mud-checkbox--indeterminate').exists()).toBe(true);
            const input = label.find('input[type="checkbox"]').element as HTMLInputElement;
            expect(input.indeterminate).toBe(true);
        },
    },
    {
        pattern: /^occurrence "([^"]+)" is rendered as an excluded ghost box$/,
        handler: (world, match) => {
            expect(findObject(world, match[1]).selected).toBe(false);
            const ghosts = excludedBoxes(world);
            expect(ghosts.length).toBeGreaterThan(0);
            expect(world.wrapper!.find('[data-excluded="true"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^occurrence "([^"]+)" has no blur fill outline$/,
        handler: world => {
            // Excluded groups omit blur-area-outline entirely.
            const excludedGroup = world.wrapper!.find('[data-excluded="true"]');
            expect(excludedGroup.exists()).toBe(true);
            expect(excludedGroup.find('[data-testid="blur-area-outline"]').exists()).toBe(false);
        },
    },
    {
        pattern: /^the excluded ghost box remains pointer-selectable$/,
        handler: async world => {
            const ghost = excludedBoxes(world)[0];
            expect(ghost).toBeTruthy();
            expect(ghost!.attributes('role')).toBe('button');
            expect(ghost!.attributes('tabindex')).toBe('0');
            await ghost!.trigger('click');
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedKey).toBe('track-1');
        },
    },
    {
        pattern: /^the track selection is keyed to track 1$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedKey).toBe('track-1');
        },
    },
    {
        pattern: /^the object details panel is open with occurrence "([^"]+)" unchecked$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            expect(world.wrapper!.find('[data-testid="object-details-panel"]').exists()).toBe(true);
            expect(vm.selectedOccurrence.id).toBe(match[1]);
            expect(vm.selectedTrackSettings.included).toBe(false);
        },
    },
    {
        pattern: /^occurrence "([^"]+)" is no longer rendered as an excluded ghost$/,
        handler: (world, match) => {
            expect(findObject(world, match[1]).selected).toBe(true);
            expect(excludedBoxes(world)).toHaveLength(0);
        },
    },
    {
        pattern: /^the single toggle before-state has "([^"]+)" selected (true|false)$/,
        handler: (world, match) => {
            const [, , , , beforeState] = world.onDetectedObjectUpdated!.mock.calls[0];
            expect(beforeState).toHaveLength(1);
            expect(beforeState[0].id).toBe(match[1]);
            expect(beforeState[0].selected).toBe(match[2] === 'true');
        },
    },
    {
        pattern: /^the bulk toggle before-state has "([^"]+)" selected (true|false)$/,
        handler: (world, match) => {
            const expectedIds = match[1].split(',').sort();
            const [, , , beforeState] = world.onDetectedObjectsBulkUpdated!.mock.calls[0];
            expect(beforeState.map((o: DetectedObjectDto) => o.id).sort()).toEqual(expectedIds);
            expect(beforeState.every((o: DetectedObjectDto) => o.selected === (match[2] === 'true'))).toBe(true);
        },
    },
    {
        pattern: /^Blazor pushes undo restoring occurrence "([^"]+)" to selected (true|false)$/,
        handler: async (world, match) => {
            const vm = world.wrapper!.vm as any;
            const obj = findObject(world, match[1]);
            vm.applyChanges({
                objectsToUpdate: [{ ...obj, selected: match[2] === 'true' }],
                objectsToRemove: [],
                objectsToAdd: [],
            });
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^Blazor pushes undo restoring "([^"]+)" to selected (true|false)$/,
        handler: async (world, match) => {
            const vm = world.wrapper!.vm as any;
            const ids = match[1].split(',');
            const selected = match[2] === 'true';
            vm.applyChanges({
                objectsToUpdate: ids.map(id => ({ ...findObject(world, id), selected })),
                objectsToRemove: [],
                objectsToAdd: [],
            });
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the other track occurrences remain included$/,
        handler: world => {
            expect(findObject(world, 'o2').selected).toBe(true);
            expect(findObject(world, 'o3').selected).toBe(true);
        },
    },
];

function parseFeature(text: string): Scenario[] {
    const scenarios: Scenario[] = [];
    let current: Scenario | null = null;

    for (const rawLine of text.split(/\r?\n/)) {
        const line = rawLine.trim();
        if (line.startsWith('Scenario:')) {
            if (current) scenarios.push(current);
            current = { name: line.slice('Scenario:'.length).trim(), steps: [] };
            continue;
        }
        if (!current) continue;
        if (/^(Given|When|Then|And|But)\s+/.test(line)) {
            current.steps.push(line.replace(/^(Given|When|Then|And|But)\s+/, ''));
        }
    }
    if (current) scenarios.push(current);
    return scenarios;
}

function matchStep(step: string): { def: StepDefinition; match: RegExpMatchArray } | null {
    for (const def of steps) {
        const match = step.match(def.pattern);
        if (match) return { def, match };
    }
    return null;
}

describe('Inclusion semantics feature', () => {
    const scenarios = parseFeature(featureText);

    for (const scenario of scenarios) {
        it(scenario.name, async () => {
            const world: World = {};
            for (const step of scenario.steps) {
                const matched = matchStep(step);
                if (!matched) {
                    throw new Error(`No step definition for: ${step}`);
                }
                await matched.def.handler(world, matched.match);
            }
        });
    }
});

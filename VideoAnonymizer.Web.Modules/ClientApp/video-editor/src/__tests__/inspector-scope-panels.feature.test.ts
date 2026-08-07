import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import { computeVideoFrameSize } from '../utils/videoLayout';
import type { AnalyzedFrameDto, DetectedObjectDto, VideoEditorProps } from '../types';
import featureText from './inspector-scope-panels.feature?raw';

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
    manualPosition?: { top: number; left: number };
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
            width: o.width ?? 100,
            height: o.height ?? 50,
            analyzedFrameId: id,
        })),
    };
}

function trackedOccurrenceFrames() {
    return [createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1 }])];
}

function twoOccurrenceTrackFrames() {
    return [
        createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1, x: 100 }]),
        createFrame('f2', 1, 1, [{ id: 'o2', trackId: 1, x: 1400 }]),
    ];
}

function interiorSegmentFrames() {
    return [
        createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1, preBufferMsOverride: 450 }]),
        createFrame('f2', 1, 1, [{ id: 'o2', trackId: 1 }]),
        createFrame('f3', 2, 2, [{ id: 'o3', trackId: 1 }]),
    ];
}

function mixedSegmentFrames() {
    return [
        createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1, preBufferMsOverride: 500 }]),
        createFrame('f2', 1, 1, [{ id: 'o2', trackId: 1 }]),
        createFrame('f3', 2, 2, [{ id: 'oX', trackId: null }]),
        createFrame('f4', 3, 3, [{ id: 'o4', trackId: 1, postBufferMsOverride: 600 }]),
    ];
}

function commonTimeBufferFrames() {
    return [
        createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1, preBufferMsOverride: 500 }]),
        createFrame('f2', 1, 1, [{ id: 'o2', trackId: 1 }]),
        createFrame('f3', 2, 2, [{ id: 'o3', trackId: 1, postBufferMsOverride: 500 }]),
    ];
}

function trackBlurFrames() {
    return [createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1, blurSizePercentOverride: 150 }])];
}

function occurrenceBlurFrames() {
    return [createFrame('f1', 0, 0, [{ id: 'o1', trackId: 1, occurrenceBlurSizePercentOverride: 120 }])];
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

function openEditor(world: World, frames: AnalyzedFrameDto[] = trackedOccurrenceFrames()) {
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

function findObject(world: World, objectId: string) {
    const vm = world.wrapper!.vm as any;
    return vm.getFrames()
        .flatMap((frame: AnalyzedFrameDto) => frame.detectedObjects)
        .find((obj: DetectedObjectDto) => obj.id === objectId);
}

async function dragGroupHandle(
    world: World,
    start: { x: number; y: number },
    end: { x: number; y: number },
    pointerId: number
) {
    const handle = world.wrapper!.find('.inspector-drag-handle');
    await handle.trigger('pointerdown', { clientX: start.x, clientY: start.y, pointerId });
    await handle.trigger('pointermove', { clientX: end.x, clientY: end.y, pointerId });
    await handle.trigger('pointerup', { clientX: end.x, clientY: end.y, pointerId });
}

const steps: StepDefinition[] = [
    {
        pattern: /^the editor is open with a selected tracked occurrence$/,
        handler: world => {
            openEditor(world);
            selectBox(world);
        },
    },
    {
        pattern: /^the editor is open with a selected tracked occurrence on a wide stage$/,
        handler: world => {
            openEditor(world);
            setupStage(world, 900, 600);
            selectBox(world);
        },
    },
    {
        pattern: /^the editor is open with a two-occurrence track and the group dragged to a custom position$/,
        handler: async world => {
            openEditor(world, twoOccurrenceTrackFrames());
            setupStage(world, 900, 600);
            selectBox(world);
            await world.wrapper!.vm.$nextTick();
            await dragGroupHandle(world, { x: 400, y: 400 }, { x: 100, y: 300 }, 7);
            const vm = world.wrapper!.vm as any;
            world.manualPosition = { ...vm.retainedInspectorPosition };
        },
    },
    {
        pattern: /^the editor is open with a selected tracked occurrence on a very short stage$/,
        handler: world => {
            openEditor(world);
            setupStage(world, 900, 200);
            selectBox(world);
        },
    },
    {
        pattern: /^the editor is open with a segment whose first occurrence holds a pre override$/,
        handler: world => {
            openEditor(world, interiorSegmentFrames());
        },
    },
    {
        pattern: /^the editor is open with a track without any blur overrides$/,
        handler: world => {
            openEditor(world, trackedOccurrenceFrames());
        },
    },
    {
        pattern: /^the editor is open with a track whose blur size is overridden$/,
        handler: world => {
            openEditor(world, trackBlurFrames());
        },
    },
    {
        pattern: /^the editor is open with a track whose occurrence blur is overridden$/,
        handler: world => {
            openEditor(world, occurrenceBlurFrames());
        },
    },
    {
        pattern: /^the editor is open with a track whose segment boundaries share one value$/,
        handler: world => {
            openEditor(world, commonTimeBufferFrames());
        },
    },
    {
        pattern: /^the editor is open with a track whose segments carry different boundary values$/,
        handler: world => {
            openEditor(world, mixedSegmentFrames());
        },
    },
    {
        pattern: /^the editor is open with a track whose segments carry custom boundary values$/,
        handler: world => {
            openEditor(world, mixedSegmentFrames());
        },
    },
    {
        pattern: /^the reviewer selects the box$/,
        handler: world => {
            selectBox(world);
        },
    },
    {
        pattern: /^the reviewer selects the middle occurrence of that segment$/,
        handler: world => {
            selectBox(world, 'o2');
        },
    },
    {
        pattern: /^the reviewer selects the box and resets the occurrence blur size$/,
        handler: world => {
            selectBox(world, 'o1');
            const vm = world.wrapper!.vm as any;
            vm.handleResetOccurrenceBlurSize();
        },
    },
    {
        pattern: /^the reviewer navigates to the next occurrence$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.goToNextOccurrence();
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^the reviewer drags the inspector group handle far beyond the stage edges$/,
        handler: world => {
            return dragGroupHandle(world, { x: 100, y: 100 }, { x: -5000, y: -5000 }, 8);
        },
    },
    {
        pattern: /^applies a time buffer of (\d+) to the track$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            vm.handleUpdateTrackTimeBuffer(450);
        },
    },
    {
        pattern: /^resets the track time buffer$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            vm.handleResetTrackTimeBuffer();
        },
    },
    {
        pattern: /^the inspector shows Current occurrence, Current segment, and Entire track panels in order$/,
        handler: world => {
            const panels = world.wrapper!.findAll('.scope-panel');
            expect(panels.map(p => p.attributes('data-testid'))).toEqual([
                'scope-panel-occurrence',
                'scope-panel-segment',
                'scope-panel-track',
            ]);
        },
    },
    {
        pattern: /^each panel has its own header and surface$/,
        handler: world => {
            for (const panel of world.wrapper!.findAll('.scope-panel')) {
                expect(panel.find('.scope-panel-header').exists()).toBe(true);
            }
            const group = world.wrapper!.find('[data-testid="object-details-panel"]');
            expect(group.classes()).not.toContain('scope-panel');
        },
    },
    {
        pattern: /^the scope panels are separate sibling elements with transparent gaps between them$/,
        handler: world => {
            const group = world.wrapper!.find('[data-testid="object-details-panel"]');
            expect(group.classes()).toEqual(['inspector-group']);
            expect(group.element.querySelectorAll(':scope > .scope-panel').length).toBe(3);
        },
    },
    {
        pattern: /^all three panels remain attached and the group stays fully inside the stage$/,
        handler: world => {
            expect(world.wrapper!.findAll('.scope-panel')).toHaveLength(3);
            const vm = world.wrapper!.vm as any;
            const placement = vm.inspectorPlacement;
            expect(placement.top).toBeGreaterThanOrEqual(8);
            expect(placement.top + vm.inspectorGroupHeight).toBeLessThanOrEqual(600);
            expect(placement.left).toBeGreaterThanOrEqual(8);
            expect(placement.left + 320).toBeLessThanOrEqual(900);
        },
    },
    {
        pattern: /^the group keeps the custom position$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.retainedInspectorPosition).toEqual(world.manualPosition);
            expect(vm.inspectorPlacement.left).toBe(world.manualPosition!.left);
        },
    },
    {
        pattern: /^the inspector group stays attached and scrolls as one unit inside the stage$/,
        handler: world => {
            expect(world.wrapper!.findAll('.scope-panel')).toHaveLength(3);
            const vm = world.wrapper!.vm as any;
            expect(vm.inspectorScrollStyle).not.toBeNull();
            expect(vm.inspectorScrollStyle.overflowY).toBe('auto');
            expect(vm.inspectorPlacement.top).toBeGreaterThanOrEqual(8);
        },
    },
    {
        pattern: /^the Current segment panel shows the segment pre override and the global post value$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedTrackSettings.pre).toBe(450);
            expect(vm.selectedTrackSettings.preIsCustom).toBe(true);
            expect(vm.selectedTrackSettings.post).toBe(300);
            expect(vm.selectedTrackSettings.postIsCustom).toBe(false);
        },
    },
    {
        pattern: /^selecting the interior occurrence does not store pre or post on it$/,
        handler: world => {
            expect(findObject(world, 'o2')!.preBufferMsOverride).toBeNull();
            expect(findObject(world, 'o2')!.postBufferMsOverride).toBeNull();
        },
    },
    {
        pattern: /^the Current occurrence panel contains only occurrence-level controls$/,
        handler: world => {
            const occ = world.wrapper!.find('[data-testid="scope-panel-occurrence"]');
            expect(occ.find('input[type="checkbox"]').exists()).toBe(true);
            expect(occ.find('[data-testid="occurrence-blur-input"]').exists()).toBe(true);
            expect(occ.text()).toContain('Adjust detection');
            expect(occ.find('[data-testid="track-shape-input"]').exists()).toBe(false);
            expect(occ.find('[data-testid="track-time-buffer-input"]').exists()).toBe(false);
        },
    },
    {
        pattern: /^the Current segment panel contains only the Before and After controls$/,
        handler: world => {
            const seg = world.wrapper!.find('[data-testid="scope-panel-segment"]');
            expect(seg.find('[data-testid="segment-pre-input"]').exists()).toBe(true);
            expect(seg.find('[data-testid="segment-post-input"]').exists()).toBe(true);
            expect(seg.find('[data-testid="occurrence-blur-input"]').exists()).toBe(false);
            expect(seg.find('[data-testid="track-blur-input"]').exists()).toBe(false);
            expect(seg.find('[data-testid="track-shape-input"]').exists()).toBe(false);
            expect(seg.find('[data-testid="track-time-buffer-input"]').exists()).toBe(false);
        },
    },
    {
        pattern: /^the Entire track panel contains shape, track blur, time buffer, and advanced controls$/,
        handler: world => {
            const trk = world.wrapper!.find('[data-testid="scope-panel-track"]');
            expect(trk.find('[data-testid="track-shape-input"]').exists()).toBe(true);
            expect(trk.find('[data-testid="track-blur-input"]').exists()).toBe(true);
            expect(trk.find('[data-testid="track-time-buffer-input"]').exists()).toBe(true);
            expect(trk.find('[data-testid="advanced-menu"]').exists()).toBe(false);
            expect(trk.text()).toContain('Advanced');
        },
    },
    {
        pattern: /^the inspector has no track-wide inclusion control$/,
        handler: world => {
            const group = world.wrapper!.find('[data-testid="object-details-panel"]');
            expect(group.findAll('input[type="checkbox"]')).toHaveLength(1);
        },
    },
    {
        pattern: /^the occurrence blur badge reads (Global|Track|Custom)$/,
        handler: (world, match) => {
            const badge = world.wrapper!.find('[data-testid="badge-occurrence-blur"]');
            expect(badge.text()).toBe(match[1]);
        },
    },
    {
        pattern: /^the time buffer control reports that common value$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedTrackSettings.trackTimeBufferIsMixed).toBe(false);
            expect(vm.selectedTrackSettings.trackTimeBufferEffective).toBe(500);
            expect(world.wrapper!.find('[data-testid="badge-time-buffer"]').text()).toBe('Custom');
        },
    },
    {
        pattern: /^the time buffer control reports Mixed$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedTrackSettings.trackTimeBufferIsMixed).toBe(true);
            expect(vm.selectedTrackSettings.trackTimeBufferEffective).toBeNull();
            expect(world.wrapper!.find('[data-testid="badge-time-buffer"]').text()).toBe('Mixed');
        },
    },
    {
        pattern: /^every current segment stores the new pre and post boundary values$/,
        handler: world => {
            expect(findObject(world, 'o1')!.preBufferMsOverride).toBe(450);
            expect(findObject(world, 'o2')!.postBufferMsOverride).toBe(450);
            expect(findObject(world, 'o4')!.preBufferMsOverride).toBe(450);
            expect(findObject(world, 'o4')!.postBufferMsOverride).toBe(450);
        },
    },
    {
        pattern: /^Vue sends one bulk update with cloned before-state$/,
        handler: world => {
            expect(world.onDetectedObjectsBulkUpdated).toHaveBeenCalledOnce();
            const [videoId, dtos, operationType, beforeState] = world.onDetectedObjectsBulkUpdated!.mock.calls[0];
            expect(videoId).toBe('v1');
            expect(operationType).toBe('track-settings');
            expect(dtos.map((d: DetectedObjectDto) => d.id).sort()).toEqual(['o1', 'o2', 'o4']);
            expect(beforeState).toHaveLength(3);
            beforeState.forEach((item: DetectedObjectDto, index: number) => {
                expect(item).not.toBe(dtos[index]);
            });
        },
    },
    {
        pattern: /^every current segment boundary falls back to global and stores null$/,
        handler: world => {
            expect(findObject(world, 'o1')!.preBufferMsOverride).toBeNull();
            expect(findObject(world, 'o2')!.postBufferMsOverride).toBeNull();
            expect(findObject(world, 'o4')!.preBufferMsOverride).toBeNull();
            expect(findObject(world, 'o4')!.postBufferMsOverride).toBeNull();
        },
    },
    {
        pattern: /^the occurrence stores a null occurrence blur override$/,
        handler: world => {
            expect(findObject(world, 'o1')!.occurrenceBlurSizePercentOverride).toBeNull();
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

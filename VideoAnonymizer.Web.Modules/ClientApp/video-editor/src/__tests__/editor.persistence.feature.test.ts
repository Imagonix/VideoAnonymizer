import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import type { AnalyzedFrameDto, DetectedObjectDto, VideoEditorProps } from '../types';
import featureText from './editor.persistence.feature?raw';

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
    addedFace?: DetectedObjectDto;
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

function createMockState(): VideoEditorProps {
    const frames = [
        createFrame('f1', 0, [
            { id: 'o1', trackId: 1 },
            { id: 'o2', trackId: 2 },
        ]),
        createFrame('f2', 1, [
            { id: 'o3', trackId: 1 },
            { id: 'o4', trackId: 2 },
            { id: 'o5', trackId: null },
        ]),
        createFrame('f3', 2, [
            { id: 'o6', trackId: 1 },
        ]),
    ];

    return {
        videoId: 'v1',
        videoSourceUrl: 'http://example.com/v.mp4',
        anonymizationSettings: { blurSizePercent: 200, timeBufferMs: 300, interpolateTrackedObjects: true },
        frames,
    };
}

function mountEditor(overrides: Partial<VideoEditorProps> = {}) {
    const state = reactive({ ...createMockState(), ...overrides } as VideoEditorProps);
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
                BoundingBoxOverlay: {
                    template: '<div class="mock-overlay" />',
                    props: ['objects', 'anonymizationSettings', 'videoDimensions', 'highlightedRowKey', 'splitSourceKey', 'alwaysShowKeys'],
                },
            },
        },
    });

    return { wrapper, state };
}

function findFace(world: World, faceId: string) {
    const vm = world.wrapper!.vm as any;
    return vm.getFrames()
        .flatMap((frame: AnalyzedFrameDto) => frame.detectedObjects)
        .find((face: DetectedObjectDto) => face.id === faceId);
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

async function addNewFace(world: World) {
    const vm = world.wrapper!.vm as any;
    vm.addBox(100, 110, 50, 60, 'face', 'new');
    await world.wrapper!.vm.$nextTick();
    world.addedFace = world.onDetectedObjectAdded!.mock.calls[0][2];
}

const steps: StepDefinition[] = [
    {
        pattern: /^the editor is open with persisted frames$/,
        handler: world => {
            openEditor(world);
        },
    },
    {
        pattern: /^the editor has sent an add callback for a new face$/,
        handler: async world => {
            openEditor(world);
            await addNewFace(world);
            expect(world.onDetectedObjectAdded).toHaveBeenCalledOnce();
        },
    },
    {
        pattern: /^the reviewer deselects face "([^"]+)"$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.toggleObject(match[1], false);
        },
    },
    {
        pattern: /^Vue sends a "([^"]+)" update for face "([^"]+)" with selected (true|false) and previous selected (true|false)$/,
        handler: (world, match) => {
            expect(world.onDetectedObjectUpdated).toHaveBeenCalledOnce();
            const [videoId, analyzedFrameId, dto, operationType, beforeState] = world.onDetectedObjectUpdated!.mock.calls[0];

            expect(videoId).toBe('v1');
            expect(analyzedFrameId).toBe('f2');
            expect(operationType).toBe(match[1]);
            expect(dto.id).toBe(match[2]);
            expect(dto.selected).toBe(match[3] === 'true');
            expect(beforeState).toHaveLength(1);
            expect(beforeState[0].id).toBe(match[2]);
            expect(beforeState[0].selected).toBe(match[4] === 'true');
        },
    },
    {
        pattern: /^the reviewer deselects tracked row (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            const trackId = Number(match[1]);
            const trackedObject = vm.timelineObjects.find((obj: any) =>
                obj.type === 'tracked' && obj.occurences[0][1].trackId === trackId);

            vm.toggleTrackedObject(trackedObject, false);
        },
    },
    {
        pattern: /^Vue sends a "([^"]+)" bulk update for faces "([^"]+)" with selected (true|false) and previous selected (true|false)$/,
        handler: (world, match) => {
            const expectedIds = match[2].split(',').sort();
            expect(world.onDetectedObjectsBulkUpdated).toHaveBeenCalledOnce();
            const [videoId, dtos, operationType, beforeState] = world.onDetectedObjectsBulkUpdated!.mock.calls[0];

            expect(videoId).toBe('v1');
            expect(operationType).toBe(match[1]);
            expect(dtos.map((face: DetectedObjectDto) => face.id).sort()).toEqual(expectedIds);
            expect(dtos.every((face: DetectedObjectDto) => face.selected === (match[3] === 'true'))).toBe(true);
            expect(beforeState.map((face: DetectedObjectDto) => face.id).sort()).toEqual(expectedIds);
            expect(beforeState.every((face: DetectedObjectDto) => face.selected === (match[4] === 'true'))).toBe(true);
        },
    },
    {
        pattern: /^the reviewer draws a new face box at (\d+),(\d+) with size (\d+)x(\d+)$/,
        handler: async (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.addBox(Number(match[1]), Number(match[2]), Number(match[3]), Number(match[4]), 'face', 'new');
            await world.wrapper!.vm.$nextTick();
            world.addedFace = world.onDetectedObjectAdded!.mock.calls[0][2];
        },
    },
    {
        pattern: /^Vue sends an add callback for the new face in frame "([^"]+)"$/,
        handler: (world, match) => {
            expect(world.onDetectedObjectAdded).toHaveBeenCalledOnce();
            const [, frameId, added] = world.onDetectedObjectAdded!.mock.calls[0];
            expect(frameId).toBe(match[1]);
            expect(added.x).toBe(100);
            expect(added.y).toBe(110);
            expect(added.width).toBe(50);
            expect(added.height).toBe(60);
        },
    },
    {
        pattern: /^the reviewer deletes that new face$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.deleteObject(world.addedFace);
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^Vue sends a delete callback for the same face in frame "([^"]+)"$/,
        handler: (world, match) => {
            expect(world.onDetectedObjectDeleted).toHaveBeenCalledOnce();
            const [, frameId, deleted] = world.onDetectedObjectDeleted!.mock.calls[0];
            expect(frameId).toBe(match[1]);
            expect(deleted.id).toBe(world.addedFace!.id);
        },
    },
    {
        pattern: /^the new face is no longer in the editor state$/,
        handler: world => {
            expect(findFace(world, world.addedFace!.id)).toBeUndefined();
        },
    },
    {
        pattern: /^Blazor pushes an update for face "([^"]+)", removes face "([^"]+)", and adds face "([^"]+)"$/,
        handler: async (world, match) => {
            const vm = world.wrapper!.vm as any;
            const added = {
                id: match[3],
                confidence: 1,
                className: 'face',
                selected: true,
                trackId: 9,
                x: 1,
                y: 2,
                width: 3,
                height: 4,
                analyzedFrameId: 'f1',
            };

            vm.applyChanges({
                objectsToUpdate: [{ ...world.state!.frames[0].detectedObjects[0], selected: false, x: 77 }],
                objectsToRemove: [match[2]],
                objectsToAdd: [added],
            });
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^face "([^"]+)" is deselected at x (\d+)$/,
        handler: (world, match) => {
            const face = findFace(world, match[1]);
            expect(face.selected).toBe(false);
            expect(face.x).toBe(Number(match[2]));
        },
    },
    {
        pattern: /^face "([^"]+)" is no longer in the editor state$/,
        handler: (world, match) => {
            expect(findFace(world, match[1])).toBeUndefined();
        },
    },
    {
        pattern: /^face "([^"]+)" is present on track (\d+)$/,
        handler: (world, match) => {
            expect(findFace(world, match[1]).trackId).toBe(Number(match[2]));
        },
    },
    {
        pattern: /^Blazor adds tracked face "([^"]+)" twice and then removes it$/,
        handler: async (world, match) => {
            const vm = world.wrapper!.vm as any;
            const trackedFace: DetectedObjectDto = {
                id: match[1],
                confidence: 1,
                className: 'face',
                selected: true,
                trackId: 9,
                x: 1,
                y: 2,
                width: 3,
                height: 4,
                analyzedFrameId: 'f2',
            };

            vm.applyChanges({ objectsToUpdate: [], objectsToRemove: [], objectsToAdd: [trackedFace] });
            vm.applyChanges({ objectsToUpdate: [], objectsToRemove: [], objectsToAdd: [trackedFace] });
            vm.applyChanges({ objectsToUpdate: [], objectsToRemove: [trackedFace.id], objectsToAdd: [] });
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^no timeline occurrence remains for face "([^"]+)"$/,
        handler: (world, match) => {
            const timelineOccurrences = (world.wrapper!.vm as any).timelineObjects
                .flatMap((timelineObject: any) => timelineObject.type === 'tracked'
                    ? timelineObject.occurences.map(([, object]: [number, DetectedObjectDto]) => object)
                    : [timelineObject.detectedObj]);
            expect(timelineOccurrences.some((object: DetectedObjectDto) => object.id === match[1])).toBe(false);
        },
    },
    {
        pattern: /^the editor is tracking tracks (\d+) and (\d+)$/,
        handler: async (world, match) => {
            openEditor(world);
            const vm = world.wrapper!.vm as any;
            vm.onVideoLoaded(10);

            for (const trackId of [Number(match[1]), Number(match[2])]) {
                const trackedObject = world.state!.frames
                    .flatMap(frame => frame.detectedObjects)
                    .find(obj => obj.trackId === trackId)!;
                vm.trackForward(trackedObject);
            }
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^Blazor reports track (\d+) at (\d+) ms and track (\d+) at (\d+) ms$/,
        handler: async (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.updateTrackingProgress(Number(match[1]), Number(match[2]), Number(match[2]));
            vm.updateTrackingProgress(Number(match[3]), Number(match[4]), Number(match[4]));
            await world.wrapper!.vm.$nextTick();
        },
    },
    {
        pattern: /^track (\d+) has its moving tracking dot at (\d+) percent$/,
        handler: (world, match) => {
            const trackId = Number(match[1]);
            const row = world.wrapper!.findAllComponents({ name: 'TimelineRow' })
                .find(candidate => candidate.props('timelineObject').occurences?.[0]?.[1].trackId === trackId);
            expect(row).toBeDefined();

            const movingDot = row!.find('.dot--pulsing');
            expect(movingDot.exists()).toBe(true);
            expect((movingDot.element as HTMLElement).style.left).toBe(`${match[2]}%`);
        },
    },
    {
        pattern: /^the moving dots are the only timeline tracking indicators$/,
        handler: world => {
            expect(world.wrapper!.findAll('.dot--pulsing')).toHaveLength(2);
            expect(world.wrapper!.find('.timeline-row--tracking').exists()).toBe(false);
            expect(world.wrapper!.find('.color-dot-wrapper--tracking').exists()).toBe(false);
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

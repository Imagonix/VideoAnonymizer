import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import AddBoxDialog from '../AddBoxDialog.vue';
import type { AnalyzedFrameDto, DetectedObjectDto, VideoEditorProps } from '../types';
import featureText from './track-settings.feature?raw';

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
    dialogWrapper?: ReturnType<typeof mount>['wrapper'];
    addedObject?: DetectedObjectDto;
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

function consecutiveTrackFrames() {
    return [
        createFrame('f1', 0, 0, [
            { id: 'o1', trackId: 1 },
            { id: 'o4', trackId: 2 },
        ]),
        createFrame('f2', 1, 1, [
            { id: 'o2', trackId: 1 },
            { id: 'oX', trackId: null },
        ]),
        createFrame('f3', 2, 2, [
            { id: 'o3', trackId: 1 },
            { id: 'o5', trackId: 2 },
        ]),
    ];
}

function trailingPostOverrideFrames() {
    return [
        createFrame('f1', 0, 0, [
            { id: 'o1', trackId: 1 },
        ]),
        createFrame('f2', 1, 1, [
            { id: 'o2', trackId: 1, postBufferMsOverride: 500 },
        ]),
        createFrame('f3', 2, 2, []),
    ];
}

function shapeAndBlurFrames() {
    return [
        createFrame('f1', 0, 0, [
            { id: 'o1', trackId: 1, blurShape: 'rectangle', blurSizePercentOverride: 150 },
        ]),
        createFrame('f2', 1, 1, [
            { id: 'o2', trackId: 1 },
        ]),
        createFrame('f3', 2, 2, []),
    ];
}

function mergeableTrackFrames() {
    return [
        createFrame('f1', 0, 0, [
            { id: 'o1', trackId: 1, preBufferMsOverride: 200, blurShape: 'rectangle', blurSizePercentOverride: 150 },
        ]),
        createFrame('f2', 1, 1, [
            { id: 'o2', trackId: 1 },
        ]),
        createFrame('f3', 2, 2, [
            { id: 'o3', trackId: 2, postBufferMsOverride: 500 },
        ]),
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
                BoundingBoxOverlay: {
                    template: '<div class="mock-overlay" />',
                    props: ['objects', 'anonymizationSettings', 'videoDimensions', 'highlightedRowKey', 'splitSourceKey', 'alwaysShowKeys', 'selectedKey'],
                },
            },
        },
    });

    return { wrapper, state };
}

function openEditor(world: World, frames: AnalyzedFrameDto[] = consecutiveTrackFrames()) {
    world.onDetectedObjectUpdated = vi.fn();
    world.onDetectedObjectsBulkUpdated = vi.fn();
    world.onDetectedObjectAdded = vi.fn();
    world.onDetectedObjectDeleted = vi.fn();

    const mounted = mountEditor(frames, {
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

function findReactiveObject(world: World, objectId: string) {
    return world.state!.frames
        .flatMap(frame => frame.detectedObjects)
        .find(obj => obj.id === objectId)!;
}

function trackOneOccurrences(world: World) {
    const vm = world.wrapper!.vm as any;
    return vm.getFrames()
        .flatMap((frame: AnalyzedFrameDto) => frame.detectedObjects)
        .filter((face: DetectedObjectDto) => face.trackId === 1);
}

const steps: StepDefinition[] = [
    {
        pattern: /^the editor is open with consecutive track segments$/,
        handler: world => {
            openEditor(world);
        },
    },
    {
        pattern: /^the editor is open with a pre override on the first occurrence of track 1$/,
        handler: world => {
            openEditor(world);
            findReactiveObject(world, 'o1').preBufferMsOverride = 450;
        },
    },
    {
        pattern: /^the editor is open with a custom pre override on the first occurrence of track 1$/,
        handler: world => {
            openEditor(world);
            findReactiveObject(world, 'o1').preBufferMsOverride = 450;
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o1'));
        },
    },
    {
        pattern: /^the editor is open with boundary overrides on track 1$/,
        handler: world => {
            openEditor(world);
            findReactiveObject(world, 'o1').preBufferMsOverride = 200;
            findReactiveObject(world, 'o3').postBufferMsOverride = 500;
        },
    },
    {
        pattern: /^the editor is open with a track whose last occurrence holds a post override$/,
        handler: world => {
            openEditor(world, trailingPostOverrideFrames());
        },
    },
    {
        pattern: /^the editor is open with a track that carries shape and blur size$/,
        handler: world => {
            openEditor(world, shapeAndBlurFrames());
        },
    },
    {
        pattern: /^the editor is open with two mergeable tracks$/,
        handler: world => {
            openEditor(world, mergeableTrackFrames());
        },
    },
    {
        pattern: /^the reviewer selects the first occurrence of track 1$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o1'));
        },
    },
    {
        pattern: /^the selected segment contains all three occurrences of track 1$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            const segment = vm.findSegmentFor(findObject(world, 'o1'));
            expect(segment.occurrences.map((o: DetectedObjectDto) => o.id)).toEqual(['o1', 'o2', 'o3']);
        },
    },
    {
        pattern: /^a gap in analyzed frames splits track 2 into two one-object segments$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            const segmentO4 = vm.findSegmentFor(findObject(world, 'o4'));
            const segmentO5 = vm.findSegmentFor(findObject(world, 'o5'));
            expect(segmentO4.occurrences.map((o: DetectedObjectDto) => o.id)).toEqual(['o4']);
            expect(segmentO5.occurrences.map((o: DetectedObjectDto) => o.id)).toEqual(['o5']);
        },
    },
    {
        pattern: /^the reviewer selects an occurrence of a track without boundary overrides$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o1'));
        },
    },
    {
        pattern: /^the pre value shows the global buffer and is marked Global$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedTrackSettings.pre).toBe(300);
            expect(vm.selectedTrackSettings.preIsCustom).toBe(false);
            expect(world.wrapper!.find('[data-testid="badge-pre"]').text()).toBe('Global');
        },
    },
    {
        pattern: /^the post value shows the global buffer and is marked Global$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedTrackSettings.post).toBe(300);
            expect(vm.selectedTrackSettings.postIsCustom).toBe(false);
        },
    },
    {
        pattern: /^the first and last occurrences keep null boundary overrides$/,
        handler: world => {
            expect(findObject(world, 'o1')!.preBufferMsOverride).toBeNull();
            expect(findObject(world, 'o3')!.postBufferMsOverride).toBeNull();
        },
    },
    {
        pattern: /^the reviewer selects the last occurrence of track 1 and sets pre to (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o3'));
            vm.handleUpdatePre(Number(match[1]));
        },
    },
    {
        pattern: /^the reviewer selects the first occurrence of track 1 and sets post to (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o1'));
            vm.handleUpdatePost(Number(match[1]));
        },
    },
    {
        pattern: /^the reviewer selects an untracked occurrence and sets pre to (\d+) and post to (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'oX'));
            vm.handleUpdatePre(Number(match[1]));
            vm.handleUpdatePost(Number(match[2]));
        },
    },
    {
        pattern: /^the reviewer sets the segment pre to the global buffer value (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o1'));
            vm.handleUpdatePre(Number(match[1]));
        },
    },
    {
        pattern: /^only the first occurrence of track 1 stores a pre override of (\d+)$/,
        handler: (world, match) => {
            expect(findObject(world, 'o1')!.preBufferMsOverride).toBe(Number(match[1]));
            expect(findObject(world, 'o2')!.preBufferMsOverride).toBeNull();
            expect(findObject(world, 'o3')!.preBufferMsOverride).toBeNull();
        },
    },
    {
        pattern: /^only the last occurrence of track 1 stores a post override of (\d+)$/,
        handler: (world, match) => {
            expect(findObject(world, 'o3')!.postBufferMsOverride).toBe(Number(match[1]));
            expect(findObject(world, 'o1')!.postBufferMsOverride).toBeNull();
            expect(findObject(world, 'o2')!.postBufferMsOverride).toBeNull();
        },
    },
    {
        pattern: /^the untracked occurrence stores both the pre override (\d+) and post override (\d+)$/,
        handler: (world, match) => {
            const object = findObject(world, 'oX');
            expect(object!.preBufferMsOverride).toBe(Number(match[1]));
            expect(object!.postBufferMsOverride).toBe(Number(match[2]));
        },
    },
    {
        pattern: /^Vue sends a "([^"]+)" update for the first occurrence with the previous null state$/,
        handler: (world, match) => {
            expect(world.onDetectedObjectUpdated).toHaveBeenCalledOnce();
            const [videoId, analyzedFrameId, dto, operationType, beforeState] = world.onDetectedObjectUpdated!.mock.calls[0];
            expect(videoId).toBe('v1');
            expect(analyzedFrameId).toBe('f1');
            expect(operationType).toBe(match[1]);
            expect(dto.id).toBe('o1');
            expect(beforeState).toHaveLength(1);
            expect(beforeState[0].id).toBe('o1');
            expect(beforeState[0].preBufferMsOverride).toBeNull();
        },
    },
    {
        pattern: /^Vue sends a "([^"]+)" update for the last occurrence with the previous null state$/,
        handler: (world, match) => {
            expect(world.onDetectedObjectUpdated).toHaveBeenCalledOnce();
            const [videoId, analyzedFrameId, dto, operationType, beforeState] = world.onDetectedObjectUpdated!.mock.calls[0];
            expect(videoId).toBe('v1');
            expect(analyzedFrameId).toBe('f3');
            expect(operationType).toBe(match[1]);
            expect(dto.id).toBe('o3');
            expect(beforeState).toHaveLength(1);
            expect(beforeState[0].id).toBe('o3');
            expect(beforeState[0].postBufferMsOverride).toBeNull();
        },
    },
    {
        pattern: /^the reviewer resets the pre override on the first occurrence$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            vm.selectObject(findObject(world, 'o1'));
            vm.handleResetPre();
        },
    },
    {
        pattern: /^the first occurrence stores a null pre override$/,
        handler: world => {
            expect(findObject(world, 'o1')!.preBufferMsOverride).toBeNull();
        },
    },
    {
        pattern: /^Vue sends a "([^"]+)" update with the previous override state$/,
        handler: (world, match) => {
            expect(world.onDetectedObjectUpdated).toHaveBeenCalledOnce();
            const [, , dto, operationType, beforeState] = world.onDetectedObjectUpdated!.mock.calls[0];
            expect(operationType).toBe(match[1]);
            expect(dto.id).toBe('o1');
            expect(beforeState).toHaveLength(1);
            expect(beforeState[0].preBufferMsOverride).toBe(450);
        },
    },
    {
        pattern: /^the global time buffer changes from (\d+) to (\d+)$/,
        handler: (world, match) => {
            world.state!.anonymizationSettings.timeBufferMs = Number(match[2]);
        },
    },
    {
        pattern: /^the custom pre override remains stored$/,
        handler: world => {
            expect(findObject(world, 'o1')!.preBufferMsOverride).toBe(450);
        },
    },
    {
        pattern: /^the displayed pre value stays (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            expect(vm.selectedTrackSettings.pre).toBe(Number(match[1]));
        },
    },
    {
        pattern: /^the reviewer adds a box on the next frame assigned to track (\d+)$/,
        handler: async (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.seekTo(2);
            vm.addBox(10, 10, 20, 20, 'face', Number(match[1]));
            await world.wrapper!.vm.$nextTick();
            world.addedObject = world.onDetectedObjectAdded!.mock.calls[0][2];
        },
    },
    {
        pattern: /^the reviewer adds a box on the next frame assigned to that track$/,
        handler: async world => {
            const vm = world.wrapper!.vm as any;
            vm.seekTo(2);
            vm.addBox(10, 10, 20, 20, 'face', 1);
            await world.wrapper!.vm.$nextTick();
            world.addedObject = world.onDetectedObjectAdded!.mock.calls[0][2];
        },
    },
    {
        pattern: /^the new box stores the previous last occurrence post override (\d+)$/,
        handler: (world, match) => {
            expect(world.addedObject!.postBufferMsOverride).toBe(Number(match[1]));
        },
    },
    {
        pattern: /^the previous last occurrence clears its post override$/,
        handler: world => {
            expect(findObject(world, 'o2')!.postBufferMsOverride).toBeNull();
        },
    },
    {
        pattern: /^Vue sends an add callback and a "([^"]+)" update for the cleared boundary$/,
        handler: (world, match) => {
            expect(world.onDetectedObjectAdded).toHaveBeenCalledOnce();
            expect(world.onDetectedObjectUpdated).toHaveBeenCalledOnce();
            const [, , dto, operationType, beforeState] = world.onDetectedObjectUpdated!.mock.calls[0];
            expect(operationType).toBe(match[1]);
            expect(dto.id).toBe('o2');
            expect(beforeState).toHaveLength(1);
            expect(beforeState[0].postBufferMsOverride).toBe(500);
        },
    },
    {
        pattern: /^the reviewer sets track (\d+) shape to rectangle and blur size to (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.applyTrackBlurShape(Number(match[1]), 'rectangle');
            vm.applyTrackBlurSize(Number(match[1]), Number(match[2]));
        },
    },
    {
        pattern: /^every track 1 occurrence stores shape rectangle and blur size override (\d+)$/,
        handler: (world, match) => {
            const occurrences = trackOneOccurrences(world);
            expect(occurrences).toHaveLength(3);
            expect(occurrences.every(o => o.blurShape === 'rectangle')).toBe(true);
            expect(occurrences.every(o => o.blurSizePercentOverride === Number(match[1]))).toBe(true);
        },
    },
    {
        pattern: /^Vue sends a "([^"]+)" bulk update with cloned before state$/,
        handler: (world, match) => {
            expect(world.onDetectedObjectsBulkUpdated).toHaveBeenCalledTimes(2);
            for (const call of world.onDetectedObjectsBulkUpdated!.mock.calls) {
                const [videoId, dtos, operationType, beforeState] = call;
                expect(videoId).toBe('v1');
                expect(operationType).toBe(match[1]);
                expect(dtos).toHaveLength(3);
                expect(beforeState).toHaveLength(3);
                expect(dtos.map((d: DetectedObjectDto) => d.id).sort()).toEqual(['o1', 'o2', 'o3']);
            }
        },
    },
    {
        pattern: /^the reviewer sets track (\d+) blur size to the global value (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.applyTrackBlurSize(Number(match[1]), Number(match[2]));
        },
    },
    {
        pattern: /^the reviewer sets track (\d+) blur size to (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.applyTrackBlurSize(Number(match[1]), Number(match[2]));
        },
    },
    {
        pattern: /^every track 1 occurrence stores a null blur size override$/,
        handler: world => {
            const occurrences = trackOneOccurrences(world);
            expect(occurrences.every(o => o.blurSizePercentOverride === null)).toBe(true);
        },
    },
    {
        pattern: /^the AddBoxDialog is open$/,
        handler: world => {
            world.dialogWrapper = mount(AddBoxDialog, {
                props: { existingTrackIds: [1, 2], trackIdsInCurrentFrame: new Set([1]) },
            });
        },
    },
    {
        pattern: /^the reviewer inspects the default class selection$/,
        handler: world => {
            const select = world.dialogWrapper!.find('select');
            expect(select.exists()).toBe(true);
        },
    },
    {
        pattern: /^the default class is Other and Face remains available$/,
        handler: world => {
            const select = world.dialogWrapper!.find('select');
            expect((select.element as HTMLSelectElement).value).toBe('other');
            const options = world.dialogWrapper!.findAll('option');
            expect(options.map(o => (o.element as HTMLOptionElement).text)).toContain('Other');
            expect(options.map(o => (o.element as HTMLOptionElement).text)).toContain('Face');
        },
    },
    {
        pattern: /^the new box inherits the track shape and blur size override$/,
        handler: world => {
            expect(world.addedObject!.blurShape).toBe('rectangle');
            expect(world.addedObject!.blurSizePercentOverride).toBe(150);
        },
    },
    {
        pattern: /^the reviewer splits out the middle occurrence of track 1$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            const selectedOccurrences = new Map([['track-1', new Set([1])]]);
            vm.splitExecute(selectedOccurrences, world.state!.frames);
        },
    },
    {
        pattern: /^the split occurrence moves to a new track$/,
        handler: world => {
            const object = findObject(world, 'o2');
            expect(object!.trackId).toBe(3);
        },
    },
    {
        pattern: /^each resulting run keeps boundary overrides only on its outer occurrences$/,
        handler: world => {
            expect(findObject(world, 'o1')!.preBufferMsOverride).toBe(200);
            expect(findObject(world, 'o1')!.postBufferMsOverride).toBeNull();
            expect(findObject(world, 'o3')!.postBufferMsOverride).toBe(500);
            expect(findObject(world, 'o3')!.preBufferMsOverride).toBeNull();
            expect(findObject(world, 'o2')!.preBufferMsOverride).toBeNull();
            expect(findObject(world, 'o2')!.postBufferMsOverride).toBeNull();
        },
    },
    {
        pattern: /^the reviewer merges track 2 into track 1$/,
        handler: world => {
            const vm = world.wrapper!.vm as any;
            vm.mergeToggle('track-1');
            vm.mergeToggle('track-2');
            vm.mergeExecute(vm.timelineObjects, world.state!.frames);
        },
    },
    {
        pattern: /^the merged track adopts track 1 shape and blur size$/,
        handler: world => {
            const occurrences = trackOneOccurrences(world);
            expect(occurrences).toHaveLength(3);
            expect(occurrences.every(o => o.blurShape === 'rectangle')).toBe(true);
            expect(occurrences.every(o => o.blurSizePercentOverride === 150)).toBe(true);
        },
    },
    {
        pattern: /^boundary overrides survive only on the merged run outer occurrences$/,
        handler: world => {
            expect(findObject(world, 'o1')!.preBufferMsOverride).toBe(200);
            expect(findObject(world, 'o1')!.postBufferMsOverride).toBeNull();
            expect(findObject(world, 'o2')!.preBufferMsOverride).toBeNull();
            expect(findObject(world, 'o2')!.postBufferMsOverride).toBeNull();
            expect(findObject(world, 'o3')!.postBufferMsOverride).toBe(500);
            expect(findObject(world, 'o3')!.preBufferMsOverride).toBeNull();
        },
    },
    {
        pattern: /^the reviewer sets an occurrence blur size to (\d+) on the first occurrence and resets it$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            const object = findReactiveObject(world, 'o1');
            vm.applyOccurrenceBlurSize(object, Number(match[1]));
            vm.resetOccurrenceBlurSize(object);
        },
    },
    {
        pattern: /^the reviewer sets an occurrence blur size to (\d+) on the first occurrence$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            vm.applyOccurrenceBlurSize(findReactiveObject(world, 'o1'), Number(match[1]));
        },
    },
    {
        pattern: /^the first occurrence stores an occurrence blur override of (\d+)$/,
        handler: (world, match) => {
            expect(findObject(world, 'o1')!.occurrenceBlurSizePercentOverride).toBe(Number(match[1]));
        },
    },
    {
        pattern: /^the first occurrence stores a null occurrence blur override$/,
        handler: world => {
            expect(findObject(world, 'o1')!.occurrenceBlurSizePercentOverride).toBeNull();
        },
    },
    {
        pattern: /^the first occurrence resolves to (\d+) while the other occurrences resolve to (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            expect(vm.resolveOccurrenceBlurSize(findObject(world, 'o1'))).toBe(Number(match[1]));
            expect(vm.resolveOccurrenceBlurSize(findObject(world, 'o2'))).toBe(Number(match[2]));
        },
    },
    {
        pattern: /^the first occurrence resolves to the track blur size (\d+)$/,
        handler: (world, match) => {
            const vm = world.wrapper!.vm as any;
            expect(vm.resolveOccurrenceBlurSize(findObject(world, 'o1'))).toBe(Number(match[1]));
        },
    },
    {
        pattern: /^the first occurrence keeps its occurrence blur override of (\d+)$/,
        handler: (world, match) => {
            expect(findObject(world, 'o1')!.occurrenceBlurSizePercentOverride).toBe(Number(match[1]));
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

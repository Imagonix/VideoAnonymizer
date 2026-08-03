import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import type { VideoEditorProps, DetectedObjectDto, AnalyzedFrameDto } from '../types';

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
            blurShape: o.blurShape,
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
                    props: ['objects', 'anonymizationSettings', 'videoDimensions', 'highlightedRowKey', 'splitSourceKey', 'alwaysShowKeys', 'selectedKey', 'mode', 'adjustObject'],
                },
            },
        },
    });
    return { wrapper, state };
}

function getButton(wrapper: ReturnType<typeof mount>['wrapper'], text: string) {
    const buttons = wrapper.findAll('button');
    return buttons.find(b => b.text().includes(text));
}

async function clickButton(wrapper: ReturnType<typeof mount>['wrapper'], text: string) {
    const btn = getButton(wrapper, text);
    if (!btn) throw new Error(`Button with text "${text}" not found`);
    await btn.trigger('click');
}

function getTrackIds(wrapper: ReturnType<typeof mount>['wrapper'], frameIndex?: number): number[] {
    const vm = wrapper.vm as any;
    const frames = frameIndex != null
        ? [vm.props.state.frames[frameIndex]]
        : vm.props.state.frames;
    const ids = new Set<number>();
    for (const f of frames) {
        for (const o of f.detectedObjects) {
            if (o.trackId != null) ids.add(o.trackId);
        }
    }
    return [...ids].sort((a, b) => a - b);
}

function getObjTrackIds(wrapper: ReturnType<typeof mount>['wrapper'], ...objIds: string[]): number[] {
    const vm = wrapper.vm as any;
    const result: number[] = [];
    for (const f of vm.props.state.frames) {
        for (const o of f.detectedObjects) {
            if (objIds.includes(o.id)) {
                result.push(o.trackId);
            }
        }
    }
    return result;
}

async function selectTrackOne(wrapper: ReturnType<typeof mount>['wrapper']) {
    const vm = wrapper.vm as any;
    const frame = wrapper.vm.$props.state.frames[0];
    vm.selectObject(frame.detectedObjects.find((o: DetectedObjectDto) => o.trackId === 1));
    await wrapper.vm.$nextTick();
}

describe('VideoEditorApp integration', () => {
    let wrapper: ReturnType<typeof mount>['wrapper'];
    let state: ReturnType<typeof mount>['state'];

    beforeEach(() => {
        const m = mountEditor();
        wrapper = m.wrapper;
        state = m.state;
    });

    describe('merge', () => {
        it('merges two tracked rows into the first selected track', async () => {
            expect(getTrackIds(wrapper)).toEqual([1, 2]);
            const vm = wrapper.vm as any;
            vm.activate('merge');
            await wrapper.vm.$nextTick();

            const labels = wrapper.findAll('.label-container');
            expect(labels.length).toBeGreaterThanOrEqual(2);

            await labels[0].trigger('click');
            await labels[1].trigger('click');

            const mergeBtn = getButton(wrapper, 'Merge 2');
            expect(mergeBtn).toBeTruthy();
            await mergeBtn!.trigger('click');

            const trackIds = getTrackIds(wrapper);
            expect(trackIds).toEqual([1, 2]);
        });

        it('disables timeline row checkboxes in merge mode', async () => {
            const vm = wrapper.vm as any;
            vm.activate('merge');
            await wrapper.vm.$nextTick();
            const labelCheckboxes = wrapper.findAll('.label-container input[type="checkbox"]');
            for (const cb of labelCheckboxes) {
                expect((cb.element as HTMLInputElement).disabled).toBe(true);
            }
        });

        it('does not create duplicate trackIds on the same frame', async () => {
            expect(getTrackIds(wrapper)).toEqual([1, 2]);

            const vm = wrapper.vm as any;
            vm.activate('merge');
            await wrapper.vm.$nextTick();
            const labels = wrapper.findAll('.label-container');
            await labels[0].trigger('click');
            await labels[1].trigger('click');
            await getButton(wrapper, 'Merge 2')!.trigger('click');

            const tracks = getObjTrackIds(wrapper, 'o1', 'o2');
            expect(tracks[0]).toBe(1);
            expect(tracks[1]).toBe(2);
        });

        it('exits merge mode after merging', async () => {
            const vm = wrapper.vm as any;
            vm.activate('merge');
            await wrapper.vm.$nextTick();
            const labels = wrapper.findAll('.label-container');
            await labels[0].trigger('click');
            await labels[1].trigger('click');
            await getButton(wrapper, 'Merge 2')!.trigger('click');
            expect(vm.activeMode).toBe('select');
            expect(wrapper.text()).not.toContain('Merge 2');
        });
    });

    describe('split', () => {
        beforeEach(async () => {
            const vm = wrapper.vm as any;
            vm.activate('split');
            await wrapper.vm.$nextTick();
        });

        it('exits split mode when cancelled', async () => {
            expect(wrapper.text()).toContain('Split out');
            await clickButton(wrapper, 'Cancel');
            expect((wrapper.vm as any).activeMode).toBe('select');
        });

        it('splits selected occurrences into a new trackId', async () => {
            const rows = wrapper.findAll('.timeline-row');
            expect(rows.length).toBeGreaterThanOrEqual(2);

            await rows[0].trigger('click');

            const dots = rows[0].findAll('.dot--selectable');
            expect(dots.length).toBeGreaterThan(0);

            await dots[0].trigger('click', { ctrlKey: false, shiftKey: false });

            const splitBtn = getButton(wrapper, 'Split out 1');
            expect(splitBtn).toBeTruthy();
            await splitBtn!.trigger('click');

            const trackIds = getTrackIds(wrapper);
            expect(trackIds).toContain(3);
        });
    });

    describe('dot selection', () => {
        beforeEach(async () => {
            const vm = wrapper.vm as any;
            vm.activate('split');
            await wrapper.vm.$nextTick();
        });

        it('selects a single dot on plain click', async () => {
            const rows = wrapper.findAll('.timeline-row');
            await rows[0].trigger('click');
            const dots = rows[0].findAll('.dot--selectable');
            await dots[0].trigger('click', { ctrlKey: false, shiftKey: false });
            expect(dots[0].classes()).toContain('dot--selected');
        });

        it('plain click on different dot switches selection', async () => {
            const rows = wrapper.findAll('.timeline-row');
            await rows[0].trigger('click');
            const dots = rows[0].findAll('.dot--selectable');

            await dots[0].trigger('click', { ctrlKey: false, shiftKey: false });
            expect(dots[0].classes()).toContain('dot--selected');

            await dots[1].trigger('click', { ctrlKey: false, shiftKey: false });
            expect(dots[0].classes()).not.toContain('dot--selected');
            expect(dots[1].classes()).toContain('dot--selected');
        });

        it('selects multiple dots with Ctrl+click', async () => {
            const rows = wrapper.findAll('.timeline-row');
            await rows[0].trigger('click');
            const dots = rows[0].findAll('.dot--selectable');

            await dots[0].trigger('click', { ctrlKey: false });
            await dots[1].trigger('click', { ctrlKey: true });

            expect(dots[0].classes()).toContain('dot--selected');
            expect(dots[1].classes()).toContain('dot--selected');
        });
    });

    describe('split mode exits after split', () => {
        it('exits split mode after splitting out', async () => {
            const vm = wrapper.vm as any;
            vm.activate('split');
            await wrapper.vm.$nextTick();
            const rows = wrapper.findAll('.timeline-row');
            await rows[0].trigger('click');
            const dots = rows[0].findAll('.dot--selectable');
            await dots[0].trigger('click', { ctrlKey: false, shiftKey: false });
            await getButton(wrapper, 'Split out 1')!.trigger('click');
            expect(vm.activeMode).toBe('select');
        });
    });

    describe('add', () => {
        it('enters add mode from the Add Object control', async () => {
            await clickButton(wrapper, 'Add Object');
            expect((wrapper.vm as any).activeMode).toBe('add');
            expect(wrapper.text()).toContain('Cancel');
        });

        it('toggles add mode on and off', async () => {
            await clickButton(wrapper, 'Add Object');
            expect((wrapper.vm as any).activeMode).toBe('add');
            await clickButton(wrapper, 'Cancel');
            expect((wrapper.vm as any).activeMode).toBe('select');
        });

        it('mode exclusivity: merge controls hide Add Object until cancelled', async () => {
            const vm = wrapper.vm as any;
            vm.activate('merge');
            await wrapper.vm.$nextTick();
            let toolsText = wrapper.find('[data-testid="review-tools"]').text();
            expect(toolsText).toContain('Merge');
            expect(toolsText).not.toContain('Add Object');

            await clickButton(wrapper, 'Cancel');
            expect(vm.activeMode).toBe('select');
            await clickButton(wrapper, 'Add Object');
            expect(vm.activeMode).toBe('add');
        });

        it('mode exclusivity: split controls hide Add Object until cancelled', async () => {
            const vm = wrapper.vm as any;
            vm.activate('split');
            await wrapper.vm.$nextTick();
            const toolsText = wrapper.find('[data-testid="review-tools"]').text();
            expect(toolsText).toContain('Split out');
            expect(toolsText).not.toContain('Add Object');

            await clickButton(wrapper, 'Cancel');
            expect(vm.activeMode).toBe('select');
            await clickButton(wrapper, 'Add Object');
            expect(vm.activeMode).toBe('add');
        });

        it('adds a new object via addBox with new trackId', async () => {
            const vm = wrapper.vm as any;
            const beforeCount = state.frames[0].detectedObjects.length;
            vm.addBox(100, 100, 50, 50, 'face', 'new');
            await wrapper.vm.$nextTick();
            const afterCount = state.frames[0].detectedObjects.length;
            expect(afterCount).toBe(beforeCount + 1);
            const added = state.frames[0].detectedObjects[state.frames[0].detectedObjects.length - 1];
            expect(added.x).toBe(100);
            expect(added.y).toBe(100);
            expect(added.width).toBe(50);
            expect(added.height).toBe(50);
            expect(added.className).toBe('face');
            expect(added.trackId).toBe(3);
        });

        it('falls back to a new trackId when adding with a track already used in the frame', async () => {
            const vm = wrapper.vm as any;
            vm.addBox(100, 100, 50, 50, 'other', 1);
            await wrapper.vm.$nextTick();
            const added = state.frames[0].detectedObjects[state.frames[0].detectedObjects.length - 1];
            expect(added.trackId).toBe(3);
            expect(added.className).toBe('other');
        });

        it('copies blur shape from the selected existing track', async () => {
            const vm = wrapper.vm as any;
            state.frames[1].detectedObjects.push({
                ...state.frames[1].detectedObjects[0],
                id: 'plate-track',
                analyzedFrameId: 'f2',
                trackId: 9,
                blurShape: 'rectangle',
            });

            vm.addBox(100, 100, 50, 50, 'other', 9);
            await wrapper.vm.$nextTick();

            const added = state.frames[0].detectedObjects[state.frames[0].detectedObjects.length - 1];
            expect(added.trackId).toBe(9);
            expect(added.className).toBe('other');
            expect(added.blurShape).toBe('rectangle');
        });

        it('existingTrackIds lists all unique trackIds', async () => {
            const vm = wrapper.vm as any;
            expect(vm.getFrames()[0].detectedObjects[0].trackId).toBe(1);
            const trackIds = new Set<number>();
            for (const f of state.frames) {
                for (const o of f.detectedObjects) {
                    if (o.trackId != null) trackIds.add(o.trackId);
                }
            }
            expect([...trackIds].sort()).toEqual([1, 2]);
        });
    });

    describe('adjust', () => {
        it('enters adjust mode from the inspector and pauses playback', async () => {
            await selectTrackOne(wrapper);
            await clickButton(wrapper, 'Adjust detection');
            const vm = wrapper.vm as any;
            expect(vm.activeMode).toBe('adjust');
        });

        it('done dispatches one adjust update with before state and returns to select', async () => {
            const onDetectedObjectUpdated = vi.fn();
            const mounted = mountEditor({ onDetectedObjectUpdated });
            wrapper = mounted.wrapper;
            state = mounted.state;

            await selectTrackOne(wrapper);
            await clickButton(wrapper, 'Adjust detection');
            const vm = wrapper.vm as any;
            expect(vm.activeMode).toBe('adjust');

            const obj = state.frames[0].detectedObjects[0];
            obj.x += 50;
            await wrapper.vm.$nextTick();

            await clickButton(wrapper, 'Done');
            expect(onDetectedObjectUpdated).toHaveBeenCalledTimes(1);
            const [videoId, analyzedFrameId, dto, operationType, beforeState] = onDetectedObjectUpdated.mock.calls[0];
            expect(videoId).toBe('v1');
            expect(analyzedFrameId).toBe('f1');
            expect(operationType).toBe('adjust');
            expect(dto.id).toBe('o1');
            expect(dto.x).toBe(50);
            expect(beforeState).toHaveLength(1);
            expect(beforeState[0].x).toBe(0);
            expect(vm.activeMode).toBe('select');
        });

        it('reset restores the before state', async () => {
            const onDetectedObjectUpdated = vi.fn();
            const mounted = mountEditor({ onDetectedObjectUpdated });
            wrapper = mounted.wrapper;
            state = mounted.state;

            await selectTrackOne(wrapper);
            await clickButton(wrapper, 'Adjust detection');
            const vm = wrapper.vm as any;

            const obj = state.frames[0].detectedObjects[0];
            obj.x += 50;
            await wrapper.vm.$nextTick();

            await clickButton(wrapper, 'Reset');
            expect(obj.x).toBe(0);
            expect(vm.activeMode).toBe('adjust');
            expect(onDetectedObjectUpdated).not.toHaveBeenCalled();
        });
    });

    describe('track forward', () => {
        it('tracks the selected occurrence from the Advanced menu', async () => {
            const onTrackForward = vi.fn();
            const mounted = mountEditor({ onTrackForward });
            wrapper = mounted.wrapper;
            state = mounted.state;

            await selectTrackOne(wrapper);
            await clickButton(wrapper, 'Advanced');
            await clickButton(wrapper, 'Track forward');

            expect(onTrackForward).toHaveBeenCalledTimes(1);
            expect(onTrackForward).toHaveBeenCalledWith('v1', 'f1', state.frames[0].detectedObjects[0]);
        });

        it('blocks duplicate tracking for the same object', async () => {
            const onTrackForward = vi.fn();
            const mounted = mountEditor({ onTrackForward });
            wrapper = mounted.wrapper;
            state = mounted.state;

            await selectTrackOne(wrapper);
            await clickButton(wrapper, 'Advanced');
            await clickButton(wrapper, 'Track forward');
            await clickButton(wrapper, 'Track forward');

            expect(onTrackForward).toHaveBeenCalledTimes(1);
        });
    });

    describe('getFrames', () => {
        it('returns deep-cloned state with mutations applied', () => {
            const vm = wrapper.vm as any;
            const frames = vm.getFrames();
            expect(Array.isArray(frames)).toBe(true);
            expect(frames.length).toBe(3);

            expect(frames[0].detectedObjects[0].id).toBe('o1');
            expect(frames[0].detectedObjects[0].trackId).toBe(1);
        });

        it('returns mutated state after merge', async () => {
            const vm = wrapper.vm as any;
            vm.activate('merge');
            await wrapper.vm.$nextTick();
            const labels = wrapper.findAll('.label-container');
            await labels[0].trigger('click');
            await labels[1].trigger('click');
            await getButton(wrapper, 'Merge 2')!.trigger('click');

            const frames = vm.getFrames();

            const tracksOnF1 = frames[0].detectedObjects.map((o: any) => o.trackId).sort();
            expect(tracksOnF1).toEqual([1, 2]);
        });
    });

});

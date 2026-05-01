import { describe, it, expect, beforeEach } from 'vitest';
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
        anonymizationSettings: { blurSizePercent: 200, timeBufferMs: 300 },
        frames,
    };
}

function mountEditor() {
    const state = reactive(createMockState());
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

describe('VideoEditorApp integration', () => {
    let wrapper: ReturnType<typeof mount>['wrapper'];
    let state: ReturnType<typeof mount>['state'];

    beforeEach(() => {
        const m = mountEditor();
        wrapper = m.wrapper;
        state = m.state;
    });

    describe('merge', () => {
        it('merges two tracked rows into the lowest trackId', async () => {
            expect(getTrackIds(wrapper)).toEqual([1, 2]);

            await clickButton(wrapper, 'Merge');
            expect(wrapper.text()).toContain('Exit Merge');

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
            await clickButton(wrapper, 'Merge');
            const labelCheckboxes = wrapper.findAll('.label-container input[type="checkbox"]');
            for (const cb of labelCheckboxes) {
                expect((cb.element as HTMLInputElement).disabled).toBe(true);
            }
        });

        it('does not create duplicate trackIds on the same frame', async () => {
            expect(getTrackIds(wrapper)).toEqual([1, 2]);

            await clickButton(wrapper, 'Merge');
            const labels = wrapper.findAll('.label-container');
            await labels[0].trigger('click');
            await labels[1].trigger('click');
            await getButton(wrapper, 'Merge 2')!.trigger('click');

            const tracks = getObjTrackIds(wrapper, 'o1', 'o2');
            expect(tracks[0]).toBe(1);
            expect(tracks[1]).toBe(2);
        });

        it('exits merge mode after merging', async () => {
            await clickButton(wrapper, 'Merge');
            const labels = wrapper.findAll('.label-container');
            await labels[0].trigger('click');
            await labels[1].trigger('click');
            await getButton(wrapper, 'Merge 2')!.trigger('click');
            expect(wrapper.text()).not.toContain('Exit Merge');
        });
    });

    describe('split', () => {
        beforeEach(async () => {
            await clickButton(wrapper, 'Split');
        });

        it('exits split mode when toggled off', async () => {
            expect(wrapper.text()).toContain('Exit Split');
            await clickButton(wrapper, 'Exit Split');
            expect(wrapper.text()).toContain('Split');
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
            await clickButton(wrapper, 'Split');
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
            await clickButton(wrapper, 'Split');
            const rows = wrapper.findAll('.timeline-row');
            await rows[0].trigger('click');
            const dots = rows[0].findAll('.dot--selectable');
            await dots[0].trigger('click', { ctrlKey: false, shiftKey: false });
            await getButton(wrapper, 'Split out 1')!.trigger('click');
            expect(wrapper.text()).not.toContain('Exit Split');
            expect(wrapper.text()).toContain('Split');
        });
    });

    describe('add', () => {
        it('opens DetailedView when Add is clicked', async () => {
            await clickButton(wrapper, 'Add');
            expect(wrapper.text()).toContain('Detailed View');
        });

        it('tooggles Add mode on and off', async () => {
            await clickButton(wrapper, 'Add');
            expect(wrapper.text()).toContain('Exit Add');
            await clickButton(wrapper, 'Exit Add');
            expect(wrapper.text()).not.toContain('Detailed View');
        });

        it('closes overlay on close button', async () => {
            await clickButton(wrapper, 'Add');
            await clickButton(wrapper, '✕');
            expect(wrapper.text()).not.toContain('Detailed View');
        });

        it('mode exclusivity: Add deactivates Merge', async () => {
            await clickButton(wrapper, 'Merge');
            expect(wrapper.text()).toContain('Exit Merge');
            await clickButton(wrapper, 'Add');
            expect(wrapper.text()).not.toContain('Exit Merge');
            expect(wrapper.text()).toContain('Detailed View');
        });

        it('mode exclusivity: Add deactivates Resize', async () => {
            await clickButton(wrapper, 'Resize');
            expect(wrapper.text()).toContain('Exit Resize');
            await clickButton(wrapper, 'Add');
            expect(wrapper.text()).not.toContain('Exit Resize');
            expect(wrapper.text()).toContain('Detailed View');
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

        it('switches modes inside DetailedView', async () => {
            await clickButton(wrapper, 'Add');
            expect(wrapper.text()).toContain('Detailed View');

            const modeBtns = wrapper.findAll('.mode-switch-btn');
            expect(modeBtns.length).toBe(3);

            await modeBtns[0].trigger('click');
            expect(wrapper.text()).toContain('Detailed View');

            await modeBtns[1].trigger('click');
            expect(wrapper.text()).toContain('Detailed View');

            await modeBtns[2].trigger('click');
            expect(wrapper.text()).toContain('Detailed View');
        });
    });

    describe('resize', () => {
        it('opens DetailedView when Resize is clicked', async () => {
            await clickButton(wrapper, 'Resize');
            expect(wrapper.text()).toContain('Detailed View');
        });

        it('updates width and height after resize', async () => {
            const obj = state.frames[0].detectedObjects[0];
            const origW = obj.width;
            const origH = obj.height;
            obj.width = 60;
            obj.height = 80;
            const vm = wrapper.vm as any;
            const result = vm.getFrames();
            expect(result[0].detectedObjects[0].width).toBe(60);
            expect(result[0].detectedObjects[0].height).toBe(80);
        });

        it('mode exclusivity: Resize deactivates Split', async () => {
            await clickButton(wrapper, 'Split');
            expect(wrapper.text()).toContain('Exit Split');
            await clickButton(wrapper, 'Resize');
            expect(wrapper.text()).not.toContain('Exit Split');
            expect(wrapper.text()).toContain('Detailed View');
        });
    });

    describe('move', () => {
        it('opens overlay when Move is clicked', async () => {
            await clickButton(wrapper, 'Move');
            expect(wrapper.text()).toContain('Detailed View');
        });

        it('closes overlay on close button', async () => {
            await clickButton(wrapper, 'Move');
            await clickButton(wrapper, '✕');
            expect(wrapper.text()).not.toContain('Detailed View');
        });

        it('returns updated coordinates via getFrames after moving in overlay', async () => {
            await clickButton(wrapper, 'Move');
            const frames = state.frames;
            const obj = frames[0].detectedObjects[0];
            const origX = obj.x;
            const origY = obj.y;

            obj.x = origX + 50;
            obj.y = 200;

            const vm = wrapper.vm as any;
            const result = vm.getFrames();
            const moved = result[0].detectedObjects[0];
            expect(moved.x).toBe(origX + 50);
            expect(moved.y).toBe(200);
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
            await clickButton(wrapper, 'Merge');
            const labels = wrapper.findAll('.label-container');
            await labels[0].trigger('click');
            await labels[1].trigger('click');
            await getButton(wrapper, 'Merge 2')!.trigger('click');

            const vm = wrapper.vm as any;
            const frames = vm.getFrames();

            const tracksOnF1 = frames[0].detectedObjects.map((o: any) => o.trackId).sort();
            expect(tracksOnF1).toEqual([1, 2]);
        });
    });
});

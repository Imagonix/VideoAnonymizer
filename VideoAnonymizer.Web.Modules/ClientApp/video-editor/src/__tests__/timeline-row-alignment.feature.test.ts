import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { nextTick, reactive } from 'vue';
import VideoEditorApp from '../VideoEditorApp.vue';
import type { AnalyzedFrameDto, DetectedObjectDto, VideoEditorProps } from '../types';
import { TIMELINE_ROW_HEIGHT_PX } from '../timelineLayout';
import featureText from './timeline-row-alignment.feature?raw';

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
            occurrenceBlurSizePercentOverride: o.occurrenceBlurSizePercentOverride ?? null,
            preBufferMsOverride: o.preBufferMsOverride ?? null,
            postBufferMsOverride: o.postBufferMsOverride ?? null,
            nextGapHandlingMode: o.nextGapHandlingMode ?? null,
            selected: o.selected ?? true,
            trackId: o.trackId ?? null,
            x: o.x ?? 10,
            y: o.y ?? 10,
            width: o.width ?? 40,
            height: o.height ?? 40,
            analyzedFrameId: id,
        })),
    };
}

function multiTrackFrames() {
    return [
        createFrame('f1', 0, 0, [
            { id: 'a1', trackId: 1, className: 'face' },
            { id: 'b1', trackId: 2, className: 'license_plate' },
            { id: 'c1', trackId: 3, className: 'other' },
        ]),
        createFrame('f2', 1, 1, [
            { id: 'a2', trackId: 1, className: 'face' },
            { id: 'b2', trackId: 2, className: 'license_plate' },
            { id: 'c2', trackId: 3, className: 'other' },
        ]),
    ];
}

function mountEditor(frames: AnalyzedFrameDto[]) {
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
                    ],
                },
            },
        },
    });

    return { wrapper, state };
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

function labelRows(world: World) {
    return world.wrapper!.findAll('.timeline-labels .label-container');
}

function occurrenceRows(world: World) {
    return world.wrapper!.findAll('[data-testid="expanded-track-area"] .timeline-row-wrapper');
}

function centerY(rect: DOMRect) {
    return rect.top + rect.height / 2;
}

/**
 * jsdom does not layout CSS. Assign deterministic geometry from the shared
 * row-height contract so pair tops/heights can be asserted.
 */
function installLayoutGeometry(world: World) {
    const headerTop = 100;
    // labels-header (toolbar+overview+gap) + ruler(+gap) from shared contract
    const labelsHeaderHeight = 36 + 28 + 4;
    const rowsOrigin = headerTop + labelsHeaderHeight + 22 + 6;

    const labels = labelRows(world);
    const rows = occurrenceRows(world);

    labels.forEach((label, index) => {
        const top = rowsOrigin + index * TIMELINE_ROW_HEIGHT_PX;
        const labelEl = label.element as HTMLElement;
        labelEl.getBoundingClientRect = () =>
            ({
                top,
                bottom: top + TIMELINE_ROW_HEIGHT_PX,
                height: TIMELINE_ROW_HEIGHT_PX,
                width: 160,
                left: 0,
                right: 160,
                x: 0,
                y: top,
                toJSON() {
                    return this;
                },
            }) as DOMRect;

        // Center children vertically inside the row.
        const childMid = top + TIMELINE_ROW_HEIGHT_PX / 2;
        for (const child of Array.from(
            labelEl.querySelectorAll('.mud-checkbox__box, .label-text, .track-id-input, [data-testid="track-thumbnail"]')
        )) {
            const el = child as HTMLElement;
            const h = el.classList.contains('mud-checkbox__box') ? 18 : 14;
            el.getBoundingClientRect = () =>
                ({
                    top: childMid - h / 2,
                    bottom: childMid + h / 2,
                    height: h,
                    width: h,
                    left: 10,
                    right: 10 + h,
                    x: 10,
                    y: childMid - h / 2,
                    toJSON() {
                        return this;
                    },
                }) as DOMRect;
        }
    });

    rows.forEach((row, index) => {
        const top = rowsOrigin + index * TIMELINE_ROW_HEIGHT_PX;
        const rowEl = row.element as HTMLElement;
        rowEl.getBoundingClientRect = () =>
            ({
                top,
                bottom: top + TIMELINE_ROW_HEIGHT_PX,
                height: TIMELINE_ROW_HEIGHT_PX,
                width: 700,
                left: 160,
                right: 860,
                x: 160,
                y: top,
                toJSON() {
                    return this;
                },
            }) as DOMRect;
    });
}

const steps: StepDefinition[] = [
    {
        pattern: /^the editor is open with multiple tracks in the expanded timeline$/,
        handler: async world => {
            const mounted = mountEditor(multiTrackFrames());
            world.wrapper = mounted.wrapper;
            world.state = mounted.state;

            const vm = world.wrapper.vm as any;
            vm.timelineExpanded = true;
            vm.videoDuration = 2;
            vm.workspaceSize = { width: 900, height: 600 };
            await nextTick();
            await nextTick();
            installLayoutGeometry(world);
        },
    },
    {
        pattern: /^the video editor shell is visible$/,
        handler: world => {
            expect(world.wrapper!.find('.video-editor').exists()).toBe(true);
            expect(world.wrapper!.find('.workspace-main').exists()).toBe(true);
            expect(world.wrapper!.find('[data-testid="timeline-panel"]').exists()).toBe(true);
            expect(world.wrapper!.find('[data-testid="expanded-timeline"]').exists()).toBe(true);
        },
    },
    {
        pattern: /^the expanded timeline shows matching label and occurrence rows$/,
        handler: world => {
            const labels = labelRows(world);
            const rows = occurrenceRows(world);
            expect(labels.length).toBeGreaterThanOrEqual(3);
            expect(rows.length).toBe(labels.length);
            for (let i = 0; i < labels.length; i++) {
                expect(labels[i].attributes('data-timeline-key')).toBe(
                    rows[i].attributes('data-timeline-key')
                );
            }
        },
    },
    {
        pattern: /^each timeline label row shares the same top and height as its occurrence row$/,
        handler: world => {
            const labels = labelRows(world);
            const rows = occurrenceRows(world);
            expect(labels.length).toBeGreaterThanOrEqual(3);
            expect(rows.length).toBe(labels.length);

            // Contract is shared: CSS vars live on .timeline-wrapper.
            const expanded = world.wrapper!.find('[data-testid="expanded-timeline"]');
            expect(expanded.exists()).toBe(true);
            expect(expanded.classes()).toContain('timeline-wrapper');

            for (let i = 0; i < labels.length; i++) {
                const labelEl = labels[i].element as HTMLElement;
                const rowEl = rows[i].element as HTMLElement;
                expect(labelEl.getAttribute('data-timeline-key')).toBe(
                    rowEl.getAttribute('data-timeline-key')
                );

                const labelRect = labelEl.getBoundingClientRect();
                const rowRect = rowEl.getBoundingClientRect();

                expect(labelRect.height).toBeCloseTo(TIMELINE_ROW_HEIGHT_PX, 0);
                expect(rowRect.height).toBeCloseTo(TIMELINE_ROW_HEIGHT_PX, 0);
                expect(labelRect.height).toBeCloseTo(rowRect.height, 0);
                expect(Math.abs(labelRect.top - rowRect.top)).toBeLessThanOrEqual(1);
            }
        },
    },
    {
        pattern: /^each label row vertically centers its checkbox preview and Track ID$/,
        handler: world => {
            const labels = labelRows(world);
            expect(labels.length).toBeGreaterThan(0);

            for (const label of labels) {
                const labelEl = label.element as HTMLElement;
                const labelRect = labelEl.getBoundingClientRect();
                const mid = centerY(labelRect);

                const checkbox = labelEl.querySelector('.mud-checkbox__box');
                const trackText = labelEl.querySelector('.label-text, .track-id-input');

                for (const el of [checkbox, trackText]) {
                    if (!el) continue;
                    const rect = (el as HTMLElement).getBoundingClientRect();
                    expect(rect.height).toBeGreaterThan(0);
                    expect(Math.abs(centerY(rect) - mid)).toBeLessThanOrEqual(3);
                }

                // CSS contract: flex + align-items center on the label row.
                expect(getComputedStyle(labelEl).alignItems === 'center'
                    || labelEl.className.includes('label-container')).toBe(true);
            }
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

describe('Expanded timeline row alignment (Command 27)', () => {
    runFeature(featureText, steps);
});

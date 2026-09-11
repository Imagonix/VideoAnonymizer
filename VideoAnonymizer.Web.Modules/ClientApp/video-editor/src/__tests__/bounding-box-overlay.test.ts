import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import BoundingBoxOverlay from '../BoundingBoxOverlay.vue';
import type { PreviewObject } from '../types';

describe('BoundingBoxOverlay', () => {
    it('projects native video coordinates onto the displayed video area', () => {
        const object: PreviewObject = {
            activation: 'detected',
            detectedObject: {
                id: 'face-1',
                confidence: 0.9,
                className: 'face',
                selected: true,
                trackId: 1,
                x: 320,
                y: 180,
                width: 160,
                height: 90,
                analyzedFrameId: 'frame-1',
            },
        };

        const wrapper = mount(BoundingBoxOverlay, {
            props: {
                objects: [object],
                anonymizationSettings: { blurSizePercent: 100, timeBufferMs: 300, interpolateTrackedObjects: true },
                videoDimensions: {
                    videoWidth: 1280,
                    videoHeight: 720,
                    displayWidth: 1200,
                    displayHeight: 675,
                },
                highlightedRowKey: null,
                splitSourceKey: null,
                alwaysShowKeys: new Set<string>(),
                selectedKey: null,
                mode: 'select',
                adjustObject: null,
            },
        });

        const box = wrapper.get('[data-testid="bounding-box"]');

        expect(box.attributes('style')).toContain('left: 25%');
        expect(box.attributes('style')).toContain('top: 25%');
        expect(box.attributes('style')).toContain('width: 12.5%');
        expect(box.attributes('style')).toContain('height: 12.5%');
    });

    it('renders rectangle blur outlines when requested by the detected object', () => {
        const object: PreviewObject = {
            activation: 'detected',
            detectedObject: {
                id: 'plate-1',
                confidence: 0.9,
                className: 'license_plate',
                blurShape: 'rectangle',
                selected: true,
                trackId: 1,
                x: 100,
                y: 120,
                width: 160,
                height: 40,
                analyzedFrameId: 'frame-1',
            },
        };

        const wrapper = mount(BoundingBoxOverlay, {
            props: {
                objects: [object],
                anonymizationSettings: { blurSizePercent: 100, timeBufferMs: 300, interpolateTrackedObjects: true },
                videoDimensions: null,
                highlightedRowKey: null,
                splitSourceKey: null,
                alwaysShowKeys: new Set<string>(),
                selectedKey: null,
                mode: 'select',
                adjustObject: null,
            },
        });

        expect(wrapper.get('[data-testid="blur-area-outline"]').classes())
            .toContain('blur-area-outline--rectangle');
    });

    it('renders excluded occurrences as ghost outlines without blur fill', () => {
        const object: PreviewObject = {
            activation: 'detected',
            detectedObject: {
                id: 'face-excluded',
                confidence: 0.9,
                className: 'face',
                selected: false,
                trackId: 3,
                x: 50,
                y: 60,
                width: 40,
                height: 50,
                analyzedFrameId: 'frame-1',
            },
        };

        const wrapper = mount(BoundingBoxOverlay, {
            props: {
                objects: [object],
                anonymizationSettings: { blurSizePercent: 150, timeBufferMs: 300, interpolateTrackedObjects: true },
                videoDimensions: null,
                highlightedRowKey: null,
                splitSourceKey: null,
                alwaysShowKeys: new Set<string>(),
                selectedKey: null,
                mode: 'select',
                adjustObject: null,
            },
        });

        const box = wrapper.get('[data-testid="bounding-box"]');
        expect(box.classes()).toContain('bbox--excluded');
        expect(wrapper.find('[data-testid="blur-area-outline"]').exists()).toBe(false);
        expect(wrapper.get('[data-excluded="true"]').exists()).toBe(true);
        expect(box.attributes('aria-label')).toContain('excluded');
        expect(box.attributes('tabindex')).toBe('0');
    });
});

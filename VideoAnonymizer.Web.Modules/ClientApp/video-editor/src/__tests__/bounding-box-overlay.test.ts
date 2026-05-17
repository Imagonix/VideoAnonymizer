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
                anonymizationSettings: { blurSizePercent: 100, timeBufferMs: 300 },
                videoDimensions: {
                    videoWidth: 1280,
                    videoHeight: 720,
                    displayWidth: 1200,
                    displayHeight: 675,
                },
                highlightedRowKey: null,
                splitSourceKey: null,
                alwaysShowKeys: new Set<string>(),
            },
        });

        const box = wrapper.get('[data-testid="bounding-box"]');

        expect(box.attributes('style')).toContain('left: 25%');
        expect(box.attributes('style')).toContain('top: 25%');
        expect(box.attributes('style')).toContain('width: 12.5%');
        expect(box.attributes('style')).toContain('height: 12.5%');
    });
});

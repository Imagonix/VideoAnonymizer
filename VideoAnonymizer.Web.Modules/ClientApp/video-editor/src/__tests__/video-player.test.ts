import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import VideoPlayer from '../VideoPlayer.vue';

function mountPlayer(initialTime: number) {
    return mount(VideoPlayer, {
        props: { videoSourceUrl: 'http://example.com/v.mp4', currentTime: initialTime },
    });
}

function videoEl(wrapper: ReturnType<typeof mountPlayer>) {
    return (wrapper.vm as any).videoRef as HTMLVideoElement;
}

describe('VideoPlayer seek application', () => {
    it('applies a seek beyond the intentional 50 ms tolerance', async () => {
        const wrapper = mountPlayer(0);
        const video = videoEl(wrapper);
        expect(video).toBeTruthy();
        video.currentTime = 0;

        await wrapper.setProps({ currentTime: 0.06 });

        expect(video.currentTime).toBe(0.06);
    });

    it('does not seek within the intentional 50 ms tolerance', async () => {
        const wrapper = mountPlayer(0);
        const video = videoEl(wrapper);
        expect(video).toBeTruthy();
        video.currentTime = 0;

        await wrapper.setProps({ currentTime: 0.04 });

        expect(video.currentTime).toBe(0);
    });

    it('still applies a far seek when far from the current position', async () => {
        const wrapper = mountPlayer(0.04);
        const video = videoEl(wrapper);
        expect(video).toBeTruthy();
        video.currentTime = 0.04;

        await wrapper.setProps({ currentTime: 2 });

        expect(video.currentTime).toBe(2);
    });
});

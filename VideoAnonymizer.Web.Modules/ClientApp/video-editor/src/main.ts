import { createApp, reactive } from 'vue';
import VideoEditorApp from './VideoEditorApp.vue';
import type { VideoEditorProps } from './types';

type AppHandle = {
    update: (nextProps: VideoEditorProps) => void;
    unmount: () => void;
    getFrames: () => any[]
};

declare global {
    interface Window {
        __videoEditorVueLoader?: Promise<unknown>;
        mountVideoEditorVueApp?: (element: HTMLElement, props: VideoEditorProps) => AppHandle;
    }
}

window.mountVideoEditorVueApp = (element: HTMLElement, props: VideoEditorProps): AppHandle => {
    const state = reactive<VideoEditorProps>({
        videoId: props.videoId,
        videoSourceUrl: props.videoSourceUrl,
        frames: props.frames ?? [],
        anonymizationSettings: props.anonymizationSettings
    });

    const app = createApp(VideoEditorApp, { state });
    const vm = app.mount(element) as {
        getFrames?: () => any[]
    };

    return {
        update(nextProps: VideoEditorProps) {
            state.videoId = nextProps.videoId;
            state.videoSourceUrl = nextProps.videoSourceUrl;
            state.frames = nextProps.frames ?? [];
            state.anonymizationSettings = nextProps.anonymizationSettings;
        },
        getFrames() {
            return vm.getFrames?.() ?? []
        },
        unmount() {
            app.unmount();
        }
    };
};
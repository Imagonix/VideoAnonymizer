import { createApp, reactive } from 'vue';
import VideoEditorApp from './VideoEditorApp.vue';
import type { VideoEditorProps } from './types';

type AppHandle = {
    update: (nextProps: VideoEditorProps) => void;
    updateSettings: (settings: AnonymizationSettings) => void;
    unmount: () => void;
    getFrames: () => any[]
};

type AnonymizationSettings = {
    blurSizePercent: number;
    timeBufferMs: number;
};

declare global {
    interface Window {
        __videoEditorVueLoader?: Promise<unknown>;
        mountVideoEditorVueApp?: (element: HTMLElement, props: VideoEditorProps) => AppHandle;
    }
}

const callbackKeys: (keyof VideoEditorProps)[] = [
    'onDetectedObjectAdded',
    'onDetectedObjectUpdated',
    'onDetectedObjectsBulkUpdated',
];

window.mountVideoEditorVueApp = (element: HTMLElement, props: VideoEditorProps): AppHandle => {
    const callbacks: Partial<VideoEditorProps> = {};
    for (const key of callbackKeys) {
        if (props[key] !== undefined) {
            callbacks[key] = props[key] as any;
        }
    }

    const state = reactive<VideoEditorProps>({
        videoId: props.videoId,
        videoSourceUrl: props.videoSourceUrl,
        frames: props.frames ?? [],
        anonymizationSettings: props.anonymizationSettings,
        ...callbacks,
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
        updateSettings(settings: AnonymizationSettings) {
            state.anonymizationSettings.blurSizePercent = settings.blurSizePercent;
            state.anonymizationSettings.timeBufferMs = settings.timeBufferMs;
        },
        getFrames() {
            return vm.getFrames?.() ?? []
        },
        unmount() {
            app.unmount();
        }
    };
};

import { createApp, reactive } from 'vue';
import VideoEditorApp from './VideoEditorApp.vue';
import type { VideoEditorProps } from './types';

type AppHandle = {
  update: (nextProps: VideoEditorProps) => void;
  unmount: () => void;
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
    frames: props.frames ?? []
  });

  const app = createApp(VideoEditorApp, { state });
  app.mount(element);

  return {
    update(nextProps: VideoEditorProps) {
      state.videoId = nextProps.videoId;
      state.videoSourceUrl = nextProps.videoSourceUrl;
      state.frames = nextProps.frames ?? [];
    },
    unmount() {
      app.unmount();
    }
  };
};
import { createApp, h, ref, onMounted } from 'vue'
import VideoEditor from './VideoEditorApp.vue'

createApp({
  setup() {
    const frames = ref([])
    const videoSourceUrl = '/sample.frame-analysis.mp4'

    onMounted(async () => {
      const res = await fetch('/sample.frame-analysis.json')
      frames.value = await res.json()
    })

    return () =>
      h(VideoEditor, {
        state: {
          videoId: '00000000-0000-0000-0000-000000000001',
          videoSourceUrl,
          frames: frames.value,
          anonymizationSettings: { blurSizePercent: 200, timeBufferMs: 300 }
        }
      })
  }
}).mount('#app')
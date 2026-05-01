<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted, computed } from 'vue';

const props = defineProps<{
  videoSourceUrl: string;
  currentTime: number;
}>();

onMounted(() => {
  videoRef.value?.requestVideoFrameCallback(onFrame)
})

let stopped = false;
onUnmounted(() => {
  stopped = true;
});

function onFrame(_: number, metadata: VideoFrameCallbackMetadata) {
  if(stopped){
    return;
  }
  videoRef.value?.requestVideoFrameCallback(onFrame)
  if (videoRef.value) emit('time-update', metadata.mediaTime);
}

const emit = defineEmits<{
  (e: 'time-update', time: number): void;
  (e: 'loaded', duration: number): void;
  (e: 'play-state-change', isPlaying: boolean): void;
  (e: 'volume-change', volume: number): void;
}>();

const videoRef = ref<HTMLVideoElement | null>(null);
const isPlaying = ref(false);

const videoDimensions = computed(() => {
  const el = videoRef.value;
  if (!el || !el.videoWidth) return null;
  return {
    videoWidth: el.videoWidth,
    videoHeight: el.videoHeight,
    displayWidth: el.offsetWidth,
    displayHeight: el.offsetHeight
  };
});

function onLoaded() {
  if (videoRef.value) emit('loaded', videoRef.value.duration);
}

async function togglePlayback() {
  const video = videoRef.value;
  if (!video) return;

  if (video.paused || video.ended) {
    try {
      await video.play();
    } catch {
      isPlaying.value = false;
    }
  } else {
    video.pause();
  }
}

function onPlay() {
  isPlaying.value = true;
  emit('play-state-change', true);
}

function onPause() {
  isPlaying.value = false;
  emit('play-state-change', false);
}

function onEnded() {
  isPlaying.value = false;
  emit('play-state-change', false);
}

function setVolume(nextVolume: number) {
  const clampedVolume = Math.max(0, Math.min(1, nextVolume));

  if (!videoRef.value) return;
  videoRef.value.volume = clampedVolume;
  videoRef.value.muted = clampedVolume === 0;
  emit('volume-change', clampedVolume);
}

watch(() => props.currentTime, (t) => {
  if (videoRef.value && Math.abs(videoRef.value.currentTime - t) > 0.05) {
    videoRef.value.currentTime = t;
  }
});

defineExpose({
  setVolume,
  togglePlayback,
  videoRef,
  videoDimensions
});
</script>

<template>
  <video
    ref="videoRef"
    class="video-player-video"
    :src="videoSourceUrl"
    playsinline
    @loadedmetadata="onLoaded"
    @play="onPlay"
    @pause="onPause"
    @ended="onEnded"
  />
</template>

<style scoped>
.video-player-video {
  display: block;
  max-width: 100%;
  background: #000;
}
</style>

<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted } from 'vue';
import type { VideoDimensions } from './types';

const props = defineProps<{
  videoSourceUrl: string;
  currentTime: number;
}>();

let resizeObserver: ResizeObserver | null = null;

onMounted(() => {
  const video = videoRef.value;
  video?.requestVideoFrameCallback(onFrame);
  if (!video) return;

  updateVideoDimensions();
  resizeObserver = new ResizeObserver(updateVideoDimensions);
  resizeObserver.observe(video);
})

let stopped = false;
onUnmounted(() => {
  stopped = true;
  resizeObserver?.disconnect();
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
const videoDimensions = ref<VideoDimensions | null>(null);

function updateVideoDimensions() {
  const el = videoRef.value;
  if (!el || !el.videoWidth || !el.videoHeight) {
    videoDimensions.value = null;
    return;
  }

  const rect = el.getBoundingClientRect();
  videoDimensions.value = {
    videoWidth: el.videoWidth,
    videoHeight: el.videoHeight,
    displayWidth: rect.width,
    displayHeight: rect.height
  };
}

function onLoaded() {
  updateVideoDimensions();
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
  width: 100%;
  height: 100%;
  object-fit: contain;
  background: #000;
}
</style>

<script setup lang="ts">
import { ref, watch } from 'vue';

const props = defineProps<{
  videoSourceUrl: string;
  currentTime: number;
}>();

const emit = defineEmits<{
  (e: 'time-update', time: number): void;
  (e: 'loaded', duration: number): void;
}>();

const videoRef = ref<HTMLVideoElement | null>(null);

function onTimeUpdate() {
  if (videoRef.value) emit('time-update', videoRef.value.currentTime);
}

function onLoaded() {
  if (videoRef.value) emit('loaded', videoRef.value.duration);
}

watch(() => props.currentTime, (t) => {
  if (videoRef.value && Math.abs(videoRef.value.currentTime - t) > 0.05) {
    videoRef.value.currentTime = t;
  }
});
</script>

<template>
  <video
    ref="videoRef"
    :src="videoSourceUrl"
    controls
    @timeupdate="onTimeUpdate"
    @loadedmetadata="onLoaded"
  />
</template>
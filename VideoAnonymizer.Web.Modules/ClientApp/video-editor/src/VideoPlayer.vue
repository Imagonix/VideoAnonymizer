<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted } from 'vue';

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
}>();

const videoRef = ref<HTMLVideoElement | null>(null);

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
    @loadedmetadata="onLoaded"
  />
</template>
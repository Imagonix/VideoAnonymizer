<script setup lang="ts">
const props = defineProps<{
  src: string;
  currentTime: number;
}>();

const emit = defineEmits<{
  (e: 'timeupdate', time: number): void;
  (e: 'loaded', duration: number): void;
}>();

const videoRef = ref<HTMLVideoElement>();

function onTimeUpdate() {
  emit('timeupdate', videoRef.value!.currentTime);
}

function onLoaded() {
  emit('loaded', videoRef.value!.duration);
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
    :src="src"
    controls
    @timeupdate="onTimeUpdate"
    @loadedmetadata="onLoaded"
  />
</template>
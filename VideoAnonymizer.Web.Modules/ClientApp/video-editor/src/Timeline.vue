<script setup lang="ts">
defineProps<{
  duration: number;
  currentTime: number;
}>();

const emit = defineEmits<{
  (e: 'seek', time: number): void;
}>();

const trackRef = ref<HTMLElement>();

function onClick(e: MouseEvent) {
  const rect = trackRef.value!.getBoundingClientRect();
  const ratio = (e.clientX - rect.left) / rect.width;
  emit('seek', ratio * props.duration);
}
</script>

<template>
  <div data-testid="timeline" ref="trackRef" @click="onClick">
    <PlaybackIndicator :time="currentTime" :duration="duration" />
    <slot />
  </div>
</template>
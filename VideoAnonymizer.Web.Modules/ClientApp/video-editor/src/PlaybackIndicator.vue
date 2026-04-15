<script setup lang="ts">
const props = defineProps<{
  time: number;
  duration: number;
}>();

const left = computed(() =>
  `${(props.time / props.duration) * 100}%`
);
</script>

<template>
  <div data-testid="playback-indicator" :style="{ left }">
    <div data-testid="playback-timestamp">
      {{ time.toFixed(1) }}s
    </div>
  </div>
</template>
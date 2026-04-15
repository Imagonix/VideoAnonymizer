<script setup lang="ts">
import { computed } from 'vue';
const props = defineProps<{
  time: number;
  duration: number;
  active: boolean;
  color: string;
}>();

const left = computed(() => `${(props.time / props.duration) * 100}%`);
</script>

<template>
  <div
    class="timeline-marker"
    :data-testid="active ? 'timeline-segment-colored' : 'timeline-segment-gray'"
    :style="{
      left,
      backgroundColor: active ? color : '#9ca3af'
    }"
  />
</template>
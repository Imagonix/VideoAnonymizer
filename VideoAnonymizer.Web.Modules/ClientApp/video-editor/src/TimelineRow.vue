<script setup lang="ts">
import { computed } from 'vue';
import TimelineMarker from './TimelineMarker.vue';

const props = defineProps<{
  objectKey: string;
  color: string;
  segments: { timeSeconds: number; selected: boolean }[];
  duration: number;
}>();

const hasContinuous = computed(() => {
  if (props.segments.length < 2) return false;
  for (let i = 1; i < props.segments.length; i++) {
    if (Math.abs(props.segments[i].timeSeconds - props.segments[i - 1].timeSeconds) < 1.1) {
      return true;
    }
  }
  return false;
});
</script>

<template>
  <div class="timeline-row">
    <div
      v-if="hasContinuous"
      class="timeline-segment-continuous"
      data-testid="timeline-segment-continuous"
      :style="{ backgroundColor: color }"
    />
    <TimelineMarker
      v-for="s in segments"
      :key="s.timeSeconds"
      :time="s.timeSeconds"
      :duration="duration"
      :active="s.selected"
      :color="color"
    />
  </div>
</template>
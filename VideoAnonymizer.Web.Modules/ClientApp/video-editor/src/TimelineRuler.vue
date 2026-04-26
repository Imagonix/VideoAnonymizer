<script setup lang="ts">
import { computed } from 'vue';
import { clamp, formatTimelineTime, type TimelineTick } from './timelineUtils';

const props = defineProps<{
  currentTime: number;
  duration: number;
  ticks: TimelineTick[];
}>();

const currentTimeLeft = computed(() => {
  if (!props.duration || props.duration <= 0) return '0%';
  const ratio = clamp(props.currentTime / props.duration, 0, 1);
  return `${ratio * 100}%`;
});

const formattedCurrentTime = computed(() => formatTimelineTime(props.currentTime));
</script>

<template>
  <div class="timeline-ruler">
    <div v-for="tick in ticks" :key="tick.time" class="timeline-tick" :style="{ left: tick.left }">
      <span>{{ tick.label }}</span>
    </div>
    <div class="timeline-current-time-marker" :style="{ left: currentTimeLeft }">
      {{ formattedCurrentTime }}
    </div>
  </div>
</template>

<style scoped>
.timeline-ruler {
  position: relative;
  height: 34px;
  margin-bottom: 12px;
  border-bottom: 1px solid var(--mud-palette-lines-default);
  background: var(--mud-palette-surface);
}

.timeline-tick {
  position: absolute;
  top: 0;
  bottom: 0;
  width: 1px;
  transform: translateX(-50%);
  background: var(--mud-palette-lines-default);
  color: var(--mud-palette-text-secondary);
  pointer-events: none;
}

.timeline-tick span {
  position: absolute;
  top: 6px;
  left: 6px;
  font-size: 11px;
  line-height: 1;
  white-space: nowrap;
  font-variant-numeric: tabular-nums;
}

.timeline-current-time-marker {
  position: absolute;
  top: 50%;
  transform: translate(-50%, -50%);
  z-index: 100;
  white-space: nowrap;
  font-size: 12px;
  line-height: 1;
  pointer-events: none;
  color: var(--mud-palette-text-primary);
  background: var(--mud-palette-surface);
  padding: 2px 8px;
  border: 1px solid var(--mud-palette-primary);
  border-radius: 4px;
  font-variant-numeric: tabular-nums;
}
</style>

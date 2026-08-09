<script setup lang="ts">
import { computed } from 'vue';
import { clamp, formatTimelineTime, type TimelineTick } from './timelineUtils';

const props = withDefaults(defineProps<{
  currentTime: number;
  duration: number;
  ticks: TimelineTick[];
  compact?: boolean;
}>(), {
  compact: false,
});

const currentTimeLeft = computed(() => {
  if (!props.duration || props.duration <= 0) return '0%';
  const ratio = clamp(props.currentTime / props.duration, 0, 1);
  return `${ratio * 100}%`;
});

const formattedCurrentTime = computed(() => formatTimelineTime(props.currentTime));

/** Show enough labels for multi-label ruler; skip only the final end tick to reduce overflow. */
function showTickLabel(index: number): boolean {
  if (props.ticks.length <= 1) return true;
  return index < props.ticks.length - 1;
}
</script>

<template>
  <div
    class="timeline-ruler"
    :class="{ 'timeline-ruler--compact': compact }"
    data-testid="timeline-ruler"
  >
    <div v-for="(tick, index) in ticks" :key="tick.time" class="timeline-tick" :style="{ left: tick.left }">
      <span v-if="showTickLabel(index)">{{ tick.label }}</span>
    </div>
    <div class="timeline-current-time-marker" :style="{ left: currentTimeLeft }">
      {{ formattedCurrentTime }}
    </div>
  </div>
</template>

<style scoped>
.timeline-ruler {
  position: relative;
  height: var(--timeline-ruler-height, 22px);
  min-height: var(--timeline-ruler-height, 22px);
  max-height: var(--timeline-ruler-height, 22px);
  margin-bottom: var(--timeline-ruler-gap, 6px);
  box-sizing: border-box;
  border-bottom: 1px solid var(--mud-palette-lines-default);
  background: var(--mud-palette-surface);
}

.timeline-ruler--compact {
  height: 18px;
  margin-bottom: 0;
  border-bottom: 0;
  background: transparent;
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
  top: 4px;
  left: 4px;
  font-size: 10px;
  line-height: 1;
  white-space: nowrap;
  font-variant-numeric: tabular-nums;
}

.timeline-ruler--compact .timeline-tick span {
  top: 3px;
  left: 3px;
  font-size: 10px;
}

.timeline-current-time-marker {
  position: absolute;
  top: 50%;
  transform: translate(-50%, -50%);
  z-index: 100;
  white-space: nowrap;
  font-size: 11px;
  line-height: 1;
  pointer-events: none;
  color: var(--mud-palette-text-primary);
  background: var(--mud-palette-surface);
  padding: 1px 6px;
  border: 1px solid var(--mud-palette-primary);
  border-radius: 4px;
  font-variant-numeric: tabular-nums;
}

.timeline-ruler--compact .timeline-current-time-marker {
  font-size: 10px;
  padding: 1px 5px;
}
</style>

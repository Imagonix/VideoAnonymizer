<script setup lang="ts">
import { computed, ref } from 'vue';
import type { TimelineObjectCount } from './types';
import { clamp } from './timelineUtils';

const props = defineProps<{
  duration: number;
  currentTime: number;
  viewportStartRatio: number;
  viewportEndRatio: number;
  objectCounts: TimelineObjectCount[];
}>();

const emit = defineEmits<{
  (e: 'seek', time: number): void;
}>();

const overviewRef = ref<HTMLElement | null>(null);

const maxCount = computed(() => {
  return Math.max(1, ...props.objectCounts.map(x => x.count));
});

const objectCountLinePoints = computed(() => {
  if (!props.duration || props.duration <= 0 || props.objectCounts.length === 0) {
    return '';
  }

  return props.objectCounts
    .map(point => {
      const x = clamp(point.timeSeconds / props.duration, 0, 1) * 100;
      const y = 28 - (point.count / maxCount.value) * 22;
      return `${x.toFixed(3)},${y.toFixed(3)}`;
    })
    .join(' ');
});

const currentTimeLeft = computed(() => {
  if (!props.duration || props.duration <= 0) return '0%';
  return `${clamp(props.currentTime / props.duration, 0, 1) * 100}%`;
});

const viewportHighlightStyle = computed(() => {
  const start = clamp(props.viewportStartRatio, 0, 1);
  const end = clamp(Math.max(props.viewportEndRatio, start), 0, 1);

  return {
    left: `${start * 100}%`,
    width: `${Math.max(0, end - start) * 100}%`
  };
});

const currentObjectCount = computed(() => {
  if (props.objectCounts.length === 0) return 0;

  return [...props.objectCounts].sort((a, b) => {
    return Math.abs(a.timeSeconds - props.currentTime) - Math.abs(b.timeSeconds - props.currentTime);
  })[0].count;
});

function onClick(e: MouseEvent) {
  if (!overviewRef.value || !props.duration || props.duration <= 0) return;

  const rect = overviewRef.value.getBoundingClientRect();
  const ratio = clamp((e.clientX - rect.left) / rect.width, 0, 1);
  emit('seek', ratio * props.duration);
}
</script>

<template>
  <div class="timeline-overview" ref="overviewRef" data-testid="timeline-overview" @click="onClick">
    <div class="timeline-overview-highlight" :style="viewportHighlightStyle"></div>
    <svg class="timeline-overview-chart" viewBox="0 0 100 32" preserveAspectRatio="none" aria-hidden="true">
      <polyline
        v-if="objectCountLinePoints"
        class="timeline-overview-count-line"
        :points="objectCountLinePoints"
      />
    </svg>
    <div class="timeline-overview-playhead" :style="{ left: currentTimeLeft }"></div>
    <div class="timeline-overview-count">{{ currentObjectCount }} detected</div>
  </div>
</template>

<style scoped>
.timeline-overview {
  margin-right: 9px;
  position: relative;
  height: 36px;
  margin-bottom: 8px;
  border: 1px solid var(--mud-palette-lines-default);
  border-radius: 6px;
  overflow: hidden;
  cursor: pointer;
  background: color-mix(in srgb, var(--mud-palette-surface) 88%, var(--mud-palette-primary) 12%);
}

.timeline-overview-highlight {
  position: absolute;
  top: 0;
  bottom: 0;
  z-index: 1;
  background: color-mix(in srgb, var(--mud-palette-primary) 26%, transparent);
  border-inline: 1px solid var(--mud-palette-primary);
  pointer-events: none;
}

.timeline-overview-chart {
  position: absolute;
  inset: 3px 0;
  z-index: 2;
  width: 100%;
  height: calc(100% - 6px);
  pointer-events: none;
}

.timeline-overview-count-line {
  fill: none;
  stroke: var(--mud-palette-secondary);
  stroke-width: 1.8;
  vector-effect: non-scaling-stroke;
}

.timeline-overview-playhead {
  position: absolute;
  top: 0;
  bottom: 0;
  z-index: 3;
  width: 2px;
  transform: translateX(-50%);
  background: var(--mud-palette-primary);
  pointer-events: none;
}

.timeline-overview-count {
  position: absolute;
  right: 8px;
  bottom: 5px;
  z-index: 4;
  color: var(--mud-palette-text-secondary);
  font-size: 11px;
  line-height: 1;
  pointer-events: none;
  font-variant-numeric: tabular-nums;
}
</style>

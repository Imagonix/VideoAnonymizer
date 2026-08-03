<script setup lang="ts">
import { computed } from 'vue';
import type { DetectedObjectDto, TimelineObject, TimelineObjectCount } from './types';
import { colorManager } from './services/ColorManager';
import { formatTimelineTime } from './timelineUtils';
import { getLabel } from './utils/utils';
import MudLikeCheckbox from './MudLikeCheckbox.vue';
import TimelineOverview from './TimelineOverview.vue';

const props = defineProps<{
  expanded: boolean;
  currentTime: number;
  duration: number;
  objectCounts: TimelineObjectCount[];
  selectedTimelineObject: TimelineObject | null;
  selectedOccurrence: DetectedObjectDto | null;
  activeGapRange: { startMs: number; endMs: number } | null;
}>();

const emit = defineEmits<{
  (e: 'toggle-expanded'): void;
  (e: 'seek', time: number): void;
  (e: 'toggle-include', checked: boolean): void;
  (e: 'select-occurrence', obj: DetectedObjectDto, time: number): void;
}>();

const expandLabel = computed(() => (props.expanded ? 'Collapse timeline' : 'Expand timeline'));

const sampleObject = computed<DetectedObjectDto | null>(() => {
  if (!props.selectedTimelineObject) return null;
  if (props.selectedTimelineObject.type === 'single') return props.selectedTimelineObject.detectedObj;
  return props.selectedTimelineObject.occurences[0]?.[1] ?? null;
});

const trackLabel = computed(() => {
  const obj = props.selectedOccurrence ?? sampleObject.value;
  return obj ? getLabel(obj) : '';
});

const trackColor = computed(() => {
  const obj = props.selectedOccurrence ?? sampleObject.value;
  return obj ? colorManager.getColor(obj) : 'transparent';
});

const inclusionChecked = computed(() => {
  const row = props.selectedTimelineObject;
  if (!row) return false;
  if (row.type === 'single') return row.detectedObj.selected;
  return row.occurences.every(([, obj]) => obj.selected);
});

const inclusionIndeterminate = computed(() => {
  const row = props.selectedTimelineObject;
  if (!row || row.type === 'single') return false;
  const allSelected = row.occurences.every(([, obj]) => obj.selected);
  const noneSelected = row.occurences.every(([, obj]) => !obj.selected);
  return !allSelected && !noneSelected;
});

const occurrences = computed<[number, DetectedObjectDto][]>(() => {
  const row = props.selectedTimelineObject;
  if (!row) return [];
  if (row.type === 'single') return [[row.timeSeconds, row.detectedObj]];
  return row.occurences;
});

const selectedOccurrenceId = computed(() => props.selectedOccurrence?.id ?? null);

const formattedCurrentTime = computed(() => formatTimelineTime(props.currentTime));
const formattedDuration = computed(() => formatTimelineTime(props.duration));

function toPercent(time: number) {
  if (!props.duration || props.duration <= 0) return '0%';
  return `${(time / props.duration) * 100}%`;
}

function onDotClick(obj: DetectedObjectDto, time: number) {
  emit('select-occurrence', obj, time);
}

const gapDotStyle = computed(() => {
  const range = props.activeGapRange;
  if (!range || range.startMs < 0) return null;
  if (!props.duration || props.duration <= 0) return null;
  const left = (range.startMs / 1000 / props.duration) * 100;
  const color = trackColor.value;
  return {
    left: `${left}%`,
    background: color,
    boxShadow: `0 0 0 4px ${color.replace('hsl(', 'hsla(').replace(')', ', 0.4)')}`,
  };
});
</script>

<template>
  <div class="collapsed-timeline-bar" data-testid="collapsed-timeline-bar">
    <button
      type="button"
      class="timeline-caret"
      data-testid="timeline-caret"
      :aria-expanded="expanded ? 'true' : 'false'"
      :aria-label="expandLabel"
      :title="expandLabel"
      @click="emit('toggle-expanded')"
    >
      <svg class="timeline-caret-icon" :class="{ 'timeline-caret-icon--expanded': expanded }" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
        <path d="M7.41 8.59 12 13.17l4.59-4.58L18 10l-6 6-6-6z" fill="currentColor" />
      </svg>
    </button>

    <template v-if="!expanded">
      <div class="collapsed-time" data-testid="collapsed-timeline-time" aria-live="polite">
        {{ formattedCurrentTime }} / {{ formattedDuration }}
      </div>

      <div v-if="selectedTimelineObject && sampleObject" class="collapsed-track-strip" data-testid="collapsed-track-strip">
        <div
          class="track-thumb-placeholder"
          data-testid="track-thumb-placeholder"
          :style="{ background: trackColor }"
          :aria-label="`Representative image for ${trackLabel}`"
        />
        <MudLikeCheckbox
          :checked="inclusionChecked"
          :indeterminate="inclusionIndeterminate"
          @change="(value: boolean) => emit('toggle-include', value)"
        />
        <span class="track-label">{{ trackLabel }}</span>
        <span class="track-color-dot" :style="{ background: trackColor }" aria-hidden="true" />
        <div class="collapsed-occurrence-track" data-testid="collapsed-occurrence-track">
          <button
            v-for="[time, obj] in occurrences"
            :key="obj.id"
            type="button"
            class="collapsed-dot"
            :class="{ 'collapsed-dot--current': selectedOccurrenceId === obj.id }"
            :style="{
              left: toPercent(time),
              background: colorManager.getColor(obj),
              opacity: obj.selected ? 1 : 0.35,
            }"
            :title="`Seek to ${formatTimelineTime(time)}`"
            :aria-label="`Seek to occurrence at ${formatTimelineTime(time)}`"
            @click="onDotClick(obj, time)"
          />
          <div v-if="gapDotStyle" class="collapsed-dot collapsed-dot--pulsing" :style="gapDotStyle" />
        </div>
      </div>

      <div v-else class="collapsed-overview" data-testid="collapsed-overview">
        <TimelineOverview
          :duration="duration"
          :current-time="currentTime"
          :viewport-start-ratio="0"
          :viewport-end-ratio="1"
          :object-counts="objectCounts"
          @seek="(time: number) => emit('seek', time)"
        />
      </div>
    </template>

    <div v-else class="expanded-header-label" data-testid="expanded-timeline-header">
      Timeline
    </div>
  </div>
</template>

<style scoped>
.collapsed-timeline-bar {
  display: grid;
  grid-template-columns: 40px auto minmax(0, 1fr);
  align-items: center;
  gap: 10px 12px;
  min-height: 48px;
  padding: 6px 12px;
  border-top: 1px solid var(--mud-palette-lines-default);
  background: var(--mud-palette-surface);
}

.expanded-header-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--mud-palette-text-secondary);
}

.timeline-caret {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border: 0;
  border-radius: 50%;
  background: transparent;
  color: var(--mud-palette-primary);
  cursor: pointer;
}

.timeline-caret:hover {
  background: var(--mud-palette-primary-hover);
}

.timeline-caret:focus-visible {
  outline: 2px solid color-mix(in srgb, var(--mud-palette-primary) 70%, transparent);
  outline-offset: 2px;
}

.timeline-caret-icon {
  width: 24px;
  height: 24px;
  transition: transform 0.15s ease;
}

.timeline-caret-icon--expanded {
  transform: rotate(180deg);
}

.collapsed-time {
  font-variant-numeric: tabular-nums;
  font-size: 0.875rem;
  font-weight: 600;
  white-space: nowrap;
  color: var(--mud-palette-text-primary);
}

.collapsed-overview {
  min-width: 0;
}

.collapsed-overview :deep(.timeline-overview) {
  position: relative;
  top: auto;
  margin: 0;
  height: 36px;
}

.collapsed-track-strip {
  display: grid;
  grid-template-columns: 28px auto auto auto minmax(0, 1fr);
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.track-thumb-placeholder {
  width: 28px;
  height: 28px;
  border-radius: 4px;
  opacity: 0.85;
}

.track-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--mud-palette-text-primary);
  white-space: nowrap;
}

.track-color-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  flex-shrink: 0;
}

.collapsed-occurrence-track {
  position: relative;
  height: 28px;
  min-width: 0;
  border: 1px solid var(--mud-palette-lines-default);
  border-radius: 6px;
  background: color-mix(in srgb, var(--mud-palette-background) 70%, var(--mud-palette-surface));
}

.collapsed-dot {
  position: absolute;
  top: 50%;
  transform: translate(-50%, -50%);
  width: 10px;
  height: 10px;
  padding: 0;
  border: 0;
  border-radius: 50%;
  cursor: pointer;
  z-index: 2;
}

.collapsed-dot--current {
  box-shadow: 0 0 0 3px var(--mud-palette-secondary);
  transform: translate(-50%, -50%) scale(1.15);
}

.collapsed-dot--pulsing {
  width: 10px;
  height: 10px;
  z-index: 3;
  pointer-events: none;
  animation: pulse-dot 1s ease-in-out infinite;
}

@keyframes pulse-dot {
  0%, 100% { transform: translate(-50%, -50%) scale(1); opacity: 1; }
  50% { transform: translate(-50%, -50%) scale(1.4); opacity: 0.7; }
}
</style>

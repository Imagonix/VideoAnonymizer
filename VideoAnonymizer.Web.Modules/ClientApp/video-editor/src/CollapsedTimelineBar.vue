<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import type { DetectedObjectDto, TimelineObject, TimelineObjectCount } from './types';
import { colorManager } from './services/ColorManager';
import { buildTimelineTicks, clamp, formatTimelineTime } from './timelineUtils';
import { getLabel } from './utils/utils';
import MudLikeCheckbox from './MudLikeCheckbox.vue';
import TimelineOverview from './TimelineOverview.vue';
import TimelineRuler from './TimelineRuler.vue';
import PlaybackIndicator from './PlaybackIndicator.vue';

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

const seekSurfaceRef = ref<HTMLElement | null>(null);
const seekSurfaceWidth = ref(1);
let resizeObserver: ResizeObserver | null = null;

const pixelsPerSecond = computed(() => {
  if (!props.duration || props.duration <= 0) return 1;
  return Math.max(1, seekSurfaceWidth.value) / props.duration;
});

const ticks = computed(() => buildTimelineTicks(props.duration, pixelsPerSecond.value));

function updateSeekSurfaceWidth() {
  if (!seekSurfaceRef.value) return;
  seekSurfaceWidth.value = Math.max(1, seekSurfaceRef.value.clientWidth);
}

function toPercent(time: number) {
  if (!props.duration || props.duration <= 0) return '0%';
  return `${(time / props.duration) * 100}%`;
}

function timeFromClientX(clientX: number): number | null {
  if (!seekSurfaceRef.value || !props.duration || props.duration <= 0) return null;
  const rect = seekSurfaceRef.value.getBoundingClientRect();
  if (rect.width <= 0) return null;
  const ratio = clamp((clientX - rect.left) / rect.width, 0, 1);
  return ratio * props.duration;
}

function onSeekSurfaceClick(event: MouseEvent) {
  const time = timeFromClientX(event.clientX);
  if (time == null) return;
  emit('seek', time);
}

function onDotClick(event: MouseEvent, obj: DetectedObjectDto, time: number) {
  event.stopPropagation();
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

onMounted(() => {
  updateSeekSurfaceWidth();
  if (seekSurfaceRef.value) {
    resizeObserver = new ResizeObserver(() => updateSeekSurfaceWidth());
    resizeObserver.observe(seekSurfaceRef.value);
  }
});

watch(
  () => props.expanded,
  async () => {
    await nextTick();
    updateSeekSurfaceWidth();
  }
);

onBeforeUnmount(() => {
  resizeObserver?.disconnect();
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
      @click.stop="emit('toggle-expanded')"
    >
      <svg class="timeline-caret-icon" :class="{ 'timeline-caret-icon--expanded': expanded }" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
        <path d="M7.41 8.59 12 13.17l4.59-4.58L18 10l-6 6-6-6z" fill="currentColor" />
      </svg>
    </button>

    <div class="collapsed-main" data-testid="collapsed-timeline-main">
      <template v-if="!expanded">
        <div
          v-if="selectedTimelineObject && sampleObject"
          class="collapsed-track-meta"
          data-testid="collapsed-track-strip"
          @click.stop
        >
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
        </div>

        <div
          ref="seekSurfaceRef"
          class="collapsed-seek-surface"
          data-testid="collapsed-seek-surface"
          @click="onSeekSurfaceClick"
        >
          <PlaybackIndicator :time="currentTime" :duration="duration" />

          <div v-if="selectedTimelineObject && sampleObject" class="collapsed-occurrence-track" data-testid="collapsed-occurrence-track">
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
              @click="onDotClick($event, obj, time)"
            />
            <div v-if="gapDotStyle" class="collapsed-dot collapsed-dot--pulsing" :style="gapDotStyle" />
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

          <TimelineRuler
            compact
            :current-time="currentTime"
            :duration="duration"
            :ticks="ticks"
          />
        </div>
      </template>

      <div v-else class="expanded-header-label" data-testid="expanded-timeline-header">
        Timeline
      </div>
    </div>
  </div>
</template>

<style scoped>
.collapsed-timeline-bar {
  display: grid;
  grid-template-columns: 36px minmax(0, 1fr);
  align-items: start;
  gap: 4px 8px;
  min-height: 0;
  padding: 4px 8px 4px 4px;
  border-top: 1px solid var(--mud-palette-lines-default);
  background: var(--mud-palette-surface);
}

.collapsed-main {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.expanded-header-label {
  font-size: 0.8125rem;
  font-weight: 600;
  line-height: 1.2;
  color: var(--mud-palette-text-secondary);
  padding: 6px 0;
}

.timeline-caret {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  margin-top: 2px;
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
  width: 22px;
  height: 22px;
  transition: transform 0.15s ease;
}

.timeline-caret-icon--expanded {
  transform: rotate(180deg);
}

.collapsed-track-meta {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
  min-height: 28px;
}

.track-thumb-placeholder {
  width: 24px;
  height: 24px;
  border-radius: 4px;
  opacity: 0.85;
  flex-shrink: 0;
}

.track-label {
  font-size: 0.8125rem;
  font-weight: 600;
  color: var(--mud-palette-text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.track-color-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  flex-shrink: 0;
}

.collapsed-seek-surface {
  position: relative;
  min-width: 0;
  cursor: pointer;
  border: 1px solid var(--mud-palette-lines-default);
  border-radius: 6px;
  overflow: hidden;
  background: color-mix(in srgb, var(--mud-palette-background) 70%, var(--mud-palette-surface));
}

.collapsed-overview {
  min-width: 0;
}

.collapsed-overview :deep(.timeline-overview) {
  position: relative;
  top: auto;
  margin: 0;
  height: 28px;
  border: 0;
  border-radius: 0;
  background: transparent;
}

.collapsed-occurrence-track {
  position: relative;
  height: 28px;
  min-width: 0;
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

.collapsed-seek-surface :deep(.timeline-ruler) {
  border-top: 1px solid var(--mud-palette-lines-default);
}

@keyframes pulse-dot {
  0%, 100% { transform: translate(-50%, -50%) scale(1); opacity: 1; }
  50% { transform: translate(-50%, -50%) scale(1.4); opacity: 0.7; }
}
</style>

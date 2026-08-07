<script setup lang="ts">
import { computed } from 'vue';
import type { TimelineObject, EditorMode, DetectedObjectDto } from './types';
import { colorManager } from './services/ColorManager'

const props = defineProps<{
    timelineObject: TimelineObject;
    videoDuration: number;
    mode: EditorMode;
    mergeSelectedKeys: Set<string>;
    selectedOccurrences: Map<string, Set<number>>;
    hoveredTimelineKey: string | null;
    activeGapRange: { startMs: number; endMs: number } | null;
    isTrackSelected?: boolean;
}>();

const emit = defineEmits<{
    (e: 'toggle-occurrence', rowKey: string, time: number, event: MouseEvent): void;
    (e: 'hover-row', key: string | null): void;
    (e: 'merge-toggle', key: string): void;
    (e: 'select-occurrence', obj: DetectedObjectDto, time: number): void;
}>();

function toPercent(time: number) {
  if (!props.videoDuration || props.videoDuration <= 0) return '0%'
  return `${(time / props.videoDuration) * 100}%`
}

function getTimelineKey(obj: TimelineObject): string {
    if (obj.type === 'single') return `obj-${obj.detectedObj.id}`;
    const tid = obj.occurences[0]?.[1].trackId;
    return tid != null ? `track-${tid}` : `obj-${obj.occurences[0]?.[1].id}`;
}

const timelineKey = computed(() => getTimelineKey(props.timelineObject));
const isMergeSelected = computed(() => props.mode === 'merge' && props.mergeSelectedKeys.has(timelineKey.value));
const isMergeHovered = computed(() => props.mode === 'merge' && props.hoveredTimelineKey === timelineKey.value);

const mergeHighlightStyle = computed(() => {
    if (!isMergeSelected.value && !isMergeHovered.value) return {};
    const obj = props.timelineObject.type === 'single'
        ? props.timelineObject.detectedObj
        : props.timelineObject.occurences[0][1];
    const color = colorManager.getColor(obj);
    const alpha = isMergeSelected.value ? '0.3' : '0.15';
    const bg = color.replace('hsl(', 'hsla(').replace(')', `, ${alpha})`);
    return { backgroundColor: bg };
});

const allowsDotSelection = computed(() =>
    props.mode === 'split' && props.timelineObject.type === 'tracked'
);

const selectedTimesForThisRow = computed(() =>
    props.selectedOccurrences.get(timelineKey.value) ?? new Set()
);

const hasAnySelection = computed(() => {
    for (const times of props.selectedOccurrences.values()) {
        if (times.size > 0) return true;
    }
    return false;
});

const otherRowsHaveSelection = computed(() =>
    hasAnySelection.value && !selectedTimesForThisRow.value.size
);

const sampleObj = computed(() => {
    if (props.timelineObject.type === 'single')
        return props.timelineObject.detectedObj;
    return props.timelineObject.occurences[0][1];
});

const trackColor = computed(() => colorManager.getColor(sampleObj.value));

const gapDotStyle = computed(() => {
    const range = props.activeGapRange;
    if (!range || range.startMs < 0) return null;
    const duration = props.videoDuration;
    if (!duration || duration <= 0) return null;
    const left = (range.startMs / 1000 / duration) * 100;
    const color = trackColor.value;
    return {
        left: `${left}%`,
        background: color,
        boxShadow: `0 0 0 4px ${color.replace('hsl(', 'hsla(').replace(')', ', 0.4)')}`
    };
});

function onRowClick() {
    if (props.mode === 'merge') {
        emit('merge-toggle', timelineKey.value);
    } else if (props.mode === 'select') {
        emit('select-occurrence', sampleObj.value, props.timelineObject.type === 'single'
            ? props.timelineObject.timeSeconds
            : props.timelineObject.occurences[0]?.[0] ?? 0);
    }
}

function onDotClick(obj: DetectedObjectDto, time: number, event: MouseEvent) {
    if (allowsDotSelection.value) {
        emit('toggle-occurrence', timelineKey.value, time, event);
        return;
    }
    if (props.mode === 'select' || props.mode === 'merge') {
        emit('select-occurrence', obj, time);
    }
}
</script>
<template>
    <div
      class="timeline-row-wrapper"
      :class="{ 'timeline-row-wrapper--selected': isTrackSelected }"
      :data-timeline-key="timelineKey"
      :data-selected="isTrackSelected ? 'true' : 'false'"
      @click="onRowClick"
      @mouseenter="emit('hover-row', timelineKey)"
      @mouseleave="emit('hover-row', null)"
    >
        <div class="timeline-row" :style="mergeHighlightStyle">
            <template v-if="props.timelineObject.type === 'single'">
                <div
                  class="dot dot--clickable"
                  :style="{
                    left: toPercent(props.timelineObject.timeSeconds),
                    background: colorManager.getColor(props.timelineObject.detectedObj)
                  }"
                  @click.stop="onDotClick(props.timelineObject.detectedObj, props.timelineObject.timeSeconds, $event)"
                />
            </template>
            <template v-else>
                <div
                  v-for="[time, obj] in props.timelineObject.occurences"
                  :key="time"
                  class="dot"
                  :class="{
                      'dot--clickable': mode === 'select' || allowsDotSelection,
                      'dot--selectable': allowsDotSelection,
                      'dot--selected': allowsDotSelection && selectedTimesForThisRow.has(time),
                      'dot--dimmed': allowsDotSelection && otherRowsHaveSelection && !selectedTimesForThisRow.has(time)
                  }"
                  :style="{
                      left: toPercent(time),
                      background: colorManager.getColor(obj),
                      opacity: obj.selected ? (otherRowsHaveSelection && !selectedTimesForThisRow.has(time) ? 0.2 : 1) : 0.3
                  }"
                  @click.stop="onDotClick(obj, time, $event)"
                />
            </template>
            <div v-if="gapDotStyle" class="dot dot--pulsing" :style="gapDotStyle" />
        </div>
    </div>
</template>

<style scoped>
.timeline-row-wrapper {
    position: relative;
    height: var(--timeline-row-height, 28px);
    min-height: var(--timeline-row-height, 28px);
    max-height: var(--timeline-row-height, 28px);
    margin: 0;
    padding: 0;
    box-sizing: border-box;
}

.timeline-row-wrapper--selected {
    outline: 2px solid var(--mud-palette-primary);
    outline-offset: -2px;
    border-radius: 8px;
    background: color-mix(in srgb, var(--mud-palette-primary) 10%, transparent);
}

.timeline-row {
    position: relative;
    height: 100%;
    box-sizing: border-box;
    border-bottom: 1px solid var(--mud-palette-lines-default);
    border-radius: 6px;
    overflow: hidden;
}

.dot {
  position: absolute;
  top: 50%;
  transform: translate(-50%, -50%);
  width: 8px;
  height: 8px;
  border-radius: 50%;
  z-index: 10;
  transition: transform 0.1s, box-shadow 0.1s, opacity 0.15s;
}

.dot--clickable {
  cursor: pointer;
}

.dot--clickable:hover {
  transform: translate(-50%, -50%) scale(1.25);
  z-index: 20;
}

.dot--selectable {
  cursor: pointer;
  width: 12px;
  height: 12px;
}

.dot--selectable:hover {
  transform: translate(-50%, -50%) scale(1.35);
  z-index: 20;
}

.dot--selected {
  box-shadow: 0 0 0 3px var(--mud-palette-secondary);
  z-index: 15;
  transform: translate(-50%, -50%) scale(1.2);
}

.dot--dimmed {
  width: 6px;
  height: 6px;
}

.dot--pulsing {
  width: 10px;
  height: 10px;
  z-index: 25;
  animation: pulse-dot 1s ease-in-out infinite;
  pointer-events: none;
}

@keyframes pulse-dot {
  0%, 100% { transform: translate(-50%, -50%) scale(1); opacity: 1; }
  50% { transform: translate(-50%, -50%) scale(1.4); opacity: 0.7; }
}
</style>

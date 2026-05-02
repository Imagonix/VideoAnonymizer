<script setup lang="ts">
import { computed } from 'vue';
import type { TimelineObject, EditorMode } from './types';
import { colorManager } from './services/ColorManager'

const props = defineProps<{
    timelineObject: TimelineObject;
    videoDuration: number;
    mode: EditorMode;
    mergeSelectedKeys: Set<string>;
    selectedOccurrences: Map<string, Set<number>>;
    hoveredTimelineKey: string | null;
}>();

const emit = defineEmits<{
    (e: 'toggle-occurrence', rowKey: string, time: number, event: MouseEvent): void;
    (e: 'hover-row', key: string | null): void;
    (e: 'merge-toggle', key: string): void;
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

function onRowClick() {
    if (props.mode === 'merge') {
        emit('merge-toggle', timelineKey.value);
    }
}

function onDotClick(time: number, event: MouseEvent) {
    if (allowsDotSelection.value) {
        emit('toggle-occurrence', timelineKey.value, time, event);
    }
}
</script>
<template>
    <div
      class="timeline-row-wrapper"
      @click="onRowClick"
      @mouseenter="emit('hover-row', timelineKey)"
      @mouseleave="emit('hover-row', null)"
    >
        <div class="timeline-row" :style="mergeHighlightStyle">
            <template v-if="props.timelineObject.type === 'single'">
                <div class="dot" :style="{
                    left: toPercent(props.timelineObject.timeSeconds),
                    background: colorManager.getColor(props.timelineObject.detectedObj)
                }" />
            </template>
            <template v-else>
                <div
                  v-for="[time, obj] in props.timelineObject.occurences"
                  :key="time"
                  class="dot"
                  :class="{
                      'dot--selectable': allowsDotSelection,
                      'dot--selected': allowsDotSelection && selectedTimesForThisRow.has(time),
                      'dot--dimmed': allowsDotSelection && otherRowsHaveSelection && !selectedTimesForThisRow.has(time)
                  }"
                  :style="{
                      left: toPercent(time),
                      background: colorManager.getColor(obj),
                      opacity: obj.selected ? (otherRowsHaveSelection && !selectedTimesForThisRow.has(time) ? 0.2 : 1) : 0.3
                  }"
                  @click.stop="onDotClick(time, $event)"
                />
            </template>
        </div>
    </div>
</template>

<style scoped>
.timeline-row-wrapper {
    position: relative;
}

.timeline-row {
    position: relative;
    height: 34px;
    border-bottom: 1px solid var(--mud-palette-lines-default);
    border-radius: 8px;
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
</style>

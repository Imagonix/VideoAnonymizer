<script setup lang="ts">
import { computed } from 'vue';
import type { TimelineObject, EditorMode } from './types';
import { colorManager } from './services/ColorManager'

const props = defineProps<{
    timelineObject: TimelineObject;
    videoDuration: number;
    mode: EditorMode;
    mergeSelectedKeys: Set<string>;
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

const mergeHighlightStyle = computed(() => {
    if (!isMergeSelected.value) return {};
    const obj = props.timelineObject.type === 'single'
        ? props.timelineObject.detectedObj
        : props.timelineObject.occurences[0][1];
    const color = colorManager.getColor(obj);
    const bg = color.replace('hsl(', 'hsla(').replace(')', ', 0.3)');
    return { backgroundColor: bg };
});
</script>
<template>
    <div class="timeline-row-wrapper">
        <div class="timeline-row" :style="mergeHighlightStyle">
            <template v-if="props.timelineObject.type === 'single'">
                <div class="dot" :style="{
                    left: toPercent(props.timelineObject.timeSeconds),
                    background: colorManager.getColor(props.timelineObject.detectedObj)
                }" />
            </template>
            <template v-else>
                <div v-for="[time, obj] in props.timelineObject.occurences" :key="time" class="dot" :style="{
                    left: toPercent(time),
                    background: colorManager.getColor(obj),
                    opacity: obj.selected ? 1 : 0.3
                }" />
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
}
</style>

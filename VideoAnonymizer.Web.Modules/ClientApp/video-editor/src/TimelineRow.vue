<script setup lang="ts">
import type { TimelineObject } from './types';
import { colorManager } from './services/ColorManager'
const props = defineProps<{
    timelineObject: TimelineObject
    videoDuration : number
}>()
function toPercent(time: number) {
  if (!props.videoDuration || props.videoDuration <= 0) return '0%'
  return `${(time / props.videoDuration) * 100}%`
}
</script>
<template>
    <div class="timeline-row-wrapper">
        <div class="timeline-row">

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

<script setup lang="ts">
import { ref, computed } from 'vue';
import type { AnonymizationSettings, PreviewObject, EditorMode, VideoDimensions } from './types';
import { colorManager } from './services/ColorManager';

const props = defineProps<{
  objects: PreviewObject[];
  anonymizationSettings: AnonymizationSettings;
  mode: EditorMode;
  videoDimensions: VideoDimensions | null;
  highlightedRowKey: string | null;
}>();

const emit = defineEmits<{
  (e: 'move-box', id: string, x: number, y: number): void;
}>();

const overlayRef = ref<HTMLDivElement | null>(null);
const dragState = ref<{ id: string; startX: number; startY: number; origX: number; origY: number } | null>(null);

const scaleX = computed(() => {
  if (!props.videoDimensions) return 1;
  return props.videoDimensions.videoWidth / props.videoDimensions.displayWidth;
});

const scaleY = computed(() => {
  if (!props.videoDimensions) return 1;
  return props.videoDimensions.videoHeight / props.videoDimensions.displayHeight;
});

function getBlurEllipseStyle(obj: PreviewObject) {
  const color = colorManager.getColor(obj.detectedObject);
  const scale = props.anonymizationSettings.blurSizePercent / 100;
  const expandedWidth = obj.detectedObject.width * scale;
  const expandedHeight = obj.detectedObject.height * scale;
  const centerX = obj.detectedObject.x + obj.detectedObject.width / 2;
  const centerY = obj.detectedObject.y + obj.detectedObject.height / 2;
  const fillAlpha = obj.activation === 'detected' ? 0.3 : 0.12;
  return {
    left: `${centerX - expandedWidth / 2}px`,
    top: `${centerY - expandedHeight / 2}px`,
    width: `${expandedWidth}px`,
    height: `${expandedHeight}px`,
    borderColor: color,
    backgroundColor: color.replace('hsl(', 'hsla(').replace(')', `, ${fillAlpha})`)
  };
}

function getObjTimelineKey(obj: PreviewObject): string {
  const d = obj.detectedObject;
  return d.trackId != null ? `track-${d.trackId}` : `obj-${d.id}`;
}

function getBoxStyle(obj: PreviewObject) {
  const color = colorManager.getColor(obj.detectedObject);
  return {
    left: `${obj.detectedObject.x}px`,
    top: `${obj.detectedObject.y}px`,
    width: `${obj.detectedObject.width}px`,
    height: `${obj.detectedObject.height}px`,
    borderColor: color,
    opacity: obj.activation === 'detected' ? 1 : 0.4
  };
}

function onBoxMouseDown(event: MouseEvent, obj: PreviewObject) {
  if (props.mode !== 'move' || obj.activation !== 'detected') return;
  const rect = overlayRef.value?.getBoundingClientRect();
  if (!rect) return;
  dragState.value = {
    id: obj.detectedObject.id,
    startX: event.clientX,
    startY: event.clientY,
    origX: obj.detectedObject.x,
    origY: obj.detectedObject.y
  };
}

function onOverlayMouseMove(event: MouseEvent) {
  if (!dragState.value) return;
  const d = dragState.value;
  const dx = (event.clientX - d.startX) * scaleX.value;
  const dy = (event.clientY - d.startY) * scaleY.value;
  emit('move-box', d.id, Math.max(0, Math.round(d.origX + dx)), Math.max(0, Math.round(d.origY + dy)));
}

function onOverlayMouseUp() {
  dragState.value = null;
}

function onOverlayMouseLeave() {
  dragState.value = null;
}
</script>

<template>
  <div
    ref="overlayRef"
    class="overlay"
    :class="{ 'overlay--move': mode === 'move' }"
    @mousemove="onOverlayMouseMove"
    @mouseup="onOverlayMouseUp"
    @mouseleave="onOverlayMouseLeave"
  >
    <template v-for="obj in objects" :key="obj.detectedObject.id">
      <div
        class="obj-group"
        :class="{ 'obj-group--dimmed': highlightedRowKey != null && getObjTimelineKey(obj) !== highlightedRowKey }"
      >
        <div
          data-testid="blur-area-outline"
          class="blur-area-outline"
          :style="getBlurEllipseStyle(obj)"
        />
        <div
          data-testid="bounding-box"
          class="bbox"
          :class="{
            'bbox--draggable': mode === 'move' && obj.activation === 'detected',
            'bbox--dragging': dragState?.id === obj.detectedObject.id
          }"
          :style="getBoxStyle(obj)"
          @mousedown.prevent="onBoxMouseDown($event, obj)"
        />
      </div>
    </template>
  </div>
</template>

<style scoped>
.overlay {
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.overlay--move {
  pointer-events: auto;
  cursor: grab;
}

.overlay--move:active {
  cursor: grabbing;
}

.bbox {
  position: absolute;
  border: 2px dashed;
  box-sizing: border-box;
  transition: box-shadow 0.1s;
}

.bbox--draggable {
  cursor: move;
  pointer-events: auto;
}

.bbox--draggable:hover {
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--mud-palette-primary) 30%, transparent);
}

.bbox--dragging {
  border-style: solid;
  border-color: var(--mud-palette-primary) !important;
  box-shadow: 0 0 0 2px var(--mud-palette-primary);
}

.obj-group {
  transition: opacity 0.15s;
}

.obj-group--dimmed {
  opacity: 0.6;
}

.blur-area-outline {
  position: absolute;
  border: 2px solid;
  border-radius: 50%;
  box-sizing: border-box;
}
</style>

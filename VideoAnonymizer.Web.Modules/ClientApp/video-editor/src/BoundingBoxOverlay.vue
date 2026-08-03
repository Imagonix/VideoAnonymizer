<script setup lang="ts">
import { computed, ref } from 'vue';
import type { AnonymizationSettings, DetectedObjectDto, PreviewObject, VideoDimensions } from './types';
import { colorManager } from './services/ColorManager';
import { getLabel } from './utils/utils';
import { useBoxGeometry } from './composables/useBoxGeometry';

const props = defineProps<{
  objects: PreviewObject[];
  anonymizationSettings: AnonymizationSettings;
  videoDimensions: VideoDimensions | null;
  highlightedRowKey: string | null;
  splitSourceKey: string | null;
  alwaysShowKeys: Set<string>;
  selectedKey: string | null;
  mode: 'select' | 'adjust' | 'add';
  adjustObject: DetectedObjectDto | null;
}>();

const emit = defineEmits<{
  (e: 'select', obj: DetectedObjectDto): void;
  (e: 'draw-complete', box: { x: number; y: number; width: number; height: number }): void;
}>();

const overlayRef = ref<HTMLElement | null>(null);

const videoWidth = computed(() => props.videoDimensions?.videoWidth ?? 640);
const videoHeight = computed(() => props.videoDimensions?.videoHeight ?? 480);
const settings = computed(() => props.anonymizationSettings);
const { minBoxSize, clampPct, clampBox, applyClampedBox, getResizeHandleStyle } = useBoxGeometry(videoWidth, videoHeight, settings);

const handlePositions = ['n', 'ne', 'e', 'se', 's', 'sw', 'w', 'nw'] as const;

function getObjTimelineKey(obj: PreviewObject): string {
  const d = obj.detectedObject;
  return d.trackId != null ? `track-${d.trackId}` : `obj-${d.id}`;
}

function shouldDim(key: string): boolean {
  const hl = props.highlightedRowKey;
  const alwaysShow = props.alwaysShowKeys;
  if (props.selectedKey === key) return false;
  if (hl == null && alwaysShow.size === 0) return false;
  if (alwaysShow.has(key)) return false;
  if (key === hl) return false;
  if (key === props.splitSourceKey) return false;
  return true;
}

function isSelected(obj: PreviewObject): boolean {
  return props.selectedKey === getObjTimelineKey(obj);
}

function isAdjustObject(obj: PreviewObject): boolean {
  return props.adjustObject != null && obj.detectedObject.id === props.adjustObject.id;
}

const visibleObjects = computed(() => {
  if (props.mode === 'adjust' && props.adjustObject) {
    return props.objects.filter(obj => obj.detectedObject.id !== props.adjustObject.id);
  }
  return props.objects;
});

function isExcluded(obj: PreviewObject): boolean {
  return !obj.detectedObject.selected;
}

/** Resolve the effective blur size the same way export does: override ?? global. */
function effectiveBlurSize(obj: DetectedObjectDto): number {
  return obj.blurSizePercentOverride ?? props.anonymizationSettings.blurSizePercent;
}

function getBlurAreaStyle(obj: PreviewObject) {
  // Excluded occurrences are ghost outlines only — no blur fill preview.
  if (isExcluded(obj)) {
    return { display: 'none' };
  }

  const color = colorManager.getColor(obj.detectedObject);
  const scale = effectiveBlurSize(obj.detectedObject) / 100;
  const expandedWidth = obj.detectedObject.width * scale;
  const expandedHeight = obj.detectedObject.height * scale;
  const centerX = obj.detectedObject.x + obj.detectedObject.width / 2;
  const centerY = obj.detectedObject.y + obj.detectedObject.height / 2;
  const fillAlpha = isPrimaryActivation(obj) ? 0.3 : 0.12;
  return {
    ...toOverlayRect(
      centerX - expandedWidth / 2,
      centerY - expandedHeight / 2,
      expandedWidth,
      expandedHeight),
    borderColor: color,
    backgroundColor: color.replace('hsl(', 'hsla(').replace(')', `, ${fillAlpha})`)
  };
}

function usesRectangleBlur(obj: PreviewObject): boolean {
  return obj.detectedObject.blurShape?.toLowerCase() === 'rectangle';
}

function isPrimaryActivation(obj: PreviewObject): boolean {
  return obj.activation === 'detected' || obj.activation === 'interpolated';
}

function getBoxStyle(obj: PreviewObject) {
  const color = colorManager.getColor(obj.detectedObject);
  const excluded = isExcluded(obj);
  return {
    ...toOverlayRect(
      obj.detectedObject.x,
      obj.detectedObject.y,
      obj.detectedObject.width,
      obj.detectedObject.height),
    borderColor: color,
    opacity: excluded ? undefined : (isPrimaryActivation(obj) ? 1 : 0.4)
  };
}

function getAdjustBoxStyle(obj: DetectedObjectDto) {
  const color = colorManager.getColor(obj);
  return {
    ...toOverlayRect(obj.x, obj.y, obj.width, obj.height),
    borderColor: color
  };
}

function getAdjustBlurStyle(obj: DetectedObjectDto) {
  const scale = effectiveBlurSize(obj) / 100;
  const expandedWidth = obj.width * scale;
  const expandedHeight = obj.height * scale;
  const centerX = obj.x + obj.width / 2;
  const centerY = obj.y + obj.height / 2;
  return toOverlayRect(
    centerX - expandedWidth / 2,
    centerY - expandedHeight / 2,
    expandedWidth,
    expandedHeight);
}

function toOverlayRect(x: number, y: number, width: number, height: number) {
  const dimensions = props.videoDimensions;
  if (!dimensions?.videoWidth || !dimensions.videoHeight) {
    return {
      left: `${x}px`,
      top: `${y}px`,
      width: `${width}px`,
      height: `${height}px`
    };
  }

  return {
    left: `${(x / dimensions.videoWidth) * 100}%`,
    top: `${(y / dimensions.videoHeight) * 100}%`,
    width: `${(width / dimensions.videoWidth) * 100}%`,
    height: `${(height / dimensions.videoHeight) * 100}%`
  };
}

function pixelScale() {
  const dimensions = props.videoDimensions;
  const rect = overlayRef.value?.getBoundingClientRect();
  const rectWidth = rect?.width || 1;
  const rectHeight = rect?.height || 1;
  return {
    scaleX: dimensions?.videoWidth ? dimensions.videoWidth / rectWidth : 1,
    scaleY: dimensions?.videoHeight ? dimensions.videoHeight / rectHeight : 1
  };
}

type DragState = {
  clientX: number;
  clientY: number;
  origX: number;
  origY: number;
  origW: number;
  origH: number;
  position: 'move' | string;
};

const dragState = ref<DragState | null>(null);

function onAdjustBoxMouseDown(event: MouseEvent, position: 'move' | string) {
  const obj = props.adjustObject;
  if (!obj) return;
  event.preventDefault();
  event.stopPropagation();
  dragState.value = {
    clientX: event.clientX,
    clientY: event.clientY,
    origX: obj.x,
    origY: obj.y,
    origW: obj.width,
    origH: obj.height,
    position
  };
}

function onOverlayMouseMove(event: MouseEvent) {
  if (props.mode === 'add' && drawState.value) {
    const rect = overlayRef.value?.getBoundingClientRect();
    if (!rect) return;
    const { scaleX, scaleY } = pixelScale();
    const px = clampPct(((event.clientX - rect.left) / Math.max(1, rect.width)) * 100);
    const py = clampPct(((event.clientY - rect.top) / Math.max(1, rect.height)) * 100);
    const d = drawState.value;
    drawState.value = { startX: d.startX, startY: d.startY, curX: px, curY: py };
    return;
  }

  const obj = props.adjustObject;
  const drag = dragState.value;
  if (!obj || !drag || props.mode !== 'adjust') return;

  const rect = overlayRef.value?.getBoundingClientRect();
  if (!rect) return;
  const { scaleX, scaleY } = pixelScale();
  const dx = (event.clientX - drag.clientX) * scaleX;
  const dy = (event.clientY - drag.clientY) * scaleY;

  if (drag.position === 'move') {
    applyClampedBox(obj, drag.origX + dx, drag.origY + dy, obj.width, obj.height);
    return;
  }

  let { origX, origY, origW, origH } = drag;
  const position = drag.position;
  if (position.includes('e')) origW = Math.max(minBoxSize, origW + dx);
  if (position.includes('w')) { origX = origX + dx; origW = Math.max(minBoxSize, origW - dx); }
  if (position.includes('s')) origH = Math.max(minBoxSize, origH + dy);
  if (position.includes('n')) { origY = origY + dy; origH = Math.max(minBoxSize, origH - dy); }
  applyClampedBox(obj, origX, origY, origW, origH);
}

function onOverlayMouseUp() {
  dragState.value = null;

  if (props.mode === 'add' && drawState.value) {
    const d = drawState.value;
    const x = Math.min(d.startX, d.curX);
    const y = Math.min(d.startY, d.curY);
    const w = Math.abs(d.curX - d.startX);
    const h = Math.abs(d.curY - d.startY);
    drawState.value = null;
    if (w > 0.5 && h > 0.5) {
      const box = clampBox(
        (x / 100) * videoWidth.value,
        (y / 100) * videoHeight.value,
        (w / 100) * videoWidth.value,
        (h / 100) * videoHeight.value
      );
      emit('draw-complete', box);
    }
  }
}

type DrawState = { startX: number; startY: number; curX: number; curY: number };

const drawState = ref<DrawState | null>(null);

function onOverlayMouseDown(event: MouseEvent) {
  if (props.mode !== 'add') return;
  const rect = overlayRef.value?.getBoundingClientRect();
  if (!rect) return;
  const px = clampPct(((event.clientX - rect.left) / Math.max(1, rect.width)) * 100);
  const py = clampPct(((event.clientY - rect.top) / Math.max(1, rect.height)) * 100);
  drawState.value = { startX: px, startY: py, curX: px, curY: py };
}

const drawPreviewStyle = computed(() => {
  if (!drawState.value) return null;
  const d = drawState.value;
  return {
    left: `${Math.min(d.startX, d.curX)}%`,
    top: `${Math.min(d.startY, d.curY)}%`,
    width: `${Math.abs(d.curX - d.startX)}%`,
    height: `${Math.abs(d.curY - d.startY)}%`
  };
});

const selectable = computed(() => props.mode === 'select');
</script>

<template>
  <div
    ref="overlayRef"
    class="overlay"
    :class="{
      'overlay--add': mode === 'add',
      'overlay--adjust': mode === 'adjust'
    }"
    @mousedown="onOverlayMouseDown"
    @mousemove="onOverlayMouseMove"
    @mouseup="onOverlayMouseUp"
  >
    <template v-for="obj in visibleObjects" :key="obj.detectedObject.id">
      <div
        class="obj-group"
        :class="{
          'obj-group--dimmed': shouldDim(getObjTimelineKey(obj)),
          'obj-group--inert': mode !== 'select',
          'obj-group--excluded': isExcluded(obj),
          'obj-group--selected': isSelected(obj)
        }"
        :data-excluded="isExcluded(obj) ? 'true' : 'false'"
      >
        <div
          v-if="!isExcluded(obj)"
          data-testid="blur-area-outline"
          class="blur-area-outline"
          :class="{ 'blur-area-outline--rectangle': usesRectangleBlur(obj), 'bbox--selected': isSelected(obj) }"
          :style="getBlurAreaStyle(obj)"
          :tabindex="selectable ? 0 : undefined"
          :role="selectable ? 'button' : undefined"
          :aria-label="selectable ? `Select ${getLabel(obj.detectedObject)} box` : undefined"
          @click.stop="selectable ? emit('select', obj.detectedObject) : undefined"
          @keydown.enter.prevent="selectable ? emit('select', obj.detectedObject) : undefined"
          @keydown.space.prevent="selectable ? emit('select', obj.detectedObject) : undefined"
        />
        <div
          data-testid="bounding-box"
          class="bbox"
          :class="{
            'bbox--selected': isSelected(obj),
            'bbox--excluded': isExcluded(obj)
          }"
          :style="getBoxStyle(obj)"
          :tabindex="selectable ? 0 : undefined"
          :role="selectable ? 'button' : undefined"
          :aria-label="selectable
            ? (isExcluded(obj)
              ? `Select excluded ${getLabel(obj.detectedObject)} box`
              : `Select ${getLabel(obj.detectedObject)} box`)
            : undefined"
          @click.stop="selectable ? emit('select', obj.detectedObject) : undefined"
          @keydown.enter.prevent="selectable ? emit('select', obj.detectedObject) : undefined"
          @keydown.space.prevent="selectable ? emit('select', obj.detectedObject) : undefined"
        />
      </div>
    </template>

    <template v-if="mode === 'adjust' && adjustObject">
      <div class="adjust-group">
        <div class="adjust-blur" :style="getAdjustBlurStyle(adjustObject)" />
        <div
          data-testid="adjust-box"
          class="adjust-box"
          :style="getAdjustBoxStyle(adjustObject)"
          @mousedown="(e) => onAdjustBoxMouseDown(e, 'move')"
        />
        <div
          v-for="position in handlePositions"
          :key="position"
          class="adjust-handle"
          :class="`adjust-handle--${position}`"
          :style="getResizeHandleStyle(adjustObject, position)"
          @mousedown.prevent.stop="(e) => onAdjustBoxMouseDown(e, position)"
        />
      </div>
    </template>

    <div v-if="drawPreviewStyle" class="draw-preview" :style="drawPreviewStyle" />
  </div>
</template>

<style scoped>
.overlay {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
}

.overlay--add {
  pointer-events: auto;
  cursor: crosshair;
}

.obj-group {
  transition: opacity 0.15s;
}

.obj-group--dimmed {
  opacity: 0.4;
}

.obj-group--inert {
  pointer-events: none;
}

/* Excluded = subdued ghost outline without blur fill; still pointer/keyboard selectable. */
.obj-group--excluded {
  opacity: 0.45;
}

.obj-group--excluded:hover,
.obj-group--excluded:focus-within,
.obj-group--excluded.obj-group--selected {
  opacity: 1;
}

.bbox {
  position: absolute;
  border: 2px dashed;
  box-sizing: border-box;
  pointer-events: auto;
  cursor: pointer;
}

.bbox--excluded {
  border-style: dashed;
  border-width: 2px;
  background: transparent;
  box-shadow: none;
}

.bbox--selected {
  border-style: solid;
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--mud-palette-primary) 40%, transparent);
}

.bbox--excluded.bbox--selected {
  border-style: solid;
}

.blur-area-outline:focus-visible,
.bbox:focus-visible {
  outline: 2px solid var(--mud-palette-primary);
  outline-offset: 2px;
}

.blur-area-outline {
  position: absolute;
  border: 2px solid;
  border-radius: 50%;
  box-sizing: border-box;
  pointer-events: auto;
  cursor: pointer;
}

.blur-area-outline--rectangle {
  border-radius: 0;
}

.adjust-group {
  position: absolute;
  inset: 0;
}

.adjust-blur {
  position: absolute;
  border-radius: 50%;
  background: color-mix(in srgb, var(--mud-palette-primary) 25%, transparent);
}

.adjust-box {
  position: absolute;
  border: 2px solid var(--mud-palette-primary);
  box-sizing: border-box;
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--mud-palette-primary) 35%, transparent);
  pointer-events: auto;
  cursor: move;
}

.adjust-handle {
  position: absolute;
  z-index: 5;
  pointer-events: auto;
  background: var(--mud-palette-primary);
  border: 1px solid var(--mud-palette-surface);
  border-radius: 50%;
}

.draw-preview {
  position: absolute;
  border: 2px dashed var(--mud-palette-primary);
  background: color-mix(in srgb, var(--mud-palette-primary) 12%, transparent);
  pointer-events: none;
  z-index: 20;
}
</style>

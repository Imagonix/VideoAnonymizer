<script setup lang="ts">
import type { AnonymizationSettings, PreviewObject, VideoDimensions } from './types';
import { colorManager } from './services/ColorManager';

const props = defineProps<{
  objects: PreviewObject[];
  anonymizationSettings: AnonymizationSettings;
  videoDimensions: VideoDimensions | null;
  highlightedRowKey: string | null;
  splitSourceKey: string | null;
  alwaysShowKeys: Set<string>;
}>();

function getObjTimelineKey(obj: PreviewObject): string {
  const d = obj.detectedObject;
  return d.trackId != null ? `track-${d.trackId}` : `obj-${d.id}`;
}

function shouldDim(key: string): boolean {
  const hl = props.highlightedRowKey;
  const alwaysShow = props.alwaysShowKeys;
  if (hl == null && alwaysShow.size === 0) return false;
  if (alwaysShow.has(key)) return false;
  if (key === hl) return false;
  if (key === props.splitSourceKey) return false;
  return true;
}

function getBlurAreaStyle(obj: PreviewObject) {
  const color = colorManager.getColor(obj.detectedObject);
  const scale = props.anonymizationSettings.blurSizePercent / 100;
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
  return {
    ...toOverlayRect(
      obj.detectedObject.x,
      obj.detectedObject.y,
      obj.detectedObject.width,
      obj.detectedObject.height),
    borderColor: color,
    opacity: isPrimaryActivation(obj) ? 1 : 0.4
  };
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
</script>

<template>
  <div class="overlay">
    <template v-for="obj in objects" :key="obj.detectedObject.id">
      <div
        class="obj-group"
        :class="{ 'obj-group--dimmed': shouldDim(getObjTimelineKey(obj)) }"
      >
        <div
          data-testid="blur-area-outline"
          class="blur-area-outline"
          :class="{ 'blur-area-outline--rectangle': usesRectangleBlur(obj) }"
          :style="getBlurAreaStyle(obj)"
        />
        <div
          data-testid="bounding-box"
          class="bbox"
          :style="getBoxStyle(obj)"
        />
      </div>
    </template>
  </div>
</template>

<style scoped>
.overlay {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
}

.obj-group {
  transition: opacity 0.15s;
}

.obj-group--dimmed {
  opacity: 0.4;
}

.bbox {
  position: absolute;
  border: 2px dashed;
  box-sizing: border-box;
}

.blur-area-outline {
  position: absolute;
  border: 2px solid;
  border-radius: 50%;
  box-sizing: border-box;
}

.blur-area-outline--rectangle {
  border-radius: 0;
}

</style>

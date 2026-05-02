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
          :style="getBlurEllipseStyle(obj)"
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
</style>

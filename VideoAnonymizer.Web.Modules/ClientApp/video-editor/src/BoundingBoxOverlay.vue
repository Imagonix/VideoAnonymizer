<script setup lang="ts">
import type { AnonymizationSettings, DetectedObjectDto, PreviewObject } from './types';
import { colorManager } from './services/ColorManager';

const props = defineProps<{
  objects: PreviewObject[];
  anonymizationSettings: AnonymizationSettings;
}>();

function getBlurEllipseStyle(obj: PreviewObject) {
  const color = colorManager.getColor(obj.detectedObject);

  const scale = props.anonymizationSettings.blurSizePercent / 100;

  const expandedWidth = obj.detectedObject.width * scale;
  const expandedHeight = obj.detectedObject.height * scale;

  const centerX = obj.detectedObject.x + obj.detectedObject.width / 2;
  const centerY = obj.detectedObject.y + obj.detectedObject.height / 2;

  const fillAlpha =
    obj.activation === 'detected' ? 0.3 : 0.12;

  return {
    left: `${centerX - expandedWidth / 2}px`,
    top: `${centerY - expandedHeight / 2}px`,
    width: `${expandedWidth}px`,
    height: `${expandedHeight}px`,
    borderColor: color,
    backgroundColor: color
      .replace('hsl(', 'hsla(')
      .replace(')', `, ${fillAlpha})`)
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
        data-testid="blur-area-outline"
        class="blur-area-outline"
        :style="getBlurEllipseStyle(obj)"
      />

      <div
        data-testid="bounding-box"
        class="bbox"
        :style="getBoxStyle(obj)"
      />
    </template>
  </div>
</template>

<style scoped>
.overlay {
  position: absolute;
  inset: 0;
  pointer-events: none;
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
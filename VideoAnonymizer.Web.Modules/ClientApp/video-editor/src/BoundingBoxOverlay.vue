<script setup lang="ts">
import type { AnonymizationSettings, DetectedObjectDto } from './types';
import { colorManager } from './services/ColorManager';

const props = defineProps<{
  objects: DetectedObjectDto[];
  anonymizationSettings: AnonymizationSettings;
}>();

function getBlurEllipseStyle(obj: DetectedObjectDto) {
  const scale = props.anonymizationSettings.blurSizePercent / 100;

  const expandedWidth = obj.width * scale;
  const expandedHeight = obj.height * scale;

  const centerX = obj.x + obj.width / 2;
  const centerY = obj.y + obj.height / 2;

  let color = colorManager.getColor(obj);

  return {
    left: `${centerX - expandedWidth / 2}px`,
    top: `${centerY - expandedHeight / 2}px`,
    width: `${expandedWidth}px`,
    height: `${expandedHeight}px`,
    borderColor: color,
    backgroundColor: getFillColor(color)
  };
}

function getBoxStyle(obj: DetectedObjectDto) {
  return {
    left: `${obj.x}px`,
    top: `${obj.y}px`,
    width: `${obj.width}px`,
    height: `${obj.height}px`,
    borderColor: colorManager.getColor(obj)
  };
}

function getFillColor(color: string) {
  return color.replace('hsl(', 'hsla(').replace(')', ', 0.3)');
}
</script>

<template>
  <div class="overlay">
    <template v-for="obj in objects" :key="obj.id">
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
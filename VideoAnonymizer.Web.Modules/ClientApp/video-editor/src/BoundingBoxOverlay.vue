<script setup lang="ts">
import type { DetectedObjectDto } from './types';
    import { colorManager } from './services/ColorManager'

defineProps<{
  objects: DetectedObjectDto[];
}>();
</script>

<template>
  <div class="overlay">
    <div
      v-for="obj in objects"
      :key="obj.id"
      data-testid="bounding-box"
      class="bbox"
      :style="{
        left: `${obj.x}px`,
        top: `${obj.y}px`,
        width: `${obj.width}px`,
        height: `${obj.height}px`,
        borderColor: colorManager.getColor(obj)
      }"
    />
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
        border: 2px solid;
        box-sizing: border-box;
    }
</style>
<script setup lang="ts">
import type { DetectedObjectDto } from '../types';

defineProps<{
  objects: DetectedObjectDto[];
  getColor: (key: string) => string;
  getKey: (obj: DetectedObjectDto) => string;
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
        borderColor: getColor(getKey(obj))
      }"
    />
  </div>
</template>
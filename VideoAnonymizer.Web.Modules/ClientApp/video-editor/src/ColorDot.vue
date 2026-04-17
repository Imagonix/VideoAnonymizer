<script setup lang="ts">
import { DetectedObjectDto } from './types';
import { colorManager } from './services/ColorManager'

const props = defineProps<{
  detectedObject: DetectedObjectDto
  alignRight?: boolean
}>()
</script>

<template>
  <div
    class="color-dot"
    :style="{
      background: colorManager.getColor(detectedObject),
      marginLeft: alignRight ? 'auto' : undefined
    }"
  />
</template>

<style scoped>
.color-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
}
</style>
<script setup lang="ts">
import type { DetectedObjectDto } from '../types';

defineProps<{
  objects: DetectedObjectDto[];
  isActive: (key: string) => boolean;
  getKey: (obj: DetectedObjectDto) => string;
}>();

const emit = defineEmits<{
  (e: 'toggle', key: string, checked: boolean): void;
}>();
</script>

<template>
  <div data-testid="object-list">
    <div v-for="obj in objects" :key="obj.id">
      <input
        type="checkbox"
        :checked="isActive(getKey(obj))"
        @change="emit('toggle', getKey(obj), $event.target.checked)"
      />
      {{ obj.className }}
    </div>
  </div>
</template>
<script setup lang="ts">
    import type { DetectedObjectDto } from './types';

    const props = defineProps<{ objects: DetectedObjectDto[] }>();
    const emit = defineEmits<{ (e: 'toggle', key: string, checked: boolean): void }>();

    function buildObjectKey(obj: DetectedObjectDto): string {
        return obj.trackId != null ? `track-${obj.trackId}` : obj.id;
    }
</script>

<template>
    <div data-testid="object-list">
        <div v-for="obj in objects" :key="obj.id" class="object-row">
            <label>
                <input type="checkbox"
                       :checked="obj.selected"
                       @change="emit('toggle', buildObjectKey(obj), ($event.target as HTMLInputElement).checked)" />
                {{ obj.className ?? 'Object' }}
            </label>
        </div>
    </div>
</template>
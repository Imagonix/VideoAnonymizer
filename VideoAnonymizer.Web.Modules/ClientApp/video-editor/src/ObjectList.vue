<script setup lang="ts">
import type { DetectedObjectDto } from './types';
import MudLikeCheckbox from './MudLikeCheckbox.vue';

const props = defineProps<{ objects: DetectedObjectDto[] }>();
const emit = defineEmits<{ (e: 'toggle', key: string, checked: boolean): void }>();

function buildObjectKey(obj: DetectedObjectDto): string {
    return obj.trackId != null ? `track-${obj.trackId}` : obj.id;
}
</script>

<template>
    <div data-testid="object-list">
        <div v-for="obj in objects" :key="obj.id" class="object-row">
            <MudLikeCheckbox :checked="obj.selected"
                @change="(value: boolean) => emit('toggle', obj.id, value)">{{ obj.className ?? 'Object' }}
            </MudLikeCheckbox>
        </div>
    </div>
</template>
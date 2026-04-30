<script setup lang="ts">
import type { DetectedObjectDto } from './types';
import MudLikeCheckbox from './MudLikeCheckbox.vue';
import { getLabel } from './utils/utils'
import ColorDot from './ColorDot.vue';

defineProps<{ objects: DetectedObjectDto[] }>();
const emit = defineEmits<{ (e: 'toggle', key: string, checked: boolean): void }>();
</script>

<template>
    <div data-testid="object-list">
        <div v-for="obj in objects" :key="obj.id" class="object-row">
            <MudLikeCheckbox :checked="obj.selected" @change="(value: boolean) => emit('toggle', obj.id, value)">
                {{ getLabel(obj) }}
            </MudLikeCheckbox>
            <ColorDot :detected-object="obj" />
        </div>
    </div>
</template>
<style scoped>
.object-row {
    display: flex;
    align-items: center;
    gap: 8px;
    height: 34px;
    margin-bottom: 12px;
}
</style>

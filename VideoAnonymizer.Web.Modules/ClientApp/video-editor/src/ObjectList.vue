<script setup lang="ts">
import { ref } from 'vue';
import type { DetectedObjectDto } from './types';
import MudLikeCheckbox from './MudLikeCheckbox.vue';
import { getLabel } from './utils/utils';
import ColorDot from './ColorDot.vue';
import { getObjTimelineKey } from './utils/keys';
import DeleteObjectDialog from './DeleteObjectDialog.vue';

const props = defineProps<{ objects: DetectedObjectDto[] }>();
const emit = defineEmits<{
    (e: 'toggle', key: string, checked: boolean): void;
    (e: 'hover-row', key: string | null): void;
    (e: 'delete-object', obj: DetectedObjectDto): void;
}>();

const confirmDeleteId = ref<string | null>(null);

function onMouseEnter(obj: DetectedObjectDto) {
    emit('hover-row', getObjTimelineKey(obj));
}

function onMouseLeave() {
    emit('hover-row', null);
}

function requestDelete(obj: DetectedObjectDto) {
    confirmDeleteId.value = obj.id;
}

function confirmDelete() {
    if (confirmDeleteId.value) {
        const obj = props.objects.find(o => o.id === confirmDeleteId.value);
        if (obj) emit('delete-object', obj);
    }
    confirmDeleteId.value = null;
}

function cancelDelete() {
    confirmDeleteId.value = null;
}
</script>

<template>
    <div data-testid="object-list" class="object-list" >
        <div v-for="obj in objects" :key="obj.id" class="object-row"
          @mouseenter="onMouseEnter(obj)" @mouseleave="onMouseLeave(obj)">
            <MudLikeCheckbox :checked="obj.selected" @change="(value: boolean) => emit('toggle', obj.id, value)">
                {{ getLabel(obj) }}
            </MudLikeCheckbox>
            <ColorDot :detected-object="obj" />
            <button class="delete-btn" @click.stop="requestDelete(obj)" title="Delete bounding box">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <polyline points="3 6 5 6 21 6" />
                    <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                    <path d="M10 11v6" />
                    <path d="M14 11v6" />
                    <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
                </svg>
            </button>
        </div>
    </div>

    <DeleteObjectDialog
      v-if="confirmDeleteId"
      @cancel="cancelDelete"
      @confirm="confirmDelete"
    />
</template>
<style scoped>
.object-row {
    display: flex;
    align-items: center;
    gap: 4px;
    height: 34px;
    margin-bottom: 12px;
}

.delete-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 22px;
    height: 22px;
    padding: 0;
    border: none;
    border-radius: 4px;
    background: transparent;
    color: var(--mud-palette-text-secondary);
    cursor: pointer;
    opacity: 0;
    transition: opacity 0.15s, background 0.15s, color 0.15s;
    flex-shrink: 0;
    margin-left: auto;
}

.object-row:hover .delete-btn {
    opacity: 1;
}

.delete-btn:hover {
    background: color-mix(in srgb, var(--mud-palette-error, #f44336) 15%, transparent);
    color: var(--mud-palette-error, #f44336);
}

.delete-btn svg {
    width: 16px;
    height: 16px;
}

.object-list {
    width: 120px;
    min-height: 46px;
    flex-shrink: 0;
}
</style>

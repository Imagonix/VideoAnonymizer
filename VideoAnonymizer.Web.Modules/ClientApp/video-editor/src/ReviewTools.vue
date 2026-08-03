<script setup lang="ts">
import type { EditorMode } from './composables/useEditorModes';

defineProps<{
    mode: EditorMode;
    mergeCount: number;
    splitCount: number;
    canSplit: boolean;
}>();

const emit = defineEmits<{
    (e: 'add-object'): void;
    (e: 'adjust-reset'): void;
    (e: 'adjust-done'): void;
    (e: 'cancel'): void;
    (e: 'merge'): void;
    (e: 'split-out'): void;
}>();
</script>

<template>
    <div data-testid="review-tools" class="review-tools">
        <template v-if="mode === 'select'">
            <button class="tool-btn" title="Draw a new bounding box" @click="emit('add-object')">
                Add Object
            </button>
        </template>
        <template v-else-if="mode === 'adjust'">
            <button class="tool-btn" title="Restore the box to its original position" @click="emit('adjust-reset')">
                Reset
            </button>
            <button class="tool-btn tool-btn--primary" title="Finish adjusting the box" @click="emit('adjust-done')">
                Done
            </button>
        </template>
        <template v-else-if="mode === 'add'">
            <button class="tool-btn" title="Cancel adding an object" @click="emit('cancel')">
                Cancel
            </button>
        </template>
        <template v-else-if="mode === 'merge'">
            <button
                class="tool-btn tool-btn--primary"
                :disabled="mergeCount < 2"
                title="Merge the selected tracks"
                @click="emit('merge')"
            >Merge {{ mergeCount }}</button>
            <button class="tool-btn" title="Cancel merging" @click="emit('cancel')">
                Cancel
            </button>
        </template>
        <template v-else-if="mode === 'split'">
            <button
                class="tool-btn tool-btn--primary"
                :disabled="!canSplit || splitCount < 1"
                title="Split the selected occurrences out of their track"
                @click="emit('split-out')"
            >Split out {{ splitCount }}</button>
            <button class="tool-btn" title="Cancel splitting" @click="emit('cancel')">
                Cancel
            </button>
        </template>
    </div>
</template>

<style scoped>
.review-tools {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 10px;
    border: 1px solid var(--mud-palette-lines-default);
    border-radius: 10px;
    background: var(--mud-palette-surface);
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);
}

.tool-btn {
    padding: 6px 14px;
    border: 1px solid var(--mud-palette-lines-inputs);
    border-radius: 6px;
    background: transparent;
    color: var(--mud-palette-text-primary);
    cursor: pointer;
    font-size: 0.85rem;
    font-weight: 600;
    white-space: nowrap;
}

.tool-btn:disabled {
    opacity: 0.4;
    cursor: default;
}

.tool-btn--primary {
    background: var(--mud-palette-primary);
    color: var(--mud-palette-primary-contrast-text);
    border-color: var(--mud-palette-primary);
}

.tool-btn:focus-visible {
    outline: 2px solid color-mix(in srgb, var(--mud-palette-primary) 70%, transparent);
    outline-offset: 2px;
}
</style>

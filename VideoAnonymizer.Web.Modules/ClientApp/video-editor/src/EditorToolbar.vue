<script setup lang="ts">
import type { EditorMode } from './composables/useEditorModes';
import AddIcon from './icons/AddIcon.vue';
import CheckIcon from './icons/CheckIcon.vue';
import CloseIcon from './icons/CloseIcon.vue';

defineProps<{
    mode: EditorMode;
    mergeCount: number;
    splitCount: number;
    canSplit: boolean;
    canConfirm: boolean;
}>();

const emit = defineEmits<{
    (e: 'add-object'): void;
    (e: 'confirm'): void;
    (e: 'discard'): void;
    (e: 'merge'): void;
    (e: 'split-out'): void;
}>();
</script>

<template>
    <div data-testid="editor-toolbar" class="editor-toolbar" @click.stop>
        <button
            v-if="mode === 'select' || mode === 'add'"
            type="button"
            class="tool-icon"
            :class="{ 'tool-icon--active': mode === 'add' }"
            data-testid="toolbar-add-object"
            title="Add Object"
            aria-label="Add Object"
            @click="emit('add-object')"
        >
            <AddIcon />
        </button>

        <button
            v-if="mode === 'adjust' || mode === 'add'"
            type="button"
            class="tool-icon tool-icon--primary"
            data-testid="toolbar-confirm"
            title="Confirm"
            aria-label="Confirm changes"
            :disabled="!canConfirm"
            @click="emit('confirm')"
        >
            <CheckIcon />
        </button>

        <button
            v-if="mode === 'merge'"
            type="button"
            class="tool-text-btn tool-text-btn--primary"
            data-testid="toolbar-merge"
            :disabled="mergeCount < 2"
            title="Merge the selected tracks"
            @click="emit('merge')"
        >Merge {{ mergeCount }}</button>

        <button
            v-if="mode === 'split'"
            type="button"
            class="tool-text-btn tool-text-btn--primary"
            data-testid="toolbar-split-out"
            :disabled="!canSplit || splitCount < 1"
            title="Split the selected occurrences out of their track"
            @click="emit('split-out')"
        >Split out {{ splitCount }}</button>

        <button
            v-if="mode === 'adjust' || mode === 'add' || mode === 'merge' || mode === 'split'"
            type="button"
            class="tool-icon tool-icon--neutral"
            data-testid="toolbar-discard"
            title="Discard"
            aria-label="Discard changes"
            @click="emit('discard')"
        >
            <CloseIcon />
        </button>
    </div>
</template>

<style scoped>
.editor-toolbar {
    position: absolute;
    top: 12px;
    right: 12px;
    z-index: 30;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
    padding: 8px;
    border: 1px solid var(--mud-palette-lines-default);
    border-radius: 10px;
    background: var(--mud-palette-surface);
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);
}

.tool-icon {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 40px;
    height: 40px;
    padding: 0;
    border: 1px solid var(--mud-palette-lines-inputs);
    border-radius: 8px;
    background: transparent;
    color: var(--mud-palette-text-primary);
    cursor: pointer;
}

.tool-icon:hover:not(:disabled) {
    background: color-mix(in srgb, var(--mud-palette-primary) 12%, transparent);
    color: var(--mud-palette-primary);
}

.tool-icon:focus-visible,
.tool-text-btn:focus-visible {
    outline: 2px solid color-mix(in srgb, var(--mud-palette-primary) 70%, transparent);
    outline-offset: 2px;
}

.tool-icon:disabled {
    opacity: 0.4;
    cursor: default;
}

.tool-icon--primary {
    background: var(--mud-palette-primary);
    color: var(--mud-palette-primary-contrast-text);
    border-color: var(--mud-palette-primary);
}

.tool-icon--primary:hover:not(:disabled) {
    background: var(--mud-palette-primary);
    color: var(--mud-palette-primary-contrast-text);
}

.tool-icon--active {
    border-color: var(--mud-palette-primary);
    color: var(--mud-palette-primary);
    background: color-mix(in srgb, var(--mud-palette-primary) 12%, transparent);
}

.tool-icon--neutral:hover:not(:disabled) {
    background: color-mix(in srgb, var(--mud-palette-text-secondary) 12%, transparent);
    color: var(--mud-palette-text-primary);
}

.tool-text-btn {
    padding: 6px 14px;
    border: 1px solid var(--mud-palette-lines-inputs);
    border-radius: 8px;
    background: transparent;
    color: var(--mud-palette-text-primary);
    cursor: pointer;
    font-size: 0.85rem;
    font-weight: 600;
    white-space: nowrap;
    width: 100%;
}

.tool-text-btn:disabled {
    opacity: 0.4;
    cursor: default;
}

.tool-text-btn--primary {
    background: var(--mud-palette-primary);
    color: var(--mud-palette-primary-contrast-text);
    border-color: var(--mud-palette-primary);
}
</style>

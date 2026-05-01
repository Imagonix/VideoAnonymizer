<script setup lang="ts">
defineProps<{
    moveMode: boolean;
    resizeMode: boolean;
    mergeMode: boolean;
    mergeCount: number;
    splitMode: boolean;
    canSplit: boolean;
    splitCount: number;
}>();
const emit = defineEmits<{
    (e: 'toggle-move-mode'): void;
    (e: 'toggle-resize-mode'): void;
    (e: 'toggle-merge-mode'): void;
    (e: 'merge'): void;
    (e: 'toggle-split-mode'): void;
    (e: 'split-out'): void;
}>();
</script>

<template>
    <div class="right-divider"></div>
    <div class="editor-controls">
        <div class="control-row">
            <button
              class="control-btn"
              :class="{ active: moveMode }"
              @click="emit('toggle-move-mode')"
              title="Drag bounding boxes to reposition them"
            >
                <svg class="btn-icon" viewBox="0 0 24 24" width="14" height="14">
                    <path d="M12 2l-4 4h3v3h-3v-3l-4 4 4 4v-3h3v3h-3l4 4 4-4h-3v-3h3v3l4-4-4-4v3h-3v-3h3l-4-4z" fill="currentColor"/>
                </svg>
                <span>{{ moveMode ? 'Exit Move' : 'Move' }}</span>
            </button>
        </div>
        <div class="control-row">
            <button
              class="control-btn"
              :class="{ active: resizeMode }"
              @click="emit('toggle-resize-mode')"
              title="Drag resize handles to change bounding box size"
            >
                <svg class="btn-icon" viewBox="0 0 24 24" width="14" height="14">
                    <path d="M22 2h-6v2h2.59L12 10.59 5.41 4H8V2H2v6h2V5.41L10.59 12 4 18.59V16H2v6h6v-2H5.41L12 13.41 18.59 20H16v2h6v-6h-2v2.59L13.41 12 20 5.41V8h2V2z" fill="currentColor"/>
                </svg>
                <span>{{ resizeMode ? 'Exit Resize' : 'Resize' }}</span>
            </button>
        </div>
        <div class="control-row">
            <button
              class="control-btn"
              :class="{ active: mergeMode }"
              @click="emit('toggle-merge-mode')"
              title="Select timeline rows to merge"
            >
                <svg class="btn-icon" viewBox="0 0 24 24" width="14" height="14">
                    <circle cx="18" cy="12" r="2.5" fill="currentColor"/>
                    <circle cx="8" cy="6" r="2.5" fill="currentColor"/>
                    <circle cx="8" cy="18" r="2.5" fill="currentColor"/>
                    <path d="M10.5 12h5.5" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
                    <path d="M8 8.5v-2.5" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
                    <path d="M8 15.5v2.5" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
                </svg>
                <span>{{ mergeMode ? 'Exit Merge' : 'Merge' }}</span>
            </button>
            <button
              v-if="mergeMode && mergeCount >= 2"
              class="action-btn"
              @click="emit('merge')"
            >
                Merge {{ mergeCount }}
            </button>
        </div>
        <div class="control-row">
            <button
              class="control-btn"
              :class="{ active: splitMode }"
              @click="emit('toggle-split-mode')"
              title="Click dots to split out of their tracked row"
            >
                <svg class="btn-icon" viewBox="0 0 24 24" width="14" height="14">
                    <path d="M10 4h4v4h-4V4zM4 16h4v4H4v-4zm0-8h4v4H4V8zm16 8h4v4h-4v-4zm-8 0h4v4h-4v-4z" fill="currentColor"/>
                </svg>
                <span>{{ splitMode ? 'Exit Split' : 'Split' }}</span>
            </button>
            <button
              v-if="splitMode && canSplit && splitCount >= 1"
              class="action-btn"
              @click="emit('split-out')"
            >
                Split out {{ splitCount }}
            </button>
        </div>
    </div>
</template>

<style scoped>
.right-divider {
    width: 1px;
    align-self: stretch;
    background: var(--mud-palette-lines-default);
    margin: 0 12px;
}

.editor-controls {
    display: flex;
    flex-direction: column;
    gap: 8px;
    min-width: 140px;
}

.control-row {
    display: flex;
    align-items: center;
    gap: 8px;
}

.control-btn {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 5px 12px;
    border: 1px solid var(--mud-palette-lines-default);
    border-radius: 8px;
    background: transparent;
    color: var(--mud-palette-text-secondary);
    cursor: pointer;
    font-size: 0.82rem;
    font-weight: 500;
    white-space: nowrap;
    transition: background 0.15s, color 0.15s;
}

.control-btn:hover {
    background: color-mix(in srgb, var(--mud-palette-primary) 8%, transparent);
    color: var(--mud-palette-text-primary);
}

.control-btn.active {
    background: var(--mud-palette-primary);
    color: var(--mud-palette-primary-contrast-text);
    border-color: var(--mud-palette-primary);
}

.btn-icon {
    display: block;
}

.action-btn {
    padding: 5px 12px;
    border: none;
    border-radius: 6px;
    background: var(--mud-palette-secondary);
    color: var(--mud-palette-secondary-contrast-text);
    cursor: pointer;
    font-size: 0.82rem;
    font-weight: 500;
    white-space: nowrap;
    transition: opacity 0.15s;
}

.action-btn:hover {
    opacity: 0.85;
}
</style>

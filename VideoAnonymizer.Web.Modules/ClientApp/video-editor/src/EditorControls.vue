<script setup lang="ts">
defineProps<{
    moveMode: boolean;
    resizeMode: boolean;
    addMode: boolean;
    mergeMode: boolean;
    mergeCount: number;
    splitMode: boolean;
    canSplit: boolean;
    splitCount: number;
}>();
const emit = defineEmits<{
    (e: 'toggle-move-mode'): void;
    (e: 'toggle-resize-mode'): void;
    (e: 'toggle-add-mode'): void;
    (e: 'toggle-merge-mode'): void;
    (e: 'merge'): void;
    (e: 'toggle-split-mode'): void;
    (e: 'split-out'): void;
}>();
</script>

<template>
    <div class="right-divider"></div>
    <div class="editor-controls">
        <div class="section-label">Frame actions</div>
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
              :class="{ active: addMode }"
              @click="emit('toggle-add-mode')"
              title="Draw a new bounding box"
            >
                <svg class="btn-icon" viewBox="0 0 24 24" width="14" height="14">
                    <path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z" fill="currentColor"/>
                </svg>
                <span>{{ addMode ? 'Exit Add' : 'Add' }}</span>
            </button>
        </div>

        <div class="section-divider"></div>
        <div class="section-label">Tracking actions</div>
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
    min-width: 152px;
    font-family: var(--mud-typography-default-family);
}

.section-label {
    font-size: 0.875rem;
    font-weight: 500;
    text-transform: none;
    letter-spacing: 0;
    color: var(--mud-palette-text-secondary);
    line-height: 1.43;
    padding: 4px 0 0;
}

.section-divider {
    height: 1px;
    background: var(--mud-palette-lines-default);
    margin: 6px 0 2px;
}

.control-row {
    display: flex;
    align-items: center;
    gap: 8px;
}

.control-btn {
    display: inline-flex;
    align-items: center;
    justify-content: flex-start;
    gap: 6px;
    min-width: 84px;
    height: 36px;
    padding: 0 14px;
    border: 1px solid var(--mud-palette-lines-inputs);
    border-radius: var(--mud-default-borderradius);
    background: color-mix(in srgb, var(--mud-palette-surface) 88%, var(--mud-palette-primary) 12%);
    color: var(--mud-palette-text-primary);
    cursor: pointer;
    font: inherit;
    font-size: 0.875rem;
    font-weight: 600;
    line-height: 1;
    white-space: nowrap;
    box-shadow: none;
    transition: background-color 0.15s ease, border-color 0.15s ease, box-shadow 0.15s ease, color 0.15s ease;
}

.control-btn:hover {
    border-color: var(--mud-palette-primary);
    background: color-mix(in srgb, var(--mud-palette-primary) 14%, var(--mud-palette-surface));
    color: var(--mud-palette-text-primary);
}

.control-btn:focus-visible,
.action-btn:focus-visible {
    outline: 2px solid color-mix(in srgb, var(--mud-palette-primary) 70%, transparent);
    outline-offset: 2px;
}

.control-btn.active {
    background: var(--mud-palette-primary);
    color: var(--mud-palette-primary-contrast-text);
    border-color: var(--mud-palette-primary);
    box-shadow: 0 0 0 3px color-mix(in srgb, var(--mud-palette-primary) 20%, transparent);
}

.btn-icon {
    display: block;
    width: 18px;
    height: 18px;
    flex: 0 0 18px;
}

.action-btn {
    height: 36px;
    padding: 0 14px;
    border: none;
    border-radius: var(--mud-default-borderradius);
    background: var(--mud-palette-secondary);
    color: var(--mud-palette-secondary-contrast-text);
    cursor: pointer;
    font: inherit;
    font-size: 0.875rem;
    font-weight: 600;
    line-height: 1;
    white-space: nowrap;
    box-shadow: 0 3px 10px color-mix(in srgb, var(--mud-palette-secondary) 20%, transparent);
    transition: background-color 0.15s ease, box-shadow 0.15s ease;
}

.action-btn:hover {
    background: color-mix(in srgb, var(--mud-palette-secondary) 88%, white);
    box-shadow: 0 5px 14px color-mix(in srgb, var(--mud-palette-secondary) 26%, transparent);
}
</style>

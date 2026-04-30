<script setup lang="ts">
defineProps<{
  mergeMode: boolean;
  mergeCount: number;
  splitMode: boolean;
  selectedOccurrenceCount: number;
  canSplit: boolean;
}>();
const emit = defineEmits<{
  (e: 'toggle-merge-mode'): void;
  (e: 'merge'): void;
  (e: 'toggle-split-mode'): void;
  (e: 'split-out'): void;
}>();
</script>

<template>
  <div class="editor-toolbar">
    <button
      class="toolbar-btn"
      :class="{ active: mergeMode }"
      @click="emit('toggle-merge-mode')"
      title="Select multiple timeline rows to merge into one tracked group"
    >
      <svg class="btn-icon" viewBox="0 0 24 24" width="16" height="16">
        <path d="M17 7h2v2h-2V7zm0 4h2v2h-2v2h-2v2h-2v-2h-2v-2H7v-2H5v-2h2V7h2V5h2v2h2v2h2v2zm0 6h2v2h-2v-2zM3 3h2v2H3V3zm0 16h2v2H3v-2z" fill="currentColor"/>
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

    <button
      class="toolbar-btn"
      :class="{ active: splitMode }"
      @click="emit('toggle-split-mode')"
      title="Click individual dots to split them out of their tracked row"
    >
      <svg class="btn-icon" viewBox="0 0 24 24" width="16" height="16">
        <path d="M10 4h4v4h-4V4zM4 16h4v4H4v-4zm0-8h4v4H4V8zm16 8h4v4h-4v-4zm-8 0h4v4h-4v-4z" fill="currentColor"/>
      </svg>
      <span>{{ splitMode ? 'Exit Split' : 'Split' }}</span>
    </button>
    <button
      v-if="splitMode && canSplit && selectedOccurrenceCount >= 1"
      class="action-btn"
      @click="emit('split-out')"
    >
      Split out {{ selectedOccurrenceCount }}
    </button>
  </div>
</template>

<style scoped>
.editor-toolbar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  background: var(--mud-palette-surface);
  border-bottom: 1px solid var(--mud-palette-lines-default);
}

.toolbar-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 14px;
  border: 1px solid var(--mud-palette-lines-default);
  border-radius: 8px;
  background: transparent;
  color: var(--mud-palette-text-secondary);
  cursor: pointer;
  font-size: 0.85rem;
  font-weight: 500;
  transition: background 0.15s, color 0.15s;
}

.toolbar-btn:hover {
  background: color-mix(in srgb, var(--mud-palette-primary) 8%, transparent);
  color: var(--mud-palette-text-primary);
}

.toolbar-btn.active {
  background: var(--mud-palette-primary);
  color: var(--mud-palette-primary-contrast-text);
  border-color: var(--mud-palette-primary);
}

.btn-icon {
  display: block;
}

.action-btn {
  padding: 6px 16px;
  border: none;
  border-radius: 6px;
  background: var(--mud-palette-secondary);
  color: var(--mud-palette-secondary-contrast-text);
  cursor: pointer;
  font-size: 0.85rem;
  font-weight: 500;
  transition: opacity 0.15s;
}

.action-btn:hover {
  opacity: 0.85;
}
</style>

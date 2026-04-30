<script setup lang="ts">
defineProps<{ mergeMode: boolean; mergeCount: number }>();
const emit = defineEmits<{
  (e: 'toggle-merge-mode'): void;
  (e: 'merge'): void;
}>();
</script>

<template>
  <div class="editor-toolbar">
    <button
      class="merge-mode-btn"
      :class="{ active: mergeMode }"
      @click="emit('toggle-merge-mode')"
      title="Select timeline rows to merge into a single tracked group"
    >
      <svg class="merge-icon" viewBox="0 0 24 24" width="16" height="16">
        <path d="M17 7h2v2h-2V7zm0 4h2v2h-2v2h-2v2h-2v-2h-2v-2H7v-2H5v-2h2V7h2V5h2v2h2v2h2v2zm0 6h2v2h-2v-2zM3 3h2v2H3V3zm0 16h2v2H3v-2z" fill="currentColor"/>
      </svg>
      <span>{{ mergeMode ? 'Exit Merge' : 'Merge Objects' }}</span>
    </button>
    <button
      v-if="mergeMode && mergeCount >= 2"
      class="merge-action-btn"
      @click="emit('merge')"
    >
      Merge {{ mergeCount }} objects
    </button>
  </div>
</template>

<style scoped>
.editor-toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 12px;
  background: var(--mud-palette-surface);
  border-bottom: 1px solid var(--mud-palette-lines-default);
}

.merge-mode-btn {
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

.merge-mode-btn:hover {
  background: color-mix(in srgb, var(--mud-palette-primary) 8%, transparent);
  color: var(--mud-palette-text-primary);
}

.merge-mode-btn.active {
  background: var(--mud-palette-primary);
  color: var(--mud-palette-primary-contrast-text);
  border-color: var(--mud-palette-primary);
}

.merge-icon {
  display: block;
}

.merge-action-btn {
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

.merge-action-btn:hover {
  opacity: 0.85;
}
</style>

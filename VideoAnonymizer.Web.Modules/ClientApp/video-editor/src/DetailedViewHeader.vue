<script setup lang="ts">
defineProps<{
    mode: 'move' | 'resize' | 'add' | 'track';
    hasActiveTracking: boolean;
}>();

const emit = defineEmits<{
    (e: 'done'): void;
    (e: 'mode-change', mode: 'move' | 'resize' | 'add' | 'track'): void;
}>();
</script>

<template>
    <div class="move-header">
        <div class="move-mode-switch">
            <button
              class="mode-switch-btn"
              :class="{ active: mode === 'move' }"
              @click="emit('mode-change', 'move')"
            >Move</button>
            <button
              class="mode-switch-btn"
              :class="{ active: mode === 'resize' }"
              @click="emit('mode-change', 'resize')"
            >Resize</button>
            <button
              class="mode-switch-btn"
              :class="{ active: mode === 'add' }"
              @click="emit('mode-change', 'add')"
            >Add</button>
            <button
              class="mode-switch-btn"
              :class="{ active: mode === 'track', 'mode-switch-btn--track-disabled': hasActiveTracking }"
              @click="emit('mode-change', 'track')"
            >Track</button>
        </div>
        <div class="move-actions">
            <button class="close-btn" @click="emit('done')" title="Close">✕</button>
        </div>
    </div>
</template>

<style scoped>
.move-header {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 12px 16px;
    border-bottom: 1px solid var(--mud-palette-lines-default);
    flex-shrink: 0;
}

.move-title {
    font-size: 1rem;
    font-weight: 600;
    color: var(--mud-palette-text-primary);
}

.move-mode-switch {
    display: inline-flex;
    border-radius: 8px;
    overflow: hidden;
    border: 1px solid var(--mud-palette-lines-default);
}

.mode-switch-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 6px;
    padding: 4px 14px;
    border: none;
    background: transparent;
    color: var(--mud-palette-text-secondary);
    cursor: pointer;
    font-size: 0.82rem;
    font-weight: 500;
    transition: background 0.15s, color 0.15s;
}

.mode-switch-btn--track-disabled {
    opacity: 0.72;
    cursor: default;
}

.mode-switch-btn--track-disabled:hover {
    background: transparent;
    color: var(--mud-palette-text-secondary);
}

.mode-switch-btn:not(:last-child) {
    border-right: 1px solid var(--mud-palette-lines-default);
}

.mode-switch-btn.active {
    background: var(--mud-palette-primary);
    color: var(--mud-palette-primary-contrast-text);
}

.mode-switch-btn:hover:not(.active) {
    background: color-mix(in srgb, var(--mud-palette-primary) 8%, transparent);
    color: var(--mud-palette-text-primary);
}

.move-actions {
    margin-left: auto;
}

.close-btn {
    width: 28px;
    height: 28px;
    border: none;
    border-radius: 4px;
    background: transparent;
    color: var(--mud-palette-text-secondary);
    cursor: pointer;
    font-size: 1rem;
    display: flex;
    align-items: center;
    justify-content: center;
}

.close-btn:hover {
    background: color-mix(in srgb, var(--mud-palette-action-default) 15%, transparent);
}
</style>

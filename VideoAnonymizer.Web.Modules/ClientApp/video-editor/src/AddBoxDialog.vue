<script setup lang="ts">
import { ref } from 'vue';

defineProps<{
    existingTrackIds: number[];
    trackIdsInCurrentFrame: Set<number>;
}>();

const emit = defineEmits<{
    (e: 'cancel'): void;
    (e: 'confirm', className: string, trackId: 'new' | number): void;
}>();

const labelClass = ref('face');
const labelTrackId = ref<'new' | number>('new');

function confirm() {
    emit('confirm', labelClass.value, labelTrackId.value);
    labelTrackId.value = 'new';
}
</script>

<template>
    <div class="label-overlay" @mousedown.self="emit('cancel')">
        <div class="label-popup">
            <select v-model="labelClass" class="label-input-field">
                <option value="face">Face</option>
                <option value="other">Other</option>
            </select>
            <div class="label-field-row">
                <span class="label-field-label">Track ID</span>
                <select v-model="labelTrackId" class="label-input-field">
                    <option :value="'new'">New</option>
                    <option v-for="id in existingTrackIds" :key="id" :value="id"
                      :disabled="trackIdsInCurrentFrame.has(id)"
                    >{{ id }}{{ trackIdsInCurrentFrame.has(id) ? ' (already in this frame)' : '' }}</option>
                </select>
            </div>
            <div class="label-popup-actions">
                <button class="popup-btn" @click="emit('cancel')">Cancel</button>
                <button class="popup-btn primary" @click="confirm">Add</button>
            </div>
        </div>
    </div>
</template>

<style scoped>
.label-overlay {
    position: fixed;
    inset: 0;
    z-index: 1100;
    display: flex;
    align-items: center;
    justify-content: center;
}

.label-popup {
    background: var(--mud-palette-surface);
    border: 1px solid var(--mud-palette-lines-default);
    border-radius: 8px;
    padding: 16px;
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);
    display: flex;
    flex-direction: column;
    gap: 8px;
    min-width: 220px;
}

.label-input-field {
    background: var(--mud-palette-background);
    border: 1px solid var(--mud-palette-lines-inputs);
    border-radius: 4px;
    padding: 8px 12px;
    color: var(--mud-palette-text-primary);
    font-size: 0.9rem;
    outline: none;
}

.label-input-field:focus {
    border-color: var(--mud-palette-primary);
}

.label-field-row {
    display: flex;
    align-items: center;
    gap: 8px;
}

.label-field-label {
    font-size: 0.85rem;
    color: var(--mud-palette-text-secondary);
    white-space: nowrap;
}

.label-field-row .label-input-field {
    flex: 1;
}

.label-popup-actions {
    display: flex;
    gap: 8px;
    justify-content: flex-end;
}

.popup-btn {
    padding: 6px 16px;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.85rem;
    font-weight: 500;
    background: transparent;
    color: var(--mud-palette-text-primary);
}

.popup-btn.primary {
    background: var(--mud-palette-primary);
    color: var(--mud-palette-primary-contrast-text);
}
</style>

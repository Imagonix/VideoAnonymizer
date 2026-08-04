<script setup lang="ts">
defineProps<{
    existingTrackIds: number[];
    trackIdsInCurrentFrame: Set<number>;
    className?: string;
    trackId?: 'new' | number;
}>();

const emit = defineEmits<{
    (e: 'class-changed', value: string): void;
    (e: 'track-changed', value: 'new' | number): void;
}>();

function parseTrackId(target: HTMLSelectElement): 'new' | number {
    const value = target.value;
    return value === 'new' ? 'new' : Number(value);
}
</script>

<template>
    <div class="add-dialog" data-testid="add-box-dialog">
        <div class="label-popup">
            <span class="label-field-label">Class</span>
            <select
                class="label-input-field"
                data-testid="add-class-select"
                :value="className ?? 'other'"
                @change="emit('class-changed', ($event.target as HTMLSelectElement).value)"
            >
                <option value="face">Face</option>
                <option value="other">Other</option>
            </select>
            <div class="label-field-row">
                <span class="label-field-label">Track ID</span>
                <select
                    class="label-input-field"
                    data-testid="add-track-select"
                    :value="trackId ?? 'new'"
                    @change="emit('track-changed', parseTrackId($event.target as HTMLSelectElement))"
                >
                    <option value="new">New</option>
                    <option v-for="id in existingTrackIds" :key="id" :value="id"
                      :disabled="trackIdsInCurrentFrame.has(id)"
                    >{{ id }}{{ trackIdsInCurrentFrame.has(id) ? ' (already in this frame)' : '' }}</option>
                </select>
            </div>
        </div>
    </div>
</template>

<style scoped>
.add-dialog {
    position: absolute;
    top: 12px;
    right: 68px;
    z-index: 31;
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
    min-width: 180px;
}

.label-input-field {
    background: var(--mud-palette-background);
    border: 1px solid var(--mud-palette-lines-inputs);
    border-radius: 4px;
    padding: 8px 12px;
    color: var(--mud-palette-text-primary);
    font-size: 0.9rem;
    outline: none;
    width: 100%;
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
</style>

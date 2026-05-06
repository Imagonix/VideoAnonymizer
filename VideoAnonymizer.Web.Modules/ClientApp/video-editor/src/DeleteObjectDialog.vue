<script setup lang="ts">
const emit = defineEmits<{
    (e: 'cancel'): void;
    (e: 'confirm'): void;
}>();
</script>

<template>
    <Teleport to="body">
        <div class="delete-overlay" @click.self="emit('cancel')">
            <div class="delete-dialog" role="dialog" aria-label="Delete confirmation">
                <div class="delete-dialog-icon">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M3 6h18" />
                        <path d="M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2" />
                        <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                    </svg>
                </div>
                <div class="delete-dialog-text">Delete this bounding box?</div>
                <div class="delete-dialog-actions">
                    <button class="dialog-btn dialog-btn--cancel" @click="emit('cancel')">Cancel</button>
                    <button class="dialog-btn dialog-btn--confirm" @click="emit('confirm')">Delete</button>
                </div>
            </div>
        </div>
    </Teleport>
</template>

<style scoped>
.delete-overlay {
    position: fixed;
    inset: 0;
    z-index: 2000;
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(0, 0, 0, 0.4);
}

.delete-dialog {
    background: var(--mud-palette-surface);
    border: 1px solid var(--mud-palette-lines-default);
    border-radius: 12px;
    padding: 24px;
    box-shadow: 0 16px 48px rgba(0, 0, 0, 0.4);
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 16px;
    min-width: 280px;
}

.delete-dialog-icon svg {
    width: 40px;
    height: 40px;
    color: var(--mud-palette-error, #f44336);
}

.delete-dialog-text {
    font-size: 1rem;
    font-weight: 500;
    color: var(--mud-palette-text-primary);
    text-align: center;
}

.delete-dialog-actions {
    display: flex;
    gap: 12px;
    justify-content: center;
    width: 100%;
}

.dialog-btn {
    padding: 8px 20px;
    border: none;
    border-radius: 8px;
    cursor: pointer;
    font-size: 0.875rem;
    font-weight: 500;
    transition: background 0.15s;
}

.dialog-btn--cancel {
    background: transparent;
    color: var(--mud-palette-text-primary);
}

.dialog-btn--cancel:hover {
    background: color-mix(in srgb, var(--mud-palette-action-default) 10%, transparent);
}

.dialog-btn--confirm {
    background: var(--mud-palette-error, #f44336);
    color: #fff;
}

.dialog-btn--confirm:hover {
    opacity: 0.9;
}
</style>

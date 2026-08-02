<script setup lang="ts">
import MudLikeCheckbox from './MudLikeCheckbox.vue';

defineProps<{
    trackId: number | null;
    label: string;
    included: boolean;
    shape: string | null;
    globalBlurSizePercent: number;
    blurSizePercentOverride: number | null;
    pre: number;
    preIsCustom: boolean;
    post: number;
    postIsCustom: boolean;
}>();

const emit = defineEmits<{
    (e: 'toggle-include', checked: boolean): void;
    (e: 'update-shape', shape: string): void;
    (e: 'update-blur-size', percent: number): void;
    (e: 'reset-blur-size'): void;
    (e: 'update-pre', valueMs: number): void;
    (e: 'update-post', valueMs: number): void;
    (e: 'reset-pre'): void;
    (e: 'reset-post'): void;
}>();

function emitBlurSize(event: Event) {
    const value = Number((event.target as HTMLInputElement).value);
    if (!isNaN(value)) emit('update-blur-size', value);
}

function emitPre(event: Event) {
    const value = Number((event.target as HTMLInputElement).value);
    if (!isNaN(value)) emit('update-pre', value);
}

function emitPost(event: Event) {
    const value = Number((event.target as HTMLInputElement).value);
    if (!isNaN(value)) emit('update-post', value);
}
</script>

<template>
    <div data-testid="track-settings-panel" class="track-settings">
        <div class="track-settings-header">
            <MudLikeCheckbox :checked="included" @change="(value: boolean) => emit('toggle-include', value)">
                <span class="track-settings-title">{{ label }}</span>
            </MudLikeCheckbox>
        </div>

        <div class="track-settings-row">
            <span class="track-settings-field-label">Shape</span>
            <select class="track-settings-input" :value="shape ?? 'ellipse'" @change="(e) => emit('update-shape', (e.target as HTMLSelectElement).value)">
                <option value="ellipse">Ellipse</option>
                <option value="rectangle">Rectangle</option>
            </select>
        </div>

        <div class="track-settings-row">
            <span class="track-settings-field-label">Blur size</span>
            <input
                class="track-settings-input"
                type="number"
                min="100"
                max="300"
                :value="blurSizePercentOverride ?? globalBlurSizePercent"
                @change="emitBlurSize"
            />
            <span class="track-settings-badge" :class="blurSizePercentOverride == null ? 'badge-global' : 'badge-custom'">
                {{ blurSizePercentOverride == null ? 'Global' : 'Custom' }}
            </span>
            <button class="track-settings-reset" title="Reset blur size to global" @click="emit('reset-blur-size')">Reset</button>
        </div>

        <div class="track-settings-row">
            <span class="track-settings-field-label">Before</span>
            <input class="track-settings-input" type="number" min="0" :value="pre" @change="emitPre" />
            <span class="track-settings-badge" :class="preIsCustom ? 'badge-custom' : 'badge-global'">
                {{ preIsCustom ? 'Custom' : 'Global' }}
            </span>
            <button class="track-settings-reset" title="Reset segment pre-buffer to global" @click="emit('reset-pre')">Reset</button>
        </div>

        <div class="track-settings-row">
            <span class="track-settings-field-label">After</span>
            <input class="track-settings-input" type="number" min="0" :value="post" @change="emitPost" />
            <span class="track-settings-badge" :class="postIsCustom ? 'badge-custom' : 'badge-global'">
                {{ postIsCustom ? 'Custom' : 'Global' }}
            </span>
            <button class="track-settings-reset" title="Reset segment post-buffer to global" @click="emit('reset-post')">Reset</button>
        </div>
    </div>
</template>

<style scoped>
.track-settings {
    display: flex;
    flex-direction: column;
    gap: 6px;
    padding: 8px 10px;
    border: 1px solid var(--mud-palette-lines-default);
    border-radius: 8px;
    background: var(--mud-palette-background);
    min-width: 220px;
}

.track-settings-header {
    display: flex;
    align-items: center;
}

.track-settings-title {
    font-size: 0.9rem;
    font-weight: 600;
}

.track-settings-row {
    display: flex;
    align-items: center;
    gap: 6px;
}

.track-settings-field-label {
    font-size: 0.8rem;
    color: var(--mud-palette-text-secondary);
    width: 62px;
    flex-shrink: 0;
}

.track-settings-input {
    width: 72px;
    background: var(--mud-palette-background);
    border: 1px solid var(--mud-palette-lines-inputs);
    border-radius: 4px;
    padding: 3px 6px;
    color: var(--mud-palette-text-primary);
    font-size: 0.85rem;
    outline: none;
}

.track-settings-input:focus {
    border-color: var(--mud-palette-primary);
}

.track-settings-badge {
    font-size: 0.7rem;
    padding: 1px 6px;
    border-radius: 999px;
    white-space: nowrap;
}

.badge-global {
    color: var(--mud-palette-text-secondary);
    background: color-mix(in srgb, var(--mud-palette-text-secondary) 12%, transparent);
}

.badge-custom {
    color: var(--mud-palette-primary);
    background: color-mix(in srgb, var(--mud-palette-primary) 15%, transparent);
}

.track-settings-reset {
    margin-left: auto;
    border: none;
    background: transparent;
    color: var(--mud-palette-text-secondary);
    font-size: 0.75rem;
    cursor: pointer;
}

.track-settings-reset:hover {
    color: var(--mud-palette-primary);
}
</style>

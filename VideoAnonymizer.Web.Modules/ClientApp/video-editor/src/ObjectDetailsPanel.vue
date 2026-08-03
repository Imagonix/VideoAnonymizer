<script setup lang="ts">
import { ref } from 'vue';
import MudLikeCheckbox from './MudLikeCheckbox.vue';
import TrackThumbnail from './TrackThumbnail.vue';

withDefaults(defineProps<{
    label: string;
    trackId: number | null;
    included: boolean;
    shape: string | null;
    globalBlurSizePercent: number;
    blurSizePercentOverride: number | null;
    pre: number;
    preIsCustom: boolean;
    post: number;
    postIsCustom: boolean;
    canGoPrevious: boolean;
    canGoNext: boolean;
    thumbnailUrl?: string | null;
    thumbnailFallbackLabel?: string;
    thumbnailFallbackColor?: string;
}>(), {
    thumbnailUrl: null,
    thumbnailFallbackLabel: '?',
    thumbnailFallbackColor: 'transparent',
});

const emit = defineEmits<{
    (e: 'toggle-include', checked: boolean): void;
    (e: 'update-shape', shape: string): void;
    (e: 'update-blur-size', percent: number): void;
    (e: 'reset-blur-size'): void;
    (e: 'update-pre', valueMs: number): void;
    (e: 'update-post', valueMs: number): void;
    (e: 'reset-pre'): void;
    (e: 'reset-post'): void;
    (e: 'previous-occurrence'): void;
    (e: 'next-occurrence'): void;
    (e: 'adjust-detection'): void;
    (e: 'track-forward'): void;
    (e: 'merge'): void;
    (e: 'split'): void;
    (e: 'delete'): void;
}>();

const advancedOpen = ref(false);

function toggleAdvanced() {
    advancedOpen.value = !advancedOpen.value;
}

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
    <div data-testid="object-details-panel" class="object-details" @click.stop>
        <div class="details-header">
            <TrackThumbnail
              :object-url="thumbnailUrl"
              :fallback-label="thumbnailFallbackLabel ?? '?'"
              :fallback-color="thumbnailFallbackColor ?? 'transparent'"
              :size="40"
              :eager="true"
              :aria-label="`Representative image for ${label}`"
            />
            <div class="details-title-block">
                <MudLikeCheckbox :checked="included" @change="(value: boolean) => emit('toggle-include', value)">
                    <span class="details-title">{{ label }}</span>
                </MudLikeCheckbox>
            </div>
            <div class="occurrence-nav">
                <button
                    class="occurrence-nav-btn"
                    :disabled="!canGoPrevious"
                    title="Previous occurrence"
                    aria-label="Previous occurrence"
                    @click="emit('previous-occurrence')"
                >&#8249;</button>
                <button
                    class="occurrence-nav-btn"
                    :disabled="!canGoNext"
                    title="Next occurrence"
                    aria-label="Next occurrence"
                    @click="emit('next-occurrence')"
                >&#8250;</button>
            </div>
        </div>

        <div class="details-row">
            <span class="details-field-label">Shape</span>
            <select class="details-input" :value="shape ?? 'ellipse'" @change="(e) => emit('update-shape', (e.target as HTMLSelectElement).value)">
                <option value="ellipse">Ellipse</option>
                <option value="rectangle">Rectangle</option>
            </select>
        </div>

        <div class="details-row">
            <span class="details-field-label">Blur size</span>
            <input
                class="details-input"
                type="number"
                min="100"
                max="300"
                :value="blurSizePercentOverride ?? globalBlurSizePercent"
                @change="emitBlurSize"
            />
            <span class="details-badge" :class="blurSizePercentOverride == null ? 'badge-global' : 'badge-custom'">
                {{ blurSizePercentOverride == null ? 'Global' : 'Custom' }}
            </span>
            <button class="details-reset" title="Reset blur size to global" @click="emit('reset-blur-size')">Reset</button>
        </div>

        <div class="details-row">
            <span class="details-field-label">Before</span>
            <input class="details-input" type="number" min="0" :value="pre" @change="emitPre" />
            <span class="details-badge" :class="preIsCustom ? 'badge-custom' : 'badge-global'">
                {{ preIsCustom ? 'Custom' : 'Global' }}
            </span>
            <button class="details-reset" title="Reset segment pre-buffer to global" @click="emit('reset-pre')">Reset</button>
        </div>

        <div class="details-row">
            <span class="details-field-label">After</span>
            <input class="details-input" type="number" min="0" :value="post" @change="emitPost" />
            <span class="details-badge" :class="postIsCustom ? 'badge-custom' : 'badge-global'">
                {{ postIsCustom ? 'Custom' : 'Global' }}
            </span>
            <button class="details-reset" title="Reset segment post-buffer to global" @click="emit('reset-post')">Reset</button>
        </div>

        <div class="details-actions">
            <button class="details-action-btn" title="Adjust the selected detection" @click="emit('adjust-detection')">
                Adjust detection
            </button>
            <button
                class="details-action-btn"
                :class="{ 'details-action-btn--active': advancedOpen }"
                title="More actions for this track"
                :aria-expanded="advancedOpen"
                @click="toggleAdvanced"
            >Advanced</button>
        </div>

        <div v-if="advancedOpen" class="advanced-menu" data-testid="advanced-menu">
            <button class="advanced-item" title="Track this occurrence forward" @click="emit('track-forward')">
                Track forward
            </button>
            <button class="advanced-item" title="Merge this track with another selected track" @click="emit('merge')">
                Merge
            </button>
            <button class="advanced-item" title="Split selected occurrences out of this track" @click="emit('split')">
                Split
            </button>
            <button class="advanced-item advanced-item--danger" title="Delete this occurrence" @click="emit('delete')">
                Delete
            </button>
        </div>
    </div>
</template>

<style scoped>
.object-details {
    position: absolute;
    display: flex;
    flex-direction: column;
    gap: 6px;
    padding: 10px;
    border: 1px solid var(--mud-palette-lines-default);
    border-radius: 10px;
    background: var(--mud-palette-surface);
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);
    z-index: 30;
}

.details-header {
    display: flex;
    align-items: center;
    gap: 8px;
}

.details-title-block {
    min-width: 0;
}

.details-title {
    font-size: 0.9rem;
    font-weight: 600;
}

.occurrence-nav {
    display: flex;
    gap: 2px;
    margin-left: auto;
    flex-shrink: 0;
}

.occurrence-nav-btn {
    width: 26px;
    height: 26px;
    border: 1px solid var(--mud-palette-lines-inputs);
    border-radius: 4px;
    background: transparent;
    color: var(--mud-palette-text-primary);
    cursor: pointer;
    font-size: 1rem;
    line-height: 1;
}

.occurrence-nav-btn:disabled {
    opacity: 0.4;
    cursor: default;
}

.occurrence-nav-btn:focus-visible,
.details-action-btn:focus-visible,
.advanced-item:focus-visible,
.details-reset:focus-visible {
    outline: 2px solid color-mix(in srgb, var(--mud-palette-primary) 70%, transparent);
    outline-offset: 2px;
}

.details-row {
    display: flex;
    align-items: center;
    gap: 6px;
}

.details-field-label {
    font-size: 0.8rem;
    color: var(--mud-palette-text-secondary);
    width: 62px;
    flex-shrink: 0;
}

.details-input {
    width: 72px;
    background: var(--mud-palette-background);
    border: 1px solid var(--mud-palette-lines-inputs);
    border-radius: 4px;
    padding: 3px 6px;
    color: var(--mud-palette-text-primary);
    font-size: 0.85rem;
    outline: none;
}

.details-input:focus {
    border-color: var(--mud-palette-primary);
}

.details-badge {
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

.details-reset {
    margin-left: auto;
    border: none;
    background: transparent;
    color: var(--mud-palette-text-secondary);
    font-size: 0.75rem;
    cursor: pointer;
}

.details-reset:hover {
    color: var(--mud-palette-primary);
}

.details-actions {
    display: flex;
    gap: 8px;
    justify-content: flex-end;
    border-top: 1px solid var(--mud-palette-lines-default);
    padding-top: 8px;
}

.details-action-btn {
    padding: 5px 12px;
    border: 1px solid var(--mud-palette-lines-inputs);
    border-radius: 6px;
    background: transparent;
    color: var(--mud-palette-text-primary);
    cursor: pointer;
    font-size: 0.8rem;
    font-weight: 600;
}

.details-action-btn--active,
.details-action-btn:hover {
    border-color: var(--mud-palette-primary);
    color: var(--mud-palette-primary);
}

.advanced-menu {
    display: flex;
    flex-direction: column;
    gap: 4px;
    border-top: 1px solid var(--mud-palette-lines-default);
    padding-top: 8px;
}

.advanced-item {
    padding: 6px 10px;
    border: none;
    border-radius: 6px;
    background: transparent;
    color: var(--mud-palette-text-primary);
    text-align: left;
    cursor: pointer;
    font-size: 0.8rem;
    font-weight: 600;
}

.advanced-item:hover {
    background: color-mix(in srgb, var(--mud-palette-primary) 12%, transparent);
}

.advanced-item--danger {
    color: var(--mud-palette-error, #f44336);
}

.advanced-item--danger:hover {
    background: color-mix(in srgb, var(--mud-palette-error, #f44336) 12%, transparent);
}
</style>

<script setup lang="ts">
import { computed, ref } from 'vue';
import MudLikeCheckbox from './MudLikeCheckbox.vue';
import TrackThumbnail from './TrackThumbnail.vue';

const props = withDefaults(defineProps<{
    label: string;
    trackId: number | null;
    included: boolean;
    occurrenceBlurSizePercentOverride: number | null;
    trackBlurSizePercentOverride: number | null;
    globalBlurSizePercent: number;
    globalTimeBufferMs: number;
    shape: string | null;
    pre: number;
    preIsCustom: boolean;
    post: number;
    postIsCustom: boolean;
    preInactiveForGap?: boolean;
    postInactiveForGap?: boolean;
    hasGapBefore?: boolean;
    hasGapAfter?: boolean;
    gapBeforeMode?: string | null;
    gapAfterMode?: string | null;
    trackTimeBufferEffective: number | null;
    trackTimeBufferIsMixed: boolean;
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
    (e: 'update-occurrence-blur-size', percent: number): void;
    (e: 'reset-occurrence-blur-size'): void;
    (e: 'update-shape', shape: string): void;
    (e: 'update-blur-size', percent: number): void;
    (e: 'reset-blur-size'): void;
    (e: 'update-track-time-buffer', valueMs: number): void;
    (e: 'reset-track-time-buffer'): void;
    (e: 'update-pre', valueMs: number): void;
    (e: 'update-post', valueMs: number): void;
    (e: 'reset-pre'): void;
    (e: 'reset-post'): void;
    (e: 'update-gap-before', mode: string): void;
    (e: 'update-gap-after', mode: string): void;
    (e: 'previous-occurrence'): void;
    (e: 'next-occurrence'): void;
    (e: 'adjust-detection'): void;
    (e: 'track-forward'): void;
    (e: 'merge'): void;
    (e: 'split'): void;
    (e: 'delete'): void;
    (e: 'drag-start'): void;
    (e: 'drag-by', delta: { dx: number; dy: number }): void;
    (e: 'drag-end'): void;
}>();

const advancedOpen = ref(false);

function toggleAdvanced() {
    advancedOpen.value = !advancedOpen.value;
}

const occurrenceBlurEffective = computed(() =>
    props.occurrenceBlurSizePercentOverride ?? props.trackBlurSizePercentOverride ?? props.globalBlurSizePercent);
const occurrenceBlurBadge = computed(() =>
    props.occurrenceBlurSizePercentOverride != null ? 'Custom'
        : props.trackBlurSizePercentOverride != null ? 'Track'
            : 'Global');
const occurrenceBlurBadgeClass = computed(() =>
    props.occurrenceBlurSizePercentOverride != null ? 'badge-custom'
        : props.trackBlurSizePercentOverride != null ? 'badge-track'
            : 'badge-global');

const trackBlurEffective = computed(() => props.trackBlurSizePercentOverride ?? props.globalBlurSizePercent);
const trackBlurBadge = computed(() => (props.trackBlurSizePercentOverride != null ? 'Custom' : 'Global'));
const trackBlurBadgeClass = computed(() => (props.trackBlurSizePercentOverride != null ? 'badge-custom' : 'badge-global'));

const trackTimeBufferBadge = computed(() => {
    if (props.trackTimeBufferIsMixed) return 'Mixed';
    return props.trackTimeBufferEffective == null || props.trackTimeBufferEffective === props.globalTimeBufferMs
        ? 'Global'
        : 'Custom';
});
const trackTimeBufferBadgeClass = computed(() => {
    if (props.trackTimeBufferIsMixed) return 'badge-mixed';
    return props.trackTimeBufferEffective == null || props.trackTimeBufferEffective === props.globalTimeBufferMs
        ? 'badge-global'
        : 'badge-custom';
});

function emitBlurSize(event: Event) {
    const value = Number((event.target as HTMLInputElement).value);
    if (!isNaN(value)) emit('update-blur-size', value);
}

function emitOccurrenceBlurSize(event: Event) {
    const value = Number((event.target as HTMLInputElement).value);
    if (!isNaN(value)) emit('update-occurrence-blur-size', value);
}

function emitTrackTimeBuffer(event: Event) {
    const value = Number((event.target as HTMLInputElement).value);
    if (!isNaN(value)) emit('update-track-time-buffer', value);
}

function emitPre(event: Event) {
    const value = Number((event.target as HTMLInputElement).value);
    if (!isNaN(value)) emit('update-pre', value);
}

function emitPost(event: Event) {
    const value = Number((event.target as HTMLInputElement).value);
    if (!isNaN(value)) emit('update-post', value);
}

const handleRef = ref<HTMLElement | null>(null);
const pointerDrag = ref<{ pointerId: number; lastX: number; lastY: number } | null>(null);

function onHandlePointerDown(event: PointerEvent) {
    event.preventDefault();
    event.stopPropagation();
    const handle = handleRef.value;
    if (!handle) return;
    if (typeof handle.setPointerCapture === 'function') {
        try { handle.setPointerCapture(event.pointerId); } catch { /* unsupported */ }
    }
    pointerDrag.value = { pointerId: event.pointerId, lastX: event.clientX, lastY: event.clientY };
    emit('drag-start');
}

function onHandlePointerMove(event: PointerEvent) {
    const drag = pointerDrag.value;
    if (!drag || drag.pointerId !== event.pointerId) return;
    emit('drag-by', { dx: event.clientX - drag.lastX, dy: event.clientY - drag.lastY });
    pointerDrag.value = { pointerId: drag.pointerId, lastX: event.clientX, lastY: event.clientY };
}

function endPointerDrag(event: PointerEvent) {
    const drag = pointerDrag.value;
    if (!drag || drag.pointerId !== event.pointerId) return;
    pointerDrag.value = null;
    const handle = handleRef.value;
    if (handle && typeof handle.releasePointerCapture === 'function') {
        try { handle.releasePointerCapture(event.pointerId); } catch { /* unsupported */ }
    }
    emit('drag-end');
}

const DRAG_STEP = 10;

function onHandleKeydown(event: KeyboardEvent) {
    let dx = 0;
    let dy = 0;
    if (event.key === 'ArrowLeft') dx = -DRAG_STEP;
    else if (event.key === 'ArrowRight') dx = DRAG_STEP;
    else if (event.key === 'ArrowUp') dy = -DRAG_STEP;
    else if (event.key === 'ArrowDown') dy = DRAG_STEP;
    else return;
    event.preventDefault();
    emit('drag-start');
    emit('drag-by', { dx, dy });
    emit('drag-end');
}
</script>

<template>
    <div data-testid="object-details-panel" class="inspector-group" @click.stop>
        <section class="scope-panel scope-panel--occurrence" data-testid="scope-panel-occurrence">
            <header class="scope-panel-header">
                <div
                    ref="handleRef"
                    class="inspector-drag-handle"
                    role="button"
                    tabindex="0"
                    aria-label="Drag inspector"
                    title="Drag to reposition"
                    @pointerdown="onHandlePointerDown"
                    @pointermove="onHandlePointerMove"
                    @pointerup="endPointerDrag"
                    @pointercancel="endPointerDrag"
                    @keydown="onHandleKeydown"
                >⠿</div>
                <TrackThumbnail
                  :object-url="thumbnailUrl"
                  :fallback-label="thumbnailFallbackLabel ?? '?'"
                  :fallback-color="thumbnailFallbackColor ?? 'transparent'"
                  :size="36"
                  :eager="true"
                  :aria-label="`Representative image for ${label}`"
                />
                <span class="scope-panel-title">Current occurrence</span>
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
            </header>

            <div class="scope-panel-body">
                <div class="scope-row">
                    <MudLikeCheckbox :checked="included" @change="(value: boolean) => emit('toggle-include', value)">
                        <span class="details-title">{{ label }}</span>
                        <span class="scope-row-hint">Include in anonymization</span>
                    </MudLikeCheckbox>
                </div>

                <div class="scope-row">
                    <span class="details-field-label">Blur size</span>
                    <input
                        class="details-input"
                        type="number"
                        min="100"
                        max="300"
                        data-testid="occurrence-blur-input"
                        :value="occurrenceBlurEffective"
                        @change="emitOccurrenceBlurSize"
                    />
                    <span class="details-badge" :class="occurrenceBlurBadgeClass" data-testid="badge-occurrence-blur">
                        {{ occurrenceBlurBadge }}
                    </span>
                    <button class="details-reset" title="Reset blur size to the track or global value" @click="emit('reset-occurrence-blur-size')">Reset</button>
                </div>

                <div class="scope-actions">
                    <button class="details-action-btn" title="Adjust the selected detection" @click="emit('adjust-detection')">
                        Adjust detection
                    </button>
                </div>
            </div>
        </section>

        <section class="scope-panel scope-panel--segment" data-testid="scope-panel-segment">
            <header class="scope-panel-header">
                <span class="scope-panel-title">Current segment</span>
            </header>

            <div class="scope-panel-body">
                <div class="scope-row">
                    <span class="details-field-label">Before</span>
                    <input class="details-input" type="number" min="0" data-testid="segment-pre-input" :value="pre" @change="emitPre" />
                    <span class="details-badge" :class="preIsCustom ? 'badge-custom' : 'badge-global'" data-testid="badge-pre">
                        {{ preIsCustom ? 'Custom' : 'Global' }}
                    </span>
                    <button class="details-reset" title="Reset segment pre-buffer to global" @click="emit('reset-pre')">Reset</button>
                </div>
                <p
                    v-if="preInactiveForGap"
                    class="scope-row-hint gap-inactive-hint"
                    data-testid="pre-inactive-hint"
                >
                    Stored but inactive while Gap before uses Interpolate
                </p>

                <div v-if="hasGapBefore" class="scope-row" data-testid="gap-before-control">
                    <span class="details-field-label">Gap before</span>
                    <select
                        class="details-input"
                        data-testid="gap-before-select"
                        :value="gapBeforeMode ?? 'Interpolate'"
                        @change="(e) => emit('update-gap-before', (e.target as HTMLSelectElement).value)"
                    >
                        <option value="Interpolate">Interpolate</option>
                        <option value="UseBuffers">Use Before/After buffers</option>
                    </select>
                </div>

                <div class="scope-row">
                    <span class="details-field-label">After</span>
                    <input class="details-input" type="number" min="0" data-testid="segment-post-input" :value="post" @change="emitPost" />
                    <span class="details-badge" :class="postIsCustom ? 'badge-custom' : 'badge-global'" data-testid="badge-post">
                        {{ postIsCustom ? 'Custom' : 'Global' }}
                    </span>
                    <button class="details-reset" title="Reset segment post-buffer to global" @click="emit('reset-post')">Reset</button>
                </div>
                <p
                    v-if="postInactiveForGap"
                    class="scope-row-hint gap-inactive-hint"
                    data-testid="post-inactive-hint"
                >
                    Stored but inactive while Gap after uses Interpolate
                </p>

                <div v-if="hasGapAfter" class="scope-row" data-testid="gap-after-control">
                    <span class="details-field-label">Gap after</span>
                    <select
                        class="details-input"
                        data-testid="gap-after-select"
                        :value="gapAfterMode ?? 'Interpolate'"
                        @change="(e) => emit('update-gap-after', (e.target as HTMLSelectElement).value)"
                    >
                        <option value="Interpolate">Interpolate</option>
                        <option value="UseBuffers">Use Before/After buffers</option>
                    </select>
                </div>
            </div>
        </section>

        <section class="scope-panel scope-panel--track" data-testid="scope-panel-track">
            <header class="scope-panel-header">
                <span class="scope-panel-title">Entire track</span>
            </header>

            <div class="scope-panel-body">
                <div class="scope-row">
                    <span class="details-field-label">Shape</span>
                    <select class="details-input" :value="shape ?? 'ellipse'" data-testid="track-shape-input" @change="(e) => emit('update-shape', (e.target as HTMLSelectElement).value)">
                        <option value="ellipse">Ellipse</option>
                        <option value="rectangle">Rectangle</option>
                    </select>
                </div>

                <div class="scope-row">
                    <span class="details-field-label">Blur size</span>
                    <input
                        class="details-input"
                        type="number"
                        min="100"
                        max="300"
                        data-testid="track-blur-input"
                        :value="trackBlurEffective"
                        @change="emitBlurSize"
                    />
                    <span class="details-badge" :class="trackBlurBadgeClass" data-testid="badge-track-blur">
                        {{ trackBlurBadge }}
                    </span>
                    <button class="details-reset" title="Reset track blur size to global" @click="emit('reset-blur-size')">Reset</button>
                </div>

                <div class="scope-row">
                    <span class="details-field-label">Time buffer</span>
                    <input
                        class="details-input"
                        type="number"
                        min="0"
                        data-testid="track-time-buffer-input"
                        :value="trackTimeBufferIsMixed ? '' : (trackTimeBufferEffective ?? globalTimeBufferMs)"
                        :placeholder="trackTimeBufferIsMixed ? 'Mixed' : String(globalTimeBufferMs)"
                        @change="emitTrackTimeBuffer"
                    />
                    <span class="details-badge" :class="trackTimeBufferBadgeClass" data-testid="badge-time-buffer">
                        {{ trackTimeBufferBadge }}
                    </span>
                    <button class="details-reset" title="Clear all current segment boundaries to global" @click="emit('reset-track-time-buffer')">Reset</button>
                </div>

                <div class="scope-actions">
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
        </section>
    </div>
</template>

<style scoped>
.inspector-group {
    position: absolute;
    display: flex;
    flex-direction: column;
    gap: 10px;
    z-index: 30;
}

.scope-panel {
    display: flex;
    flex-direction: column;
    gap: 6px;
    width: 100%;
    padding: 8px 10px;
    border: 1px solid var(--mud-palette-lines-default);
    border-radius: 8px;
    background: var(--mud-palette-surface);
    box-shadow: 0 6px 18px rgba(0, 0, 0, 0.35);
}

.scope-panel-header {
    display: flex;
    align-items: center;
    gap: 8px;
    min-width: 0;
}

.inspector-drag-handle {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 22px;
    height: 26px;
    flex-shrink: 0;
    cursor: grab;
    color: var(--mud-palette-text-secondary);
    font-size: 0.9rem;
    line-height: 1;
    user-select: none;
    touch-action: none;
}

.inspector-drag-handle:active {
    cursor: grabbing;
}

.inspector-drag-handle:focus-visible {
    outline: 2px solid color-mix(in srgb, var(--mud-palette-primary) 70%, transparent);
    outline-offset: 2px;
    border-radius: 4px;
}

.scope-panel-title {
    font-size: 0.75rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: var(--mud-palette-text-secondary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    min-width: 0;
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

.scope-panel-body {
    display: flex;
    flex-direction: column;
    gap: 6px;
    min-width: 0;
}

.details-title {
    font-size: 0.9rem;
    font-weight: 600;
}

.scope-row-hint {
    display: block;
    font-size: 0.7rem;
    color: var(--mud-palette-text-secondary);
}

.gap-inactive-hint {
    margin: -2px 0 4px 68px;
    font-style: italic;
}

.scope-row {
    display: flex;
    align-items: center;
    gap: 6px;
    min-width: 0;
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
    min-width: 0;
}

.details-input:focus {
    border-color: var(--mud-palette-primary);
}

.details-badge {
    font-size: 0.7rem;
    padding: 1px 6px;
    border-radius: 999px;
    white-space: nowrap;
    flex-shrink: 0;
}

.badge-global {
    color: var(--mud-palette-text-secondary);
    background: color-mix(in srgb, var(--mud-palette-text-secondary) 12%, transparent);
}

.badge-custom {
    color: var(--mud-palette-primary);
    background: color-mix(in srgb, var(--mud-palette-primary) 15%, transparent);
}

.badge-track {
    color: var(--mud-palette-tertiary, var(--mud-palette-primary));
    background: color-mix(in srgb, var(--mud-palette-tertiary, var(--mud-palette-primary)) 15%, transparent);
}

.badge-mixed {
    color: var(--mud-palette-warning, #ff9800);
    background: color-mix(in srgb, var(--mud-palette-warning, #ff9800) 15%, transparent);
}

.details-reset {
    margin-left: auto;
    border: none;
    background: transparent;
    color: var(--mud-palette-text-secondary);
    font-size: 0.75rem;
    cursor: pointer;
    flex-shrink: 0;
}

.details-reset:hover {
    color: var(--mud-palette-primary);
}

.scope-actions {
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

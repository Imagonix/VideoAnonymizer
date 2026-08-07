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
/** Exact provenance badges for occurrence blur. */
const occurrenceBlurBadge = computed(() =>
    props.occurrenceBlurSizePercentOverride != null ? 'Occurrence override'
        : props.trackBlurSizePercentOverride != null ? 'Inherited: Track'
            : 'Inherited: Video');
const occurrenceBlurBadgeClass = computed(() =>
    props.occurrenceBlurSizePercentOverride != null ? 'badge-override'
        : props.trackBlurSizePercentOverride != null ? 'badge-track'
            : 'badge-video');

const trackBlurEffective = computed(() => props.trackBlurSizePercentOverride ?? props.globalBlurSizePercent);
const trackBlurBadge = computed(() =>
    props.trackBlurSizePercentOverride != null ? 'Track override' : 'Inherited: Video');
const trackBlurBadgeClass = computed(() =>
    props.trackBlurSizePercentOverride != null ? 'badge-override' : 'badge-video');

const segmentPreBadge = computed(() =>
    props.preIsCustom ? 'Segment override' : 'Inherited: Video');
const segmentPostBadge = computed(() =>
    props.postIsCustom ? 'Segment override' : 'Inherited: Video');
const segmentPreBadgeClass = computed(() =>
    props.preIsCustom ? 'badge-override' : 'badge-video');
const segmentPostBadgeClass = computed(() =>
    props.postIsCustom ? 'badge-override' : 'badge-video');

/** Checked means Interpolate; null/default renders checked. */
const gapBeforeInterpolate = computed(
    () => (props.gapBeforeMode ?? 'Interpolate') !== 'UseBuffers'
);
const gapAfterInterpolate = computed(
    () => (props.gapAfterMode ?? 'Interpolate') !== 'UseBuffers'
);

/** Outer track boundaries stay visible; gap-adjacent buffers show only when UseBuffers. */
const showPreControls = computed(
    () => !props.hasGapBefore || !gapBeforeInterpolate.value
);
const showPostControls = computed(
    () => !props.hasGapAfter || !gapAfterInterpolate.value
);

function emitBlurSize(event: Event) {
    const value = Number((event.target as HTMLInputElement).value);
    if (!isNaN(value)) emit('update-blur-size', value);
}

function emitOccurrenceBlurSize(event: Event) {
    const value = Number((event.target as HTMLInputElement).value);
    if (!isNaN(value)) emit('update-occurrence-blur-size', value);
}

function emitPre(event: Event) {
    const value = Number((event.target as HTMLInputElement).value);
    if (!isNaN(value)) emit('update-pre', value);
}

function emitPost(event: Event) {
    const value = Number((event.target as HTMLInputElement).value);
    if (!isNaN(value)) emit('update-post', value);
}

function onGapBeforeChange(checked: boolean) {
    emit('update-gap-before', checked ? 'Interpolate' : 'UseBuffers');
}

function onGapAfterChange(checked: boolean) {
    emit('update-gap-after', checked ? 'Interpolate' : 'UseBuffers');
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

                <div class="scope-row scope-row--wrap">
                    <span class="details-field-label">Blur size</span>
                    <input
                        class="details-input"
                        type="number"
                        min="100"
                        max="300"
                        step="10"
                        data-testid="occurrence-blur-input"
                        :value="occurrenceBlurEffective"
                        @change="emitOccurrenceBlurSize"
                    />
                    <span class="details-badge" :class="occurrenceBlurBadgeClass" data-testid="badge-occurrence-blur">
                        {{ occurrenceBlurBadge }}
                    </span>
                    <button
                        class="details-reset"
                        title="Reset to inherited value"
                        aria-label="Reset to inherited value"
                        @click="emit('reset-occurrence-blur-size')"
                    >Reset</button>
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
                <div
                    v-if="hasGapBefore"
                    class="scope-row scope-row--checkbox"
                    data-testid="gap-before-control"
                >
                    <MudLikeCheckbox
                        :checked="gapBeforeInterpolate"
                        @change="onGapBeforeChange"
                    >
                        <span class="gap-checkbox-label" data-testid="gap-before-label">Interpolate gap before</span>
                    </MudLikeCheckbox>
                </div>

                <div v-if="showPreControls" class="scope-row scope-row--wrap" data-testid="segment-pre-controls">
                    <span class="details-field-label">Before</span>
                    <input
                        class="details-input"
                        type="number"
                        min="0"
                        step="100"
                        data-testid="segment-pre-input"
                        :value="pre"
                        @change="emitPre"
                    />
                    <span class="details-badge" :class="segmentPreBadgeClass" data-testid="badge-pre">
                        {{ segmentPreBadge }}
                    </span>
                    <button
                        class="details-reset"
                        title="Reset to video value"
                        aria-label="Reset to video value"
                        @click="emit('reset-pre')"
                    >Reset</button>
                </div>

                <div
                    v-if="hasGapAfter"
                    class="scope-row scope-row--checkbox"
                    data-testid="gap-after-control"
                >
                    <MudLikeCheckbox
                        :checked="gapAfterInterpolate"
                        @change="onGapAfterChange"
                    >
                        <span class="gap-checkbox-label" data-testid="gap-after-label">Interpolate gap after</span>
                    </MudLikeCheckbox>
                </div>

                <div v-if="showPostControls" class="scope-row scope-row--wrap" data-testid="segment-post-controls">
                    <span class="details-field-label">After</span>
                    <input
                        class="details-input"
                        type="number"
                        min="0"
                        step="100"
                        data-testid="segment-post-input"
                        :value="post"
                        @change="emitPost"
                    />
                    <span class="details-badge" :class="segmentPostBadgeClass" data-testid="badge-post">
                        {{ segmentPostBadge }}
                    </span>
                    <button
                        class="details-reset"
                        title="Reset to video value"
                        aria-label="Reset to video value"
                        @click="emit('reset-post')"
                    >Reset</button>
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

                <div class="scope-row scope-row--wrap">
                    <span class="details-field-label">Blur size</span>
                    <input
                        class="details-input"
                        type="number"
                        min="100"
                        max="300"
                        step="10"
                        data-testid="track-blur-input"
                        :value="trackBlurEffective"
                        @change="emitBlurSize"
                    />
                    <span class="details-badge" :class="trackBlurBadgeClass" data-testid="badge-track-blur">
                        {{ trackBlurBadge }}
                    </span>
                    <button
                        class="details-reset"
                        title="Reset to video value"
                        aria-label="Reset to video value"
                        @click="emit('reset-blur-size')"
                    >Reset</button>
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
    box-sizing: border-box;
    min-width: 0;
    max-width: 100%;
    overflow-x: hidden;
}

.scope-panel {
    display: flex;
    flex-direction: column;
    gap: 6px;
    width: 100%;
    box-sizing: border-box;
    padding: 8px 10px;
    border: 1px solid var(--mud-palette-lines-default);
    border-radius: 8px;
    background: var(--mud-palette-surface);
    box-shadow: 0 6px 18px rgba(0, 0, 0, 0.35);
    min-width: 0;
}

.scope-panel-header {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
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
    white-space: normal;
    overflow: visible;
    text-overflow: clip;
    min-width: 0;
    flex: 1 1 auto;
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
    white-space: normal;
    word-break: break-word;
}

.scope-row-hint {
    display: block;
    font-size: 0.7rem;
    color: var(--mud-palette-text-secondary);
    white-space: normal;
    word-break: break-word;
}

.scope-row {
    display: flex;
    align-items: center;
    gap: 6px;
    min-width: 0;
}

.scope-row--wrap {
    flex-wrap: wrap;
}

.scope-row--checkbox {
    flex-wrap: wrap;
    align-items: flex-start;
}

.gap-checkbox-label {
    display: inline-block;
    font-size: 0.85rem;
    font-weight: 600;
    white-space: normal;
    word-break: break-word;
    line-height: 1.3;
}

.details-field-label {
    font-size: 0.8rem;
    color: var(--mud-palette-text-secondary);
    min-width: 62px;
    flex: 0 1 auto;
    white-space: normal;
    word-break: break-word;
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
    flex: 0 1 auto;
}

.details-input:focus {
    border-color: var(--mud-palette-primary);
}

.details-badge {
    font-size: 0.7rem;
    padding: 1px 6px;
    border-radius: 999px;
    white-space: normal;
    flex-shrink: 0;
}

.badge-video {
    color: var(--mud-palette-text-secondary);
    background: color-mix(in srgb, var(--mud-palette-text-secondary) 12%, transparent);
}

.badge-override {
    color: var(--mud-palette-primary);
    background: color-mix(in srgb, var(--mud-palette-primary) 15%, transparent);
}

.badge-track {
    color: var(--mud-palette-tertiary, var(--mud-palette-primary));
    background: color-mix(in srgb, var(--mud-palette-tertiary, var(--mud-palette-primary)) 15%, transparent);
}

.details-reset {
    margin-left: auto;
    border: none;
    background: transparent;
    color: var(--mud-palette-text-secondary);
    font-size: 0.75rem;
    cursor: pointer;
    flex-shrink: 0;
    white-space: normal;
}

.details-reset:hover {
    color: var(--mud-palette-primary);
}

.scope-actions {
    display: flex;
    flex-wrap: wrap;
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
    white-space: normal;
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
    white-space: normal;
    word-break: break-word;
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

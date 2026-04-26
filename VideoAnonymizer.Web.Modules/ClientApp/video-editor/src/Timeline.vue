<script setup lang="ts">
    import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
    import PlaybackIndicator from './PlaybackIndicator.vue';

    const props = defineProps<{
        duration: number;
        currentTime: number;
    }>();

    const emit = defineEmits<{
        (e: 'seek', time: number): void;
    }>();

    const minZoom = 1;
    const maxZoom = 120;
    const zoomStep = 1.25;
    const tickIntervals = [
        0.1, 0.2, 0.5,
        1, 2, 5, 10, 15, 30,
        60, 120, 300, 600, 900, 1800,
        3600, 7200
    ];

    const viewportRef = ref<HTMLElement | null>(null);
    const viewportWidth = ref(1);
    const zoomLevel = ref(1);
    let resizeObserver: ResizeObserver | null = null;

    const contentWidth = computed(() => {
        return Math.max(viewportWidth.value, 1) * zoomLevel.value;
    });

    const contentWidthPx = computed(() => `${contentWidth.value}px`);
    const pixelsPerSecond = computed(() => {
        if (!props.duration || props.duration <= 0) return 0;
        return contentWidth.value / props.duration;
    });

    const zoomPercent = computed(() => `${Math.round(zoomLevel.value * 100)}%`);
    const formattedCurrentTime = computed(() => formatTime(props.currentTime));
    const formattedDuration = computed(() => formatTime(props.duration));

    const ticks = computed(() => {
        if (!props.duration || props.duration <= 0 || pixelsPerSecond.value <= 0) {
            return [];
        }

        const targetSpacing = 88;
        const interval = tickIntervals.find(x => x * pixelsPerSecond.value >= targetSpacing)
            ?? tickIntervals[tickIntervals.length - 1];
        const result: { time: number; left: string; label: string }[] = [];
        const maxTicks = 500;

        for (let time = 0, index = 0; time <= props.duration && index < maxTicks; time += interval, index++) {
            const normalizedTime = Number(time.toFixed(3));
            result.push({
                time: normalizedTime,
                left: `${(normalizedTime / props.duration) * 100}%`,
                label: formatTime(normalizedTime, interval < 1)
            });
        }

        const last = result[result.length - 1];
        if (!last || Math.abs(last.time - props.duration) > 0.001) {
            result.push({
                time: props.duration,
                left: '100%',
                label: formatTime(props.duration)
            });
        }

        return result;
    });

    const currentTimeLeft = computed(() => {
        if (!props.duration || props.duration <= 0) return '0%';
        const ratio = Math.max(0, Math.min(1, props.currentTime / props.duration));
        return `${ratio * 100}%`;
    });

    function clamp(value: number, min: number, max: number) {
        return Math.max(min, Math.min(max, value));
    }

    function formatTime(value: number, forceTenths = true) {
        const safeValue = Math.max(0, Number.isFinite(value) ? value : 0);
        const wholeSeconds = Math.floor(safeValue);
        const tenths = Math.floor((safeValue - wholeSeconds) * 10);
        const hours = Math.floor(wholeSeconds / 3600);
        const minutes = Math.floor((wholeSeconds % 3600) / 60);
        const seconds = wholeSeconds % 60;
        const secondsText = seconds.toString().padStart(2, '0');

        if (hours > 0) {
            const minutesText = minutes.toString().padStart(2, '0');
            return forceTenths
                ? `${hours}:${minutesText}:${secondsText}.${tenths}`
                : `${hours}:${minutesText}:${secondsText}`;
        }

        return forceTenths
            ? `${minutes}:${secondsText}.${tenths}`
            : `${minutes}:${secondsText}`;
    }

    function updateViewportWidth() {
        if (!viewportRef.value) return;
        viewportWidth.value = Math.max(1, viewportRef.value.clientWidth);
        syncScrollToCurrentTime();
    }

    function getAnchorViewportX() {
        const viewport = viewportRef.value;
        if (!viewport || !props.duration || props.duration <= 0) return viewportWidth.value / 2;

        const currentTimeX = (props.currentTime / props.duration) * contentWidth.value;
        const visibleCurrentTimeX = currentTimeX - viewport.scrollLeft;

        if (visibleCurrentTimeX >= 0 && visibleCurrentTimeX <= viewport.clientWidth) {
            return visibleCurrentTimeX;
        }

        return viewport.clientWidth / 2;
    }

    function getCurrentTimeX() {
        if (!props.duration || props.duration <= 0) return 0;
        const ratio = clamp(props.currentTime / props.duration, 0, 1);
        return ratio * contentWidth.value;
    }

    function syncScrollToCurrentTime() {
        const viewport = viewportRef.value;
        if (!viewport || !props.duration || props.duration <= 0) return;

        const maxScrollLeft = Math.max(0, contentWidth.value - viewport.clientWidth);
        if (maxScrollLeft <= 0) {
            viewport.scrollLeft = 0;
            return;
        }

        const fixedIndicatorX = viewport.clientWidth / 2;
        const targetScrollLeft = clamp(getCurrentTimeX() - fixedIndicatorX, 0, maxScrollLeft);
        viewport.scrollLeft = targetScrollLeft;
    }

    async function setZoom(nextZoom: number, anchorViewportX = getAnchorViewportX()) {
        const viewport = viewportRef.value;
        const currentWidth = contentWidth.value;
        const anchorRatio = viewport && currentWidth > 0
            ? (viewport.scrollLeft + anchorViewportX) / currentWidth
            : 0;

        zoomLevel.value = clamp(nextZoom, minZoom, maxZoom);
        await nextTick();

        if (!viewport) return;
        viewport.scrollLeft = Math.max(0, (anchorRatio * contentWidth.value) - anchorViewportX);
        syncScrollToCurrentTime();
    }

    function zoomIn() {
        setZoom(zoomLevel.value * zoomStep);
    }

    function zoomOut() {
        setZoom(zoomLevel.value / zoomStep);
    }

    function fitTimeline() {
        zoomLevel.value = minZoom;
        nextTick(() => {
            if (viewportRef.value) {
                viewportRef.value.scrollLeft = 0;
            }
        });
    }

    function onZoomInput(e: Event) {
        const value = Number((e.target as HTMLInputElement).value);
        setZoom(value);
    }

    function onClick(e: MouseEvent) {
        if (!viewportRef.value || props.duration <= 0) return;

        const rect = viewportRef.value.getBoundingClientRect();
        const x = e.clientX - rect.left + viewportRef.value.scrollLeft;
        const ratio = clamp(x / contentWidth.value, 0, 1);

        emit('seek', ratio * props.duration);
    }

    function onWheel(e: WheelEvent) {
        if ((!e.ctrlKey && !e.metaKey) || props.duration <= 0 || !viewportRef.value) return;

        e.preventDefault();

        const rect = viewportRef.value.getBoundingClientRect();
        const anchorViewportX = clamp(e.clientX - rect.left, 0, viewportRef.value.clientWidth);
        const direction = e.deltaY < 0 ? zoomStep : 1 / zoomStep;

        setZoom(zoomLevel.value * direction, anchorViewportX);
    }

    onMounted(() => {
        updateViewportWidth();
        syncScrollToCurrentTime();

        if (!viewportRef.value) return;
        resizeObserver = new ResizeObserver(updateViewportWidth);
        resizeObserver.observe(viewportRef.value);
    });

    watch(
        () => [props.currentTime, props.duration, contentWidth.value, viewportWidth.value],
        () => nextTick(syncScrollToCurrentTime)
    );

    onBeforeUnmount(() => {
        resizeObserver?.disconnect();
    });
</script>

<template>
    <div class="timeline-shell">
        <div class="timeline-toolbar">
            <div class="timeline-time-readout" aria-live="polite">
                {{ formattedCurrentTime }} / {{ formattedDuration }}
            </div>
            <div class="timeline-zoom-controls" aria-label="Timeline zoom">
                <button type="button" class="timeline-icon-button" title="Zoom out" aria-label="Zoom out"
                    :disabled="zoomLevel <= minZoom" @click="zoomOut">
                    -
                </button>
                <input class="timeline-zoom-slider" type="range" :min="minZoom" :max="maxZoom" step="0.1"
                    :value="zoomLevel" aria-label="Timeline zoom level" @input="onZoomInput" />
                <button type="button" class="timeline-icon-button" title="Zoom in" aria-label="Zoom in"
                    :disabled="zoomLevel >= maxZoom" @click="zoomIn">
                    +
                </button>
                <button type="button" class="timeline-fit-button" title="Fit timeline" @click="fitTimeline">
                    Fit
                </button>
                <span class="timeline-zoom-value">{{ zoomPercent }}</span>
            </div>
        </div>
        <div class="timeline-viewport" ref="viewportRef" @click="onClick" @wheel="onWheel">
            <div class="timeline" data-testid="timeline" :style="{ width: contentWidthPx }">
                <PlaybackIndicator :time="currentTime" :duration="duration" />
                <div class="timeline-ruler">
                    <div v-for="tick in ticks" :key="tick.time" class="timeline-tick" :style="{ left: tick.left }">
                        <span>{{ tick.label }}</span>
                    </div>
                    <div class="timeline-current-time-marker" :style="{ left: currentTimeLeft }">
                        {{ formattedCurrentTime }}
                    </div>
                </div>
                <slot />
            </div>
        </div>
    </div>
</template>

<style scoped>
    .timeline-shell {
        margin: 16px;
        min-width: 0;
    }

    .timeline-toolbar {
        position: sticky;
        top: 0;
        z-index: 120;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 16px;
        min-height: 32px;
        margin-bottom: 12px;
        background: var(--mud-palette-surface);
    }

    .timeline-time-readout {
        font-variant-numeric: tabular-nums;
        font-size: 13px;
        line-height: 1;
        white-space: nowrap;
    }

    .timeline-zoom-controls {
        display: flex;
        align-items: center;
        gap: 8px;
        min-width: 0;
    }

    .timeline-icon-button,
    .timeline-fit-button {
        height: 28px;
        border: 1px solid var(--mud-palette-lines-default);
        border-radius: 6px;
        background: var(--mud-palette-surface);
        color: var(--mud-palette-text-primary);
        font: inherit;
    }

    .timeline-icon-button {
        width: 28px;
        font-size: 18px;
        line-height: 1;
    }

    .timeline-fit-button {
        padding: 0 10px;
        font-size: 12px;
    }

    .timeline-icon-button:disabled {
        color: var(--mud-palette-text-disabled);
        cursor: default;
    }

    .timeline-zoom-slider {
        width: clamp(120px, 20vw, 260px);
    }

    .timeline-zoom-value {
        min-width: 48px;
        text-align: right;
        font-size: 12px;
        color: var(--mud-palette-text-secondary);
        font-variant-numeric: tabular-nums;
    }

    .timeline-viewport {
        min-width: 0;
        overflow-x: auto;
        overflow-y: visible;
        scrollbar-gutter: stable;
    }

    .timeline {
        border-radius: 8px;
        min-width: 100%;
        position: relative;
    }

    .timeline-ruler {
        position: relative;
        height: 34px;
        margin-bottom: 12px;
        border-bottom: 1px solid var(--mud-palette-lines-default);
        background: var(--mud-palette-surface);
    }

    .timeline-tick {
        position: absolute;
        top: 0;
        bottom: 0;
        width: 1px;
        transform: translateX(-50%);
        background: var(--mud-palette-lines-default);
        color: var(--mud-palette-text-secondary);
        pointer-events: none;
    }

    .timeline-tick span {
        position: absolute;
        top: 6px;
        left: 6px;
        font-size: 11px;
        line-height: 1;
        white-space: nowrap;
        font-variant-numeric: tabular-nums;
    }

    .timeline-current-time-marker {
        position: absolute;
        top: 50%;
        transform: translate(-50%, -50%);
        z-index: 100;
        white-space: nowrap;
        font-size: 12px;
        line-height: 1;
        pointer-events: none;
        color: var(--mud-palette-text-primary);
        background: var(--mud-palette-surface);
        padding: 2px 8px;
        border: 1px solid var(--mud-palette-primary);
        border-radius: 4px;
        font-variant-numeric: tabular-nums;
    }

    .timeline :deep(.timeline-row) {
        margin-bottom: 12px;
    }
</style>

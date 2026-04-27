<script setup lang="ts">
    import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
    import PlaybackIndicator from './PlaybackIndicator.vue';
    import TimelineOverview from './TimelineOverview.vue';
    import TimelineRuler from './TimelineRuler.vue';
    import TimelineToolbar from './TimelineToolbar.vue';
    import type { TimelineObjectCount } from './types';
    import { buildTimelineTicks, clamp } from './timelineUtils';

    const props = defineProps<{
        duration: number;
        currentTime: number;
        isPlaying: boolean;
        volume: number;
        objectCounts: TimelineObjectCount[];
    }>();

    const emit = defineEmits<{
        (e: 'seek', time: number): void;
        (e: 'toggle-playback'): void;
        (e: 'volume-change', volume: number): void;
    }>();

    const minZoom = 1;
    const maxZoom = 120;
    const zoomStep = 1.25;

    const viewportRef = ref<HTMLElement | null>(null);
    const viewportWidth = ref(1);
    const scrollLeft = ref(0);
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

    const ticks = computed(() => {
        return buildTimelineTicks(props.duration, pixelsPerSecond.value);
    });

    const viewportStartRatio = computed(() => {
        if (contentWidth.value <= 0) return 0;
        return clamp(scrollLeft.value / contentWidth.value, 0, 1);
    });

    const viewportEndRatio = computed(() => {
        if (contentWidth.value <= 0) return 1;
        return clamp((scrollLeft.value + viewportWidth.value) / contentWidth.value, 0, 1);
    });

    function updateViewportWidth() {
        if (!viewportRef.value) return;
        viewportWidth.value = Math.max(1, viewportRef.value.clientWidth);
        scrollLeft.value = viewportRef.value.scrollLeft;
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
        scrollLeft.value = viewport.scrollLeft;
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
        scrollLeft.value = viewport.scrollLeft;
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

    function onClick(e: MouseEvent) {
        if (!viewportRef.value || props.duration <= 0) return;

        const rect = viewportRef.value.getBoundingClientRect();
        const x = e.clientX - rect.left + viewportRef.value.scrollLeft;
        const ratio = clamp(x / contentWidth.value, 0, 1);

        emit('seek', ratio * props.duration);
    }

    function onWheel(e: WheelEvent) {
        if (props.duration <= 0 || !viewportRef.value) return;

        e.preventDefault();

        const rect = viewportRef.value.getBoundingClientRect();
        const anchorViewportX = clamp(e.clientX - rect.left, 0, viewportRef.value.clientWidth);
        const direction = e.deltaY < 0 ? zoomStep : 1 / zoomStep;

        setZoom(zoomLevel.value * direction, anchorViewportX);
    }

    function onScroll() {
        if (!viewportRef.value) return;
        scrollLeft.value = viewportRef.value.scrollLeft;
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
        <TimelineToolbar
            :current-time="currentTime"
            :duration="duration"
            :is-playing="isPlaying"
            :volume="volume"
            :zoom-level="zoomLevel"
            :min-zoom="minZoom"
            :max-zoom="maxZoom"
            @toggle-playback="emit('toggle-playback')"
            @volume-change="(volume: number) => emit('volume-change', volume)"
            @zoom-in="zoomIn"
            @zoom-out="zoomOut"
            @fit="fitTimeline"
            @zoom="setZoom"
        />
        <div class="timeline-viewport" ref="viewportRef" @click="onClick" @scroll="onScroll" @wheel="onWheel">
            <div class="timeline" data-testid="timeline" :style="{ width: contentWidthPx }">
                <PlaybackIndicator :time="currentTime" :duration="duration" />
                <TimelineRuler :current-time="currentTime" :duration="duration" :ticks="ticks" />
                <slot />
            </div>
        </div>
        <TimelineOverview
            :duration="duration"
            :current-time="currentTime"
            :viewport-start-ratio="viewportStartRatio"
            :viewport-end-ratio="viewportEndRatio"
            :object-counts="objectCounts"
            @seek="(time: number) => emit('seek', time)"
        />
    </div>
</template>

<style scoped>
    .timeline-shell {
        margin: 16px;
        min-width: 0;
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

    .timeline :deep(.timeline-row) {
        margin-bottom: 12px;
    }
</style>

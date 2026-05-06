<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import type { DetectedObjectDto, AnalyzedFrameDto, AnonymizationSettings } from './types';
import { useBoxGeometry } from './composables/useBoxGeometry';
import AddBoxDialog from './AddBoxDialog.vue';
import DetailedViewHeader from './DetailedViewHeader.vue';

const props = defineProps<{
    frame: AnalyzedFrameDto;
    videoRef: HTMLVideoElement | null;
    anonymizationSettings: AnonymizationSettings;
    mode: 'move' | 'resize' | 'add';
    frames: AnalyzedFrameDto[];
}>();

const emit = defineEmits<{
    (e: 'done'): void;
    (e: 'mode-change', mode: 'move' | 'resize' | 'add'): void;
    (e: 'add-box', x: number, y: number, width: number, height: number, className: string, trackId: 'new' | number): void;
    (e: 'box-updated', obj: DetectedObjectDto, beforeState: DetectedObjectDto[]): void;
}>();



const canvasRef = ref<HTMLCanvasElement | null>(null);
const overlayRef = ref<HTMLDivElement | null>(null);

const videoWidth = computed(() => props.videoRef?.videoWidth ?? 640);
const videoHeight = computed(() => props.videoRef?.videoHeight ?? 480);
const anonymizationSettings = computed(() => props.anonymizationSettings);
const { minBoxSize, clampPct, clampBox, applyClampedBox, getBlurPct, getBoxPct, getEdgeHandleStyle } = useBoxGeometry(
    videoWidth,
    videoHeight,
    anonymizationSettings
);

const dragState = ref<{ id: string; startX: number; startY: number; origX: number; origY: number } | null>(null);
const resizeState = ref<{ id: string; position: string; startX: number; startY: number; origX: number; origY: number; origW: number; origH: number } | null>(null);

const handlePositions = ['n', 's', 'w', 'e'] as const;

const drawState = ref<{ startX: number; startY: number; curX: number; curY: number } | null>(null);
const pendingLabel = ref<{ x: number; y: number; w: number; h: number } | null>(null);
const trackIdsInCurrentFrame = computed(() => {
    const ids = new Set<number>();
    for (const o of props.frame.detectedObjects) {
        if (o.trackId != null) ids.add(o.trackId);
    }
    return ids;
});

const existingTrackIds = computed(() => {
    const ids = new Set<number>();
    for (const f of props.frames) {
        for (const o of f.detectedObjects) {
            if (o.trackId != null) ids.add(o.trackId);
        }
    }
    return [...ids].sort((a, b) => a - b);
});

const drawPreviewStyle = computed(() => {
    if (!drawState.value) return null;
    const d = drawState.value;
    const px = Math.min(d.startX, d.curX);
    const py = Math.min(d.startY, d.curY);
    const pw = Math.abs(d.curX - d.startX);
    const ph = Math.abs(d.curY - d.startY);
    return { left: `${px}%`, top: `${py}%`, width: `${pw}%`, height: `${ph}%` };
});

const drawBlurPreviewStyle = computed(() => {
    if (!drawState.value) return null;
    const d = drawState.value;
    const px = Math.min(d.startX, d.curX);
    const py = Math.min(d.startY, d.curY);
    const pw = Math.abs(d.curX - d.startX);
    const ph = Math.abs(d.curY - d.startY);
    const scale = anonymizationSettings.value.blurSizePercent / 100;
    const cx = px + pw / 2;
    const cy = py + ph / 2;
    const ew = pw * scale;
    const eh = ph * scale;
    return {
        left: `${cx - ew / 2}%`,
        top: `${cy - eh / 2}%`,
        width: `${ew}%`,
        height: `${eh}%`,
        backgroundColor: 'color-mix(in srgb, var(--mud-palette-primary) 20%, transparent)',
        borderRadius: '50%',
    };
});

onMounted(() => {
    const canvas = canvasRef.value;
    const video = props.videoRef;
    if (!canvas || !video) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    canvas.width = videoWidth.value;
    canvas.height = videoHeight.value;
    try { ctx.drawImage(video, 0, 0, videoWidth.value, videoHeight.value); } catch {}
});

const activeBoxId = ref<string | null>(null);
const beforeEditStates = ref<Map<string, DetectedObjectDto>>(new Map());

function onBoxMouseDown(event: MouseEvent, obj: DetectedObjectDto) {
    const rect = overlayRef.value?.getBoundingClientRect();
    if (!rect) return;
    beforeEditStates.value.set(obj.id, JSON.parse(JSON.stringify(obj)));
    dragState.value = {
        id: obj.id,
        startX: event.clientX,
        startY: event.clientY,
        origX: obj.x,
        origY: obj.y,
    };
}

function onResizeHandleDown(event: MouseEvent, obj: DetectedObjectDto, position: string) {
    event.stopPropagation();
    const rect = overlayRef.value?.getBoundingClientRect();
    if (!rect) return;
    beforeEditStates.value.set(obj.id, JSON.parse(JSON.stringify(obj)));
    resizeState.value = {
        id: obj.id, position,
        startX: event.clientX, startY: event.clientY,
        origX: obj.x, origY: obj.y, origW: obj.width, origH: obj.height,
    };
}

function onOverlayMouseDown(event: MouseEvent) {
    if (props.mode !== 'add') return;
    const rect = overlayRef.value?.getBoundingClientRect();
    if (!rect) return;
    const px = clampPct(((event.clientX - rect.left) / rect.width) * 100);
    const py = clampPct(((event.clientY - rect.top) / rect.height) * 100);
    drawState.value = { startX: px, startY: py, curX: px, curY: py };
}

function onOverlayMouseMove(event: MouseEvent) {
    const rect = overlayRef.value?.getBoundingClientRect();
    if (!rect) return;

    if (drawState.value) {
        const px = clampPct(((event.clientX - rect.left) / rect.width) * 100);
        const py = clampPct(((event.clientY - rect.top) / rect.height) * 100);
        drawState.value.curX = px;
        drawState.value.curY = py;
        return;
    }

    if (resizeState.value) {
        const s = resizeState.value;
        const dx = ((event.clientX - s.startX) / rect.width) * videoWidth.value;
        const dy = ((event.clientY - s.startY) / rect.height) * videoHeight.value;
        const obj = props.frame.detectedObjects.find(o => o.id === s.id);
        if (!obj) return;
        let { origX, origY, origW, origH } = s;
        const pos = s.position;
        if (pos.includes('e')) origW = Math.max(minBoxSize, s.origW + dx);
        if (pos.includes('w')) { origX = s.origX + dx; origW = Math.max(minBoxSize, s.origW - dx); }
        if (pos.includes('s')) origH = Math.max(minBoxSize, s.origH + dy);
        if (pos.includes('n')) { origY = s.origY + dy; origH = Math.max(minBoxSize, s.origH - dy); }
        applyClampedBox(obj, origX, origY, origW, origH);
        return;
    }

    if (!dragState.value) return;
    const d = dragState.value;
    const dx = ((event.clientX - d.startX) / rect.width) * videoWidth.value;
    const dy = ((event.clientY - d.startY) / rect.height) * videoHeight.value;
    const obj = props.frame.detectedObjects.find(o => o.id === d.id);
    if (obj) {
        applyClampedBox(obj, d.origX + dx, d.origY + dy, obj.width, obj.height);
    }
}

function onOverlayMouseUp() {
    if (drawState.value) {
        const d = drawState.value;
        const x = Math.min(d.startX, d.curX);
        const y = Math.min(d.startY, d.curY);
        const w = Math.abs(d.curX - d.startX);
        const h = Math.abs(d.curY - d.startY);
        drawState.value = null;
        if (w > 0.5 && h > 0.5) {
            const box = clampBox(
                (x / 100) * videoWidth.value,
                (y / 100) * videoHeight.value,
                (w / 100) * videoWidth.value,
                (h / 100) * videoHeight.value
            );
            pendingLabel.value = {
                x: box.x,
                y: box.y,
                w: box.width,
                h: box.height,
            };
        }
        return;
    }

    let updatedObj: DetectedObjectDto | null = null;
    if (dragState.value) {
        updatedObj = props.frame.detectedObjects.find(o => o.id === dragState.value.id) ?? null;
        dragState.value = null;
    } else if (resizeState.value) {
        updatedObj = props.frame.detectedObjects.find(o => o.id === resizeState.value.id) ?? null;
        resizeState.value = null;
    }

    if (updatedObj) {
        const before = beforeEditStates.value.get(updatedObj.id)
            ? [beforeEditStates.value.get(updatedObj.id)!]
            : [];
        beforeEditStates.value.delete(updatedObj.id);
        emit('box-updated', updatedObj, before);
    }
}

function confirmAdd(label: string, selectedTrackId: 'new' | number) {
    if (!pendingLabel.value) return;
    const p = pendingLabel.value;
    const trackId = selectedTrackId !== 'new' && trackIdsInCurrentFrame.value.has(selectedTrackId)
        ? 'new'
        : selectedTrackId;
    emit('add-box', Math.round(p.x), Math.round(p.y), Math.round(p.w), Math.round(p.h), label || '', trackId);
    pendingLabel.value = null;
}

function cancelAdd() {
    pendingLabel.value = null;
}
</script>

<template>
        <div class="move-overlay" @mousedown="onOverlayMouseDown" @mousemove="onOverlayMouseMove" @mouseup="onOverlayMouseUp" @mouseleave="onOverlayMouseUp">
        <div class="move-modal">
            <DetailedViewHeader
              :mode="mode"
              @done="emit('done')"
              @mode-change="(nextMode) => emit('mode-change', nextMode)"
            />
            <div class="move-stage">
                <div class="move-stage-inner" :style="{ aspectRatio: `${videoWidth} / ${videoHeight}` }">
                <canvas ref="canvasRef" class="move-canvas" :class="{ 'move-canvas--add': mode === 'add' }" />
                <div ref="overlayRef" class="move-boxes" :class="{ 'move-boxes--add': mode === 'add' }">
                    <div
                      v-for="obj in frame.detectedObjects"
                      :key="obj.id"
                      class="move-obj-group"
                    >
                      <div class="move-blur" :style="getBlurPct(obj)" />
                      <div
                        class="move-box"
                        :class="{
                          'move-box--dragging': dragState?.id === obj.id,
                          'move-box--active': activeBoxId === obj.id,
                          'move-box--no-move': mode !== 'move'
                        }"
                        :style="getBoxPct(obj)"
                        @mouseenter="activeBoxId = obj.id"
                        @mouseleave="activeBoxId = null"
                        @mousedown.prevent="mode === 'move' ? onBoxMouseDown($event, obj) : undefined"
                      />
                      <template v-if="mode === 'resize'">
                        <div
                          v-for="pos in handlePositions"
                          :key="pos"
                          class="resize-handle"
                          :class="`resize-handle--${pos}`"
                          :style="getEdgeHandleStyle(obj, pos)"
                          @mousedown.prevent="onResizeHandleDown($event, obj, pos)"
                        />
                      </template>
                    </div>
                    <div v-if="drawBlurPreviewStyle" class="draw-blur-preview" :style="drawBlurPreviewStyle" />
                    <div v-if="drawPreviewStyle" class="draw-preview" :style="drawPreviewStyle" />
                </div>
                </div>
            </div>
        </div>
        <AddBoxDialog
          v-if="pendingLabel"
          :existing-track-ids="existingTrackIds"
          :track-ids-in-current-frame="trackIdsInCurrentFrame"
          @cancel="cancelAdd"
          @confirm="confirmAdd"
        />
    </div>
</template>

<style scoped>
.move-overlay {
    position: fixed;
    inset: 3rem;
    z-index: 1000;
    background: transparent;
    display: flex;
}

.move-modal {
    background: var(--mud-palette-surface);
    border-radius: 12px;
    display: flex;
    flex-direction: column;
    flex: 1;
    min-height: 0;
    box-shadow: 0 16px 48px rgba(0, 0, 0, 0.5);
}

.move-stage {
    position: relative;
    flex: 1;
    min-height: 0;
    overflow: hidden;
    background: #000;
}

.move-stage-inner {
    position: absolute;
    inset: 0;
    margin: auto;
    max-width: 100%;
    max-height: 100%;
    overflow: hidden;
}

.move-canvas {
    display: block;
    width: 100%;
    height: 100%;
}

.move-boxes {
    position: absolute;
    inset: 0;
    pointer-events: none;
}

.move-obj-group {
    position: absolute;
    inset: 0;
    pointer-events: none;
}

.move-blur {
    position: absolute;
    border: none;
    border-radius: 50%;
    box-sizing: border-box;
    pointer-events: none;
}

.move-box {
    position: absolute;
    border: 2px solid;
    box-sizing: border-box;
    cursor: move;
    pointer-events: auto;
    transition: box-shadow 0.1s;
}

.move-box--active {
    box-shadow: 0 0 0 2px color-mix(in srgb, var(--mud-palette-primary) 30%, transparent);
}

.move-box--dragging {
    border-style: solid;
    border-color: var(--mud-palette-primary) !important;
    box-shadow: 0 0 0 2px var(--mud-palette-primary);
}

.move-box--no-move {
    cursor: default;
}

.resize-handle {
    position: absolute;
    z-index: 5;
    pointer-events: auto;
    background: color-mix(in srgb, var(--mud-palette-primary) 15%, transparent);
    transition: background 0.15s;
}

.resize-handle:hover {
    background: color-mix(in srgb, var(--mud-palette-primary) 35%, transparent);
}
.resize-handle--n { cursor: ns-resize; }
.resize-handle--s { cursor: ns-resize; }
.resize-handle--w { cursor: ew-resize; }

.draw-preview {
    position: absolute;
    border: 2px dashed var(--mud-palette-primary);
    background: color-mix(in srgb, var(--mud-palette-primary) 10%, transparent);
    pointer-events: none;
    z-index: 20;
}

.draw-blur-preview {
    position: absolute;
    border: none;
    pointer-events: none;
    z-index: 19;
}

.move-canvas--add {
    cursor: crosshair;
}

.move-boxes--add .move-box {
    pointer-events: none;
    opacity: 0.5;
}

.move-boxes--add .move-blur {
    opacity: 0.3;
}
.resize-handle--e { cursor: ew-resize; }
</style>

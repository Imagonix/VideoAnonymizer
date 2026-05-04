<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue';
import type { DetectedObjectDto, AnalyzedFrameDto, AnonymizationSettings } from './types';
import { colorManager } from './services/ColorManager';

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
const minBoxSize = 10;

const dragState = ref<{ id: string; startX: number; startY: number; origX: number; origY: number } | null>(null);
const resizeState = ref<{ id: string; position: string; startX: number; startY: number; origX: number; origY: number; origW: number; origH: number } | null>(null);

const handlePositions = ['n', 's', 'w', 'e'] as const;

const drawState = ref<{ startX: number; startY: number; curX: number; curY: number } | null>(null);
const pendingLabel = ref<{ x: number; y: number; w: number; h: number } | null>(null);
const labelClass = ref('face');
const labelTrackId = ref<'new' | number>('new');
const labelInputRef = ref<HTMLInputElement | null>(null);
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

watch(pendingLabel, async (v) => { if (v) { await nextTick(); labelInputRef.value?.focus(); } });

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
    const scale = props.anonymizationSettings.blurSizePercent / 100;
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

onMounted(() => {});
onUnmounted(() => {});

function close() {
    emit('done');
}

function clamp(value: number, min: number, max: number) {
    return Math.min(Math.max(value, min), max);
}

function clampPct(value: number) {
    return clamp(value, 0, 100);
}

function clampBox(x: number, y: number, width: number, height: number) {
    const maxWidth = Math.max(1, videoWidth.value);
    const maxHeight = Math.max(1, videoHeight.value);
    const minWidth = Math.min(minBoxSize, maxWidth);
    const minHeight = Math.min(minBoxSize, maxHeight);
    const nextWidth = clamp(Math.round(width), minWidth, maxWidth);
    const nextHeight = clamp(Math.round(height), minHeight, maxHeight);

    return {
        x: clamp(Math.round(x), 0, maxWidth - nextWidth),
        y: clamp(Math.round(y), 0, maxHeight - nextHeight),
        width: nextWidth,
        height: nextHeight,
    };
}

function applyClampedBox(obj: DetectedObjectDto, x: number, y: number, width: number, height: number) {
    const box = clampBox(x, y, width, height);
    obj.x = box.x;
    obj.y = box.y;
    obj.width = box.width;
    obj.height = box.height;
}

function getBlurPct(obj: DetectedObjectDto) {
    const scale = props.anonymizationSettings.blurSizePercent / 100;
    const cx = (obj.x + obj.width / 2) / videoWidth.value * 100;
    const cy = (obj.y + obj.height / 2) / videoHeight.value * 100;
    const ew = (obj.width * scale) / videoWidth.value * 100;
    const eh = (obj.height * scale) / videoHeight.value * 100;
    const color = colorManager.getColor(obj);
    const fill = color.replace('hsl(', 'hsla(').replace(')', ', 0.3)');
    return {
        left: `${cx - ew / 2}%`,
        top: `${cy - eh / 2}%`,
        width: `${ew}%`,
        height: `${eh}%`,
        backgroundColor: fill,
    };
}

function getBoxPct(obj: DetectedObjectDto) {
    return {
        left: `${(obj.x / videoWidth.value) * 100}%`,
        top: `${(obj.y / videoHeight.value) * 100}%`,
        width: `${(obj.width / videoWidth.value) * 100}%`,
        height: `${(obj.height / videoHeight.value) * 100}%`,
        borderColor: colorManager.getColor(obj),
    };
}

function getEdgeHandleStyle(obj: DetectedObjectDto, position: string) {
    const px = (obj.x / videoWidth.value) * 100;
    const py = (obj.y / videoHeight.value) * 100;
    const pw = (obj.width / videoWidth.value) * 100;
    const ph = (obj.height / videoHeight.value) * 100;
    if (position === 'n') return { left: `${px}%`, top: `calc(${py}% - 3px)`, width: `${pw}%`, height: '6px' };
    if (position === 's') return { left: `${px}%`, top: `calc(${py + ph}% - 3px)`, width: `${pw}%`, height: '6px' };
    if (position === 'w') return { left: `calc(${px}% - 3px)`, top: `${py}%`, width: '6px', height: `${ph}%` };
    if (position === 'e') return { left: `calc(${px + pw}% - 3px)`, top: `${py}%`, width: '6px', height: `${ph}%` };
    return {};
}

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

function confirmAdd(label: string) {
    if (!pendingLabel.value) return;
    const p = pendingLabel.value;
    const trackId = labelTrackId.value !== 'new' && trackIdsInCurrentFrame.value.has(labelTrackId.value)
        ? 'new'
        : labelTrackId.value;
    emit('add-box', Math.round(p.x), Math.round(p.y), Math.round(p.w), Math.round(p.h), label || '', trackId);
    pendingLabel.value = null;
    labelTrackId.value = 'new';
}

function cancelAdd() {
    pendingLabel.value = null;
}
</script>

<template>
        <div class="move-overlay" @mousedown="onOverlayMouseDown" @mousemove="onOverlayMouseMove" @mouseup="onOverlayMouseUp" @mouseleave="onOverlayMouseUp">
        <div class="move-modal">
            <div class="move-header">
                <span class="move-title">Detailed View</span>
                <div class="move-mode-switch">
                    <button
                      class="mode-switch-btn"
                      :class="{ active: mode === 'move' }"
                      @click="emit('mode-change', 'move')"
                    >Move</button>
                    <button
                      class="mode-switch-btn"
                      :class="{ active: mode === 'resize' }"
                      @click="emit('mode-change', 'resize')"
                    >Resize</button>
                    <button
                      class="mode-switch-btn"
                      :class="{ active: mode === 'add' }"
                      @click="emit('mode-change', 'add')"
                    >Add</button>
                </div>
                <div class="move-actions">
                    <button class="close-btn" @click="close" title="Close">✕</button>
                </div>
            </div>
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
        <div v-if="pendingLabel" class="label-overlay" @mousedown.self="cancelAdd">
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
                    <button class="popup-btn" @click="cancelAdd">Cancel</button>
                    <button class="popup-btn primary" @click="confirmAdd(labelClass)">Add</button>
                </div>
            </div>
        </div>
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

.move-header {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 12px 16px;
    border-bottom: 1px solid var(--mud-palette-lines-default);
    flex-shrink: 0;
}

.move-mode-switch {
    display: inline-flex;
    border-radius: 8px;
    overflow: hidden;
    border: 1px solid var(--mud-palette-lines-default);
}

.mode-switch-btn {
    padding: 4px 14px;
    border: none;
    background: transparent;
    color: var(--mud-palette-text-secondary);
    cursor: pointer;
    font-size: 0.82rem;
    font-weight: 500;
    transition: background 0.15s, color 0.15s;
}

.mode-switch-btn:not(:last-child) {
    border-right: 1px solid var(--mud-palette-lines-default);
}

.mode-switch-btn.active {
    background: var(--mud-palette-primary);
    color: var(--mud-palette-primary-contrast-text);
}

.mode-switch-btn:hover:not(.active) {
    background: color-mix(in srgb, var(--mud-palette-primary) 8%, transparent);
    color: var(--mud-palette-text-primary);
}

.move-title {
    font-size: 1rem;
    font-weight: 600;
    color: var(--mud-palette-text-primary);
}

.move-actions {
    margin-left: auto;
}

.close-btn {
    width: 28px;
    height: 28px;
    border: none;
    border-radius: 4px;
    background: transparent;
    color: var(--mud-palette-text-secondary);
    cursor: pointer;
    font-size: 1rem;
    display: flex;
    align-items: center;
    justify-content: center;
}

.close-btn:hover {
    background: color-mix(in srgb, var(--mud-palette-action-default) 15%, transparent);
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

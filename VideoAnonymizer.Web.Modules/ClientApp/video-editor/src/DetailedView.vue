<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import type { DetectedObjectDto, AnalyzedFrameDto, AnonymizationSettings } from './types';
import { colorManager } from './services/ColorManager';

const props = defineProps<{
    frame: AnalyzedFrameDto;
    videoRef: HTMLVideoElement | null;
    anonymizationSettings: AnonymizationSettings;
    mode: 'move' | 'resize';
}>();

const emit = defineEmits<{
    (e: 'done'): void;
    (e: 'cancel'): void;
    (e: 'mode-change', mode: 'move' | 'resize'): void;
}>();



const canvasRef = ref<HTMLCanvasElement | null>(null);
const overlayRef = ref<HTMLDivElement | null>(null);

const videoWidth = computed(() => props.videoRef?.videoWidth ?? 640);
const videoHeight = computed(() => props.videoRef?.videoHeight ?? 480);

const originalPositions = new Map<string, { x: number; y: number; width: number; height: number }>();
for (const obj of props.frame.detectedObjects) {
    originalPositions.set(obj.id, { x: obj.x, y: obj.y, width: obj.width, height: obj.height });
}

const dragState = ref<{ id: string; startX: number; startY: number; origX: number; origY: number } | null>(null);
const resizeState = ref<{ id: string; position: string; startX: number; startY: number; origX: number; origY: number; origW: number; origH: number } | null>(null);

const handlePositions = ['n', 's', 'w', 'e'] as const;

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

function handleKeyDown(e: KeyboardEvent) {
    if (e.key === 'Escape') cancel();
}
onMounted(() => window.addEventListener('keydown', handleKeyDown));
onUnmounted(() => window.removeEventListener('keydown', handleKeyDown));

function cancel() {
    for (const obj of props.frame.detectedObjects) {
        const orig = originalPositions.get(obj.id);
        if (orig) { obj.x = orig.x; obj.y = orig.y; obj.width = orig.width; obj.height = orig.height; }
    }
    emit('cancel');
}

function done() {
    emit('done');
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

function onBoxMouseDown(event: MouseEvent, obj: DetectedObjectDto) {
    const rect = overlayRef.value?.getBoundingClientRect();
    if (!rect) return;
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
    resizeState.value = {
        id: obj.id, position,
        startX: event.clientX, startY: event.clientY,
        origX: obj.x, origY: obj.y, origW: obj.width, origH: obj.height,
    };
}

function onOverlayMouseMove(event: MouseEvent) {
    const rect = overlayRef.value?.getBoundingClientRect();
    if (!rect) return;

    if (resizeState.value) {
        const s = resizeState.value;
        const dx = ((event.clientX - s.startX) / rect.width) * videoWidth.value;
        const dy = ((event.clientY - s.startY) / rect.height) * videoHeight.value;
        const obj = props.frame.detectedObjects.find(o => o.id === s.id);
        if (!obj) return;
        let { origX, origY, origW, origH } = s;
        const pos = s.position;
        if (pos.includes('e')) origW = Math.max(10, s.origW + dx);
        if (pos.includes('w')) { origX = s.origX + dx; origW = Math.max(10, s.origW - dx); }
        if (pos.includes('s')) origH = Math.max(10, s.origH + dy);
        if (pos.includes('n')) { origY = s.origY + dy; origH = Math.max(10, s.origH - dy); }
        obj.x = Math.max(0, Math.round(origX));
        obj.y = Math.max(0, Math.round(origY));
        obj.width = Math.round(origW);
        obj.height = Math.round(origH);
        return;
    }

    if (!dragState.value) return;
    const d = dragState.value;
    const dx = ((event.clientX - d.startX) / rect.width) * videoWidth.value;
    const dy = ((event.clientY - d.startY) / rect.height) * videoHeight.value;
    const obj = props.frame.detectedObjects.find(o => o.id === d.id);
    if (obj) {
        obj.x = Math.max(0, Math.round(d.origX + dx));
        obj.y = Math.max(0, Math.round(d.origY + dy));
    }
}

function onOverlayMouseUp() {
    dragState.value = null;
    resizeState.value = null;
}
</script>

<template>
    <div class="move-overlay" @mousemove="onOverlayMouseMove" @mouseup="onOverlayMouseUp" @mouseleave="onOverlayMouseUp">
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
                </div>
                <div class="move-actions">
                    <button class="move-btn cancel-btn" @click="cancel">Cancel</button>
                    <button class="move-btn done-btn" @click="done">Done</button>
                </div>
            </div>
            <div class="move-stage">
                <div class="move-stage-inner" :style="{ aspectRatio: `${videoWidth} / ${videoHeight}` }">
                <canvas ref="canvasRef" class="move-canvas" />
                <div ref="overlayRef" class="move-boxes">
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
                </div>
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
    display: flex;
    gap: 8px;
}

.move-btn {
    padding: 6px 16px;
    border: none;
    border-radius: 6px;
    cursor: pointer;
    font-size: 0.85rem;
    font-weight: 500;
}

.cancel-btn {
    background: transparent;
    color: var(--mud-palette-text-secondary);
}

.cancel-btn:hover {
    background: color-mix(in srgb, var(--mud-palette-action-default) 10%, transparent);
}

.done-btn {
    background: var(--mud-palette-primary);
    color: var(--mud-palette-primary-contrast-text);
}

.done-btn:hover {
    opacity: 0.9;
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
.resize-handle--e { cursor: ew-resize; }
</style>

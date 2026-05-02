<script setup lang="ts">
import { computed } from 'vue';
import PauseCircleOutlineIcon from './icons/PauseCircleOutlineIcon.vue';
import PlayCircleOutlineIcon from './icons/PlayCircleOutlineIcon.vue';
import VolumeUpIcon from './icons/VolumeUpIcon.vue';
import { formatTimelineTime } from './timelineUtils';

const props = defineProps<{
  currentTime: number;
  duration: number;
  isPlaying: boolean;
  volume: number;
  zoomLevel: number;
  minZoom: number;
  maxZoom: number;
}>();

const emit = defineEmits<{
  (e: 'toggle-playback'): void;
  (e: 'volume-change', volume: number): void;
  (e: 'zoom-in'): void;
  (e: 'zoom-out'): void;
  (e: 'fit'): void;
  (e: 'zoom', value: number): void;
}>();

const formattedCurrentTime = computed(() => formatTimelineTime(props.currentTime));
const formattedDuration = computed(() => formatTimelineTime(props.duration));
const zoomPercent = computed(() => `${Math.round(props.zoomLevel * 100)}%`);

function onZoomInput(e: Event) {
  emit('zoom', Number((e.target as HTMLInputElement).value));
}

function onVolumeInput(e: Event) {
  emit('volume-change', Number((e.target as HTMLInputElement).value));
}
</script>

<template>
  <div class="timeline-toolbar">
    <div class="timeline-transport-controls">
      <button
        type="button"
        class="playback-button mud-icon-button"
        :aria-label="isPlaying ? 'Pause video' : 'Play video'"
        :title="isPlaying ? 'Pause' : 'Play'"
        @click="emit('toggle-playback')"
      >
        <PauseCircleOutlineIcon v-if="isPlaying" />
        <PlayCircleOutlineIcon v-else />
      </button>
      <div class="volume-control">
        <VolumeUpIcon />
        <input
          class="volume-slider"
          type="range"
          min="0"
          max="1"
          step="0.01"
          :value="volume"
          aria-label="Video volume"
          @input="onVolumeInput"
        />
      </div>
      <div class="timeline-time-readout" aria-live="polite">
        {{ formattedCurrentTime }} / {{ formattedDuration }}
      </div>
    </div>
    <div class="timeline-zoom-controls" aria-label="Timeline zoom">
      <button
        type="button"
        class="timeline-icon-button mud-icon-button"
        title="Zoom out"
        aria-label="Zoom out"
        :disabled="zoomLevel <= minZoom"
        @click="emit('zoom-out')"
      >
        <svg class="timeline-material-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
          <path d="M19 13H5v-2h14v2z" />
        </svg>
      </button>
      <input
        class="timeline-zoom-slider"
        type="range"
        :min="minZoom"
        :max="maxZoom"
        step="0.1"
        :value="zoomLevel"
        aria-label="Timeline zoom level"
        @input="onZoomInput"
      />
      <button
        type="button"
        class="timeline-icon-button mud-icon-button"
        title="Zoom in"
        aria-label="Zoom in"
        :disabled="zoomLevel >= maxZoom"
        @click="emit('zoom-in')"
      >
        <svg class="timeline-material-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
          <path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z" />
        </svg>
      </button>
      <button type="button" class="timeline-fit-button" title="Fit timeline" @click="emit('fit')">
        Fit
      </button>
      <span class="timeline-zoom-value">{{ zoomPercent }}</span>
    </div>
  </div>
</template>

<style scoped>
.timeline-toolbar {
  position: sticky;
  top: 0;
  z-index: 120;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  min-height: 48px;
  margin-bottom: 12px;
  background: var(--mud-palette-surface);
  color: var(--mud-palette-text-primary);
  font-family: var(--mud-typography-default-family);
}

.timeline-time-readout {
  font-variant-numeric: tabular-nums;
  font-size: 0.875rem;
  font-weight: 600;
  line-height: 1;
  white-space: nowrap;
}

.timeline-transport-controls {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.timeline-zoom-controls {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.playback-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 auto;
  width: 48px;
  height: 48px;
  padding: 12px;
  border: 0;
  border-radius: 50%;
  color: var(--mud-palette-primary);
  background: transparent;
  cursor: pointer;
  overflow: visible;
  font-size: 1.5rem;
  text-align: center;
  transition: background-color 0.15s ease, color 0.15s ease;
}

.playback-button:hover {
  background: var(--mud-palette-primary-hover);
}

.playback-button:focus-visible,
.timeline-icon-button:focus-visible {
  outline: none;
  background: var(--mud-palette-action-default-hover);
}

.timeline-fit-button:focus-visible {
  outline: 2px solid color-mix(in srgb, var(--mud-palette-primary) 70%, transparent);
  outline-offset: 2px;
}

.playback-button :deep(svg) {
  width: 24px;
  height: 24px;
}

.volume-control {
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--mud-palette-text-secondary);
}

.volume-control :deep(svg) {
  width: 24px;
  height: 24px;
}

.volume-slider {
  width: 110px;
}

.timeline-icon-button,
.timeline-fit-button {
  height: 36px;
  border-radius: var(--mud-default-borderradius);
  background: color-mix(in srgb, var(--mud-palette-surface) 88%, var(--mud-palette-primary) 12%);
  color: var(--mud-palette-text-primary);
  font: inherit;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.15s ease, border-color 0.15s ease, color 0.15s ease;
}

.timeline-icon-button:hover:not(:disabled),
.timeline-fit-button:hover {
  border-color: var(--mud-palette-primary);
  background: color-mix(in srgb, var(--mud-palette-primary) 14%, var(--mud-palette-surface));
}

.timeline-icon-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 auto;
  width: 48px;
  height: 48px;
  padding: 12px;
  border: 0;
  border-radius: 50%;
  background: transparent;
  color: var(--mud-palette-primary);
  overflow: visible;
  font-size: 1.5rem;
  line-height: 1;
  text-align: center;
}

.timeline-icon-button:hover:not(:disabled) {
  background: var(--mud-palette-primary-hover);
}

.timeline-fit-button {
  border: 1px solid var(--mud-palette-lines-inputs);
  padding: 0 14px;
  font-size: 0.875rem;
}

.timeline-icon-button:disabled {
  color: var(--mud-palette-text-disabled);
  background: transparent;
  cursor: default;
}

.timeline-material-icon {
  display: block;
  width: 24px;
  height: 24px;
  fill: currentColor;
}

.timeline-zoom-slider {
  width: clamp(120px, 20vw, 260px);
}

.timeline-zoom-value {
  min-width: 48px;
  text-align: right;
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--mud-palette-text-secondary);
  font-variant-numeric: tabular-nums;
}

.volume-slider,
.timeline-zoom-slider {
  height: 22px;
  margin: 0;
  background: transparent;
  cursor: pointer;
  accent-color: var(--mud-palette-primary);
  appearance: none;
  -webkit-appearance: none;
}

.volume-slider:focus-visible,
.timeline-zoom-slider:focus-visible {
  outline: none;
}

.volume-slider::-webkit-slider-runnable-track,
.timeline-zoom-slider::-webkit-slider-runnable-track {
  height: 6px;
  border-radius: 999px;
  background: var(--mud-palette-lines-inputs);
}

.volume-slider::-webkit-slider-thumb,
.timeline-zoom-slider::-webkit-slider-thumb {
  width: 18px;
  height: 18px;
  margin-top: -6px;
  border: 2px solid var(--mud-palette-surface);
  border-radius: 50%;
  background: var(--mud-palette-primary);
  box-shadow: 0 0 0 4px color-mix(in srgb, var(--mud-palette-primary) 16%, transparent);
  appearance: none;
  -webkit-appearance: none;
}

.volume-slider:hover::-webkit-slider-runnable-track,
.timeline-zoom-slider:hover::-webkit-slider-runnable-track {
  background: color-mix(in srgb, var(--mud-palette-primary) 42%, var(--mud-palette-lines-inputs));
}

.volume-slider:focus-visible::-webkit-slider-thumb,
.timeline-zoom-slider:focus-visible::-webkit-slider-thumb {
  box-shadow: 0 0 0 6px color-mix(in srgb, var(--mud-palette-primary) 26%, transparent);
}

.volume-slider::-moz-range-track,
.timeline-zoom-slider::-moz-range-track {
  height: 6px;
  border-radius: 999px;
  background: var(--mud-palette-lines-inputs);
}

.volume-slider::-moz-range-progress,
.timeline-zoom-slider::-moz-range-progress {
  height: 6px;
  border-radius: 999px;
  background: var(--mud-palette-primary);
}

.volume-slider::-moz-range-thumb,
.timeline-zoom-slider::-moz-range-thumb {
  width: 18px;
  height: 18px;
  border: 2px solid var(--mud-palette-surface);
  border-radius: 50%;
  background: var(--mud-palette-primary);
  box-shadow: 0 0 0 4px color-mix(in srgb, var(--mud-palette-primary) 16%, transparent);
}
</style>

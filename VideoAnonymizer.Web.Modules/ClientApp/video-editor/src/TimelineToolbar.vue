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
        class="playback-button"
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
        class="timeline-icon-button"
        title="Zoom out"
        aria-label="Zoom out"
        :disabled="zoomLevel <= minZoom"
        @click="emit('zoom-out')"
      >
        -
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
        class="timeline-icon-button"
        title="Zoom in"
        aria-label="Zoom in"
        :disabled="zoomLevel >= maxZoom"
        @click="emit('zoom-in')"
      >
        +
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

.timeline-transport-controls {
  display: flex;
  align-items: center;
  gap: 12px;
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
  width: 28px;
  height: 28px;
  border: 1px solid var(--mud-palette-lines-default);
  border-radius: 6px;
  color: var(--mud-palette-text-primary);
  background: var(--mud-palette-surface);
}

.volume-control {
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--mud-palette-text-secondary);
}

.volume-slider {
  width: 110px;
  accent-color: var(--mud-palette-primary);
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
  accent-color: var(--mud-palette-primary);
}

.timeline-zoom-value {
  min-width: 48px;
  text-align: right;
  font-size: 12px;
  color: var(--mud-palette-text-secondary);
  font-variant-numeric: tabular-nums;
}
</style>

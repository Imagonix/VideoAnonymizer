<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type {
  AnalyzedFrameDto,
  DetectedObjectDto,
  TimelineObject,
  VideoEditorProps
} from './types';

const props = defineProps<{
  state: VideoEditorProps;
}>();

const currentTime = ref(0);
const activeKeys = ref<string[]>([]);

const palette = [
  '#ef4444',
  '#22c55e',
  '#3b82f6',
  '#f59e0b',
  '#8b5cf6',
  '#ec4899',
  '#14b8a6',
  '#f97316'
];

function buildObjectKey(obj: DetectedObjectDto): string {
  if (obj.trackId !== null && obj.trackId !== undefined) {
    return `track-${obj.trackId}`;
  }

  const className = obj.className ?? 'object';
  return `frameobj-${className}-${obj.id}`;
}

function buildObjectLabel(obj: DetectedObjectDto): string {
  const className = obj.className ?? 'Object';

  if (obj.trackId !== null && obj.trackId !== undefined) {
    return `${className} #${obj.trackId}`;
  }

  return className;
}

const orderedFrames = computed<AnalyzedFrameDto[]>(() =>
  [...(props.state.frames ?? [])].sort((a, b) => a.timeSeconds - b.timeSeconds)
);

const timelineObjects = computed<TimelineObject[]>(() => {
  const map = new Map<string, TimelineObject>();

  for (const frame of orderedFrames.value) {
    for (const obj of frame.detectedObjects ?? []) {
      const key = buildObjectKey(obj);

      if (!map.has(key)) {
        map.set(key, {
          key,
          label: buildObjectLabel(obj),
          color: palette[map.size % palette.length],
          trackId: obj.trackId
        });
      }
    }
  }

  return Array.from(map.values());
});

watch(
  timelineObjects,
  (value) => {
    const selectedFromData = new Set<string>();

    for (const frame of orderedFrames.value) {
      for (const obj of frame.detectedObjects ?? []) {
        if (obj.selected) {
          selectedFromData.add(buildObjectKey(obj));
        }
      }
    }

    activeKeys.value = selectedFromData.size > 0
      ? Array.from(selectedFromData)
      : value.map(x => x.key);
  },
  { immediate: true }
);

const currentFrame = computed<AnalyzedFrameDto | null>(() => {
  if (orderedFrames.value.length === 0) {
    return null;
  }

  let selected = orderedFrames.value[0];

  for (const frame of orderedFrames.value) {
    if (frame.timeSeconds <= currentTime.value) {
      selected = frame;
    } else {
      break;
    }
  }

  return selected;
});

const currentFrameObjects = computed(() => {
  const frame = currentFrame.value;
  if (!frame) return [];

  return frame.detectedObjects.filter(obj => activeKeys.value.includes(buildObjectKey(obj)));
});

function toggleObject(objectKey: string, checked: boolean) {
  if (checked) {
    if (!activeKeys.value.includes(objectKey)) {
      activeKeys.value = [...activeKeys.value, objectKey];
    }
    return;
  }

  activeKeys.value = activeKeys.value.filter(x => x !== objectKey);
}

function timelineMoments(objectKey: string): number[] {
  return orderedFrames.value
    .filter(frame => (frame.detectedObjects ?? []).some(obj => buildObjectKey(obj) === objectKey))
    .map(frame => frame.timeSeconds);
}

function onVideoTimeUpdate(event: Event) {
  const video = event.target as HTMLVideoElement;
  currentTime.value = video.currentTime;
}

function colorForKey(objectKey: string): string {
  return timelineObjects.value.find(x => x.key === objectKey)?.color ?? '#3b82f6';
}
</script>

<template>
    <div class="video-editor">
        <div class="top-layout">
            <div class="preview-panel">
                <div class="video-stage">
                    <video :src="state.videoSourceUrl"
                           controls
                           @timeupdate="onVideoTimeUpdate"></video>

                    <div class="overlay">
                        <div v-for="item in currentFrameObjects"
                             :key="item.id"
                             class="bbox"
                             :style="{
                left: `${item.x}px`,
                top: `${item.y}px`,
                width: `${item.width}px`,
                height: `${item.height}px`,
                borderColor: colorForKey(buildObjectKey(item))
              }">
                            <span class="bbox-label"
                                  :style="{ backgroundColor: colorForKey(buildObjectKey(item)) }">
                                {{ buildObjectLabel(item) }}
                            </span>
                        </div>
                    </div>
                </div>
            </div>

            <aside class="object-panel" data-testid="object-list">
                <h3>Detected objects</h3>

                <div v-for="item in (currentFrame?.detectedObjects ?? [])"
                     :key="item.id"
                     class="object-row">
                    <label>
                        <input type="checkbox"
                               :checked="activeKeys.includes(buildObjectKey(item))"
                               @change="toggleObject(buildObjectKey(item), ($event.target as HTMLInputElement).checked)" />
                        {{ buildObjectLabel(item) }}
                    </label>
                </div>
            </aside>
        </div>

        <section class="timeline-panel" data-testid="timeline">
            <div class="timeline-grid">
                <div class="timeline-left" data-testid="timeline-object-list">
                    <div v-for="item in timelineObjects"
                         :key="item.key"
                         class="timeline-object"
                         data-testid="timeline-object"
                         :style="{ borderLeftColor: item.color }">
                        <label class="timeline-object-label">
                            <input type="checkbox"
                                   :checked="activeKeys.includes(item.key)"
                                   @change="toggleObject(item.key, ($event.target as HTMLInputElement).checked)" />
                            <span class="timeline-color" :style="{ backgroundColor: item.color }"></span>
                            <span>{{ item.label }}</span>
                        </label>
                    </div>
                </div>

                <div class="timeline-right">
                    <div v-for="item in timelineObjects"
                         :key="item.key"
                         class="timeline-row">
                        <div v-for="moment in timelineMoments(item.key)"
                             :key="`${item.key}-${moment}`"
                             class="timeline-marker"
                             :style="{
                left: `${Math.min(100, Math.max(0, moment * 10))}%`,
                backgroundColor: item.color
              }"></div>
                    </div>
                </div>
            </div>
        </section>
    </div>
</template>

<style scoped>
    .video-editor {
        display: flex;
        flex-direction: column;
        gap: 16px;
    }

    .top-layout {
        display: grid;
        grid-template-columns: minmax(0, 1fr) 320px;
        gap: 16px;
        align-items: start;
    }

    .video-stage {
        position: relative;
        width: 100%;
        background: #111827;
        border-radius: 12px;
        overflow: hidden;
    }

    video {
        display: block;
        width: 100%;
        height: auto;
        min-height: 320px;
        background: black;
    }

    .overlay {
        position: absolute;
        inset: 0;
        pointer-events: none;
    }

    .bbox {
        position: absolute;
        border: 2px solid;
        box-sizing: border-box;
    }

    .bbox-label {
        position: absolute;
        top: -24px;
        left: 0;
        padding: 2px 6px;
        font-size: 12px;
        color: white;
        border-radius: 6px;
    }

    .object-panel,
    .timeline-panel {
        border: 1px solid #d1d5db;
        border-radius: 12px;
        padding: 12px;
        background: white;
    }

    .object-row {
        padding: 6px 0;
    }

    .timeline-grid {
        display: grid;
        grid-template-columns: 260px minmax(0, 1fr);
        gap: 12px;
    }

    .timeline-object {
        padding: 8px;
        border-left: 6px solid transparent;
    }

    .timeline-object-label {
        display: flex;
        align-items: center;
        gap: 8px;
    }

    .timeline-color {
        display: inline-block;
        width: 12px;
        height: 12px;
        border-radius: 999px;
    }

    .timeline-right {
        display: flex;
        flex-direction: column;
        gap: 8px;
    }

    .timeline-row {
        position: relative;
        height: 34px;
        border-radius: 8px;
        background: #f3f4f6;
        overflow: hidden;
    }

    .timeline-marker {
        position: absolute;
        top: 4px;
        width: 10px;
        height: 26px;
        border-radius: 999px;
        transform: translateX(-50%);
    }

    @media (max-width: 900px) {
        .top-layout,
        .timeline-grid {
            grid-template-columns: 1fr;
        }
    }
</style>
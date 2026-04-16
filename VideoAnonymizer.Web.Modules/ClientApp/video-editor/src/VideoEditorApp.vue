<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import VideoPlayer from './VideoPlayer.vue';
import ObjectList from './ObjectList.vue';
import Timeline from './Timeline.vue';
import TimelineRow from './TimelineRow.vue';
import BoundingBoxOverlay from './BoundingBoxOverlay.vue';
import type { VideoEditorProps, TimelineObject, DetectedObjectDto } from './types';
import { colorManager } from './services/ColorManager'
import TimelineRowLabel from './TimelineRowLabel.vue';

const props = defineProps<{
    state: VideoEditorProps;
}>();

const currentTime = ref(0);
const videoDuration = ref(0);
const activeKeys = ref<string[]>([]);

const frames = computed(() => props.state.frames ?? []);

function buildObjectKey(obj: DetectedObjectDto): string {
    return obj.trackId != null ? `track-${obj.trackId}` : obj.id;
}

watch(frames, () => {
    const selectedFromData = new Set<string>();
    for (const frame of frames.value) {
        for (const obj of frame.detectedObjects) {
            if (obj.selected) {
                selectedFromData.add(buildObjectKey(obj));
            }
        }
    }
    activeKeys.value = selectedFromData.size > 0
        ? Array.from(selectedFromData)
        : [];
}, { immediate: true });

const timelineObjects = computed<TimelineObject[]>(() => {
    const map = new Map<string, TimelineObject>();
    for (const frame of frames.value) {
        for (const obj of frame.detectedObjects) {
            const key = buildObjectKey(obj);
            if (!map.has(key)) {
                map.set(key, {
                    key,
                    label: obj.trackId != null ? `Object ${obj.trackId}` : (obj.className ?? 'Object'),
                    color: colorManager.getColor(obj),
                    trackId: obj.trackId
                });
            }
        }
    }
    return [...map.values()];
});

const currentFrame = computed(() => {
    if (frames.value.length === 0) return null;
    return [...frames.value]
        .sort((a, b) => Math.abs(a.timeSeconds - currentTime.value) - Math.abs(b.timeSeconds - currentTime.value))[0];
});

const visibleCurrentFrameObjects = computed(() => {
    const frame = currentFrame.value;
    if (!frame) return [];
    return frame.detectedObjects.filter(obj => activeKeys.value.includes(buildObjectKey(obj)));
});

function toggleObject(objectKey: string, checked: boolean) {
    if (checked) {
        if (!activeKeys.value.includes(objectKey)) activeKeys.value = [...activeKeys.value, objectKey];
    } else {
        activeKeys.value = activeKeys.value.filter(k => k !== objectKey);
    }
}

function getKey(obj: DetectedObjectDto): string {
    return buildObjectKey(obj);
}

function buildSegments(objectKey: string) {
    return frames.value
        .filter(frame => frame.detectedObjects.some(obj => buildObjectKey(obj) === objectKey))
        .map(frame => {
            const obj = frame.detectedObjects.find(o => buildObjectKey(o) === objectKey)!;
            return {
                timeSeconds: frame.timeSeconds,
                selected: activeKeys.value.includes(buildObjectKey(obj))
            };
        });
}

function seekTo(time: number) {
    currentTime.value = time;
}

function onTimeUpdate(time: number) {
    currentTime.value = time;
}

function onVideoLoaded(duration: number) {
    if (duration && duration > 0) videoDuration.value = duration;
}
</script>

<template>
    <div class="video-editor" data-testid="video-editor">
        <div class="top-layout">
            <div class="video-stage">
                <VideoPlayer :videoSourceUrl="state.videoSourceUrl" :currentTime="currentTime"
                    @time-update="onTimeUpdate" @loaded="onVideoLoaded" />
                <BoundingBoxOverlay v-if="currentFrame && visibleCurrentFrameObjects.length > 0"
                    :objects="visibleCurrentFrameObjects" :getKey="getKey" />
            </div>

            <ObjectList data-testid="object-list" :objects="currentFrame?.detectedObjects ?? []"
                @toggle="toggleObject" />
        </div>

        <div class="timeline-wrapper">
            <div class="timeline-labels">
                <TimelineRowLabel v-for="obj in timelineObjects" :objectKey="obj.key" :color="obj.color" />
            </div>
            <div>
                <Timeline :duration="videoDuration" :currentTime="currentTime" @seek="seekTo">
                    <TimelineRow v-for="obj in timelineObjects" :key="obj.key" :objectKey="obj.key" :color="obj.color"
                        :duration="videoDuration" :segments="buildSegments(obj.key)" />
                </Timeline>
            </div>
        </div>
    </div>
</template>

<style scoped>
.video-editor {
    background: var(--mud-palette-background);
    color: var(--mud-palette-text-primary);
    display: flex;
    flex-direction: column;
    gap: 20px;
}

.top-layout {
    display: grid;
    grid-template-columns: 2fr 1fr;
    gap: 16px;
}

.video-stage {
    position: relative;
    width: 100%;
    border-radius: 12px;
    overflow: hidden;
}

.timeline-wrapper {
    display: grid;
    grid-template-columns: 170px 1fr;
}

.timeline-labels {
    padding: 16px;
}
</style>
<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import VideoPlayer from './VideoPlayer.vue';
import ObjectList from './ObjectList.vue';
import Timeline from './Timeline.vue';
import TimelineRow from './TimelineRow.vue';
import BoundingBoxOverlay from './BoundingBoxOverlay.vue';
import type { VideoEditorProps, TimelineObject, SingleTimelineObject, TrackedTimelineObject, DetectedObjectDto } from './types';
import { colorManager } from './services/ColorManager'
import TimelineRowLabel from './TimelineRowLabel.vue';

const props = defineProps<{
    state: VideoEditorProps;
}>();

const currentTime = ref(0);
const videoDuration = ref(0);

const frames = computed(() => props.state.frames ?? []);

function buildObjectKey(obj: DetectedObjectDto): string {
    return obj.trackId != null ? `track-${obj.trackId}` : obj.id;
}

function createLabel(obj: DetectedObjectDto): string {
    return obj.trackId != null ? `Object ${obj.trackId}` : (obj.className ?? 'Object');
}

const timelineObjects = computed<TimelineObject[]>(() => {
    var untracked = frames.value.flatMap(frame => frame.detectedObjects.filter(x => x.trackId == null).map((obj) : SingleTimelineObject => ({
        detectedObj: obj,
        type: 'single',
        timeSeconds: frame.timeSeconds,
    })))

    const grouped = frames.value.flatMap(frame =>
        frame.detectedObjects.map(obj => ({
            trackId: obj.trackId,
            detectedObj: obj,
            timeSeconds: frame.timeSeconds
        }))
    ).reduce((acc, x) => {
        if (x.trackId == null) return acc;

        if (!acc[x.trackId]) {
            acc[x.trackId] = {
                type: 'tracked',
                occurences: []
            };
        }

        acc[x.trackId].occurences.push([x.timeSeconds, x.detectedObj]);
        return acc;
    }, {} as Record<number, TrackedTimelineObject>);

    const tracked = Object.values(grouped);

    return [...tracked, ...untracked];
});

const currentFrame = computed(() => {
    if (frames.value.length === 0) return null;
    return [...frames.value]
        .sort((a, b) => Math.abs(a.timeSeconds - currentTime.value) - Math.abs(b.timeSeconds - currentTime.value))[0];
});

const visibleCurrentFrameObjects = computed(() => {
    const frame = currentFrame.value;
    if (!frame) return [];
    return frame.detectedObjects.filter(obj => obj.selected);
});

function toggleObject(id: string, checked: boolean) {
    frames.value.flatMap(x => x.detectedObjects.filter(y => y.id === id)).forEach(obj => {
        obj.selected = checked
    })
}

function toggleTrackedObject(obj: TimelineObject, checked: boolean) {
    if (obj.type === 'single') {
        obj.detectedObj.selected = checked;
        return;
    }

    if (obj.type === 'tracked') {
        obj.occurences.forEach(([_, o]) => {
            o.selected = checked;
        });
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
                selected: obj.selected
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
                <TimelineRowLabel v-for="obj in timelineObjects" :timeline-object="obj" @toggle="toggleTrackedObject"/>
            </div>
            <div>
                <Timeline :duration="videoDuration" :currentTime="currentTime" @seek="seekTo">
                    <TimelineRow v-for="obj in timelineObjects" :timeline-object="obj"
                        :video-duration="videoDuration"/>
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
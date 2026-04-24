<script setup lang="ts">
import { computed, ref } from 'vue';
import VideoPlayer from './VideoPlayer.vue';
import ObjectList from './ObjectList.vue';
import Timeline from './Timeline.vue';
import TimelineRow from './TimelineRow.vue';
import BoundingBoxOverlay from './BoundingBoxOverlay.vue';
import type { VideoEditorProps, TimelineObject, SingleTimelineObject, TrackedTimelineObject, DetectedObjectDto, PreviewObject } from './types';
import TimelineRowLabel from './TimelineRowLabel.vue';

const props = defineProps<{
    state: VideoEditorProps;
}>();
defineExpose({
    getFrames
})

const currentTime = ref(0);
const videoDuration = ref(0);

const frames = computed(() => props.state.frames ?? []);

function getFrames() {
    return JSON.parse(JSON.stringify(props.state.frames))
}

function buildObjectKey(obj: DetectedObjectDto): string {
    return obj.trackId != null ? `track-${obj.trackId}` : obj.id;
}

const timelineObjects = computed<TimelineObject[]>(() => {
    var untracked = frames.value.flatMap(frame => frame.detectedObjects.filter(x => x.trackId == null).map((obj): SingleTimelineObject => ({
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

const orderedCurrentFrameObjects = computed(() => {
    const frame = currentFrame.value;
    if (!frame) return [];

    const orderMap = new Map(
        timelineObjects.value.map((obj, index) => {
            if (obj.type === 'tracked') {
                const first = obj.occurences[0]?.[1];
                return [first?.trackId != null ? `track-${first.trackId}` : first?.id, index];
            }

            return [obj.detectedObj.id, index];
        })
    );

    return [...frame.detectedObjects].sort((a, b) => {
        const aOrder = orderMap.get(buildObjectKey(a)) ?? Number.MAX_SAFE_INTEGER;
        const bOrder = orderMap.get(buildObjectKey(b)) ?? Number.MAX_SAFE_INTEGER;
        return aOrder - bOrder;
    });
});

const visibleBlurPreviewObjects = computed(() => {
    const bufferSeconds = props.state.anonymizationSettings.timeBufferMs / 1000;

    const result: PreviewObject[] = [];
    const seen = new Set<string>();

    const current = currentFrame.value;

    if (!current) {
        return [];
    }

    for (const obj of current.detectedObjects) {
        const key = buildObjectKey(obj);
        if (!obj.selected) continue;
        seen.add(key);
        result.push({ detectedObject: obj, activation: 'detected' });
    }
    for (const frame of props.state.frames) {
        const delta = current.timeSeconds - frame.timeSeconds;

        if (Math.abs(delta) > bufferSeconds) continue;

        for (const obj of frame.detectedObjects) {
            if (!obj.selected) continue;

            const key = buildObjectKey(obj);
            if (seen.has(key)) continue;

            seen.add(key);

            let activation: PreviewObject['activation'];
            if (delta < 0) {
                activation = 'pre';
            } else {
                activation = 'post';
            }

            result.push({ detectedObject: obj, activation });
        }
    }

    return result;
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

function seekTo(time: number) {
    currentTime.value = time;
}

function onTimeUpdate(time: number) {
    currentTime.value = time;
}

function onVideoLoaded(duration: number) {
    if (duration && duration > 0) videoDuration.value = duration;
}
const currentTimePercent = computed(() => {
    if (!videoDuration || videoDuration.value <= 0) {
        return '0%';
    }

    return `${(currentTime.value / videoDuration.value) * 100}%`;
});

const formattedCurrentTime = computed(() => {
    return `${currentTime.value.toFixed(1)}s`;
});
</script>

<template>
    <div class="video-editor" data-testid="video-editor">
        <div class="top-layout">
            <div class="video-stage">
                <VideoPlayer :videoSourceUrl="state.videoSourceUrl" :currentTime="currentTime"
                    @time-update="onTimeUpdate" @loaded="onVideoLoaded" />
                <BoundingBoxOverlay v-if="currentFrame && visibleCurrentFrameObjects.length > 0"
                    :objects="visibleBlurPreviewObjects" :anonymization-settings="state.anonymizationSettings" />
            </div>

            <ObjectList data-testid="object-list" :objects="orderedCurrentFrameObjects" @toggle="toggleObject" />
        </div>

        <div class="timeline-wrapper">
            <div class="timeline-labels">
                <div class="timeline-header-spacer"></div>
                <TimelineRowLabel v-for="obj in timelineObjects" :timeline-object="obj" @toggle="toggleTrackedObject" />
            </div>
            <div>
                <Timeline :duration="videoDuration" :currentTime="currentTime" @seek="seekTo">
                    <div class="timeline-time-row">
                        <div class="timeline-current-time-marker" :style="{ left: currentTimePercent }">
                            {{ formattedCurrentTime }}
                        </div>
                    </div>
                    <TimelineRow v-for="obj in timelineObjects" :timeline-object="obj"
                        :video-duration="videoDuration" />
                </Timeline>
            </div>
        </div>
    </div>
</template>

<style scoped>
.video-editor {
    background: var(--mud-palette-surface);
    color: var(--mud-palette-text-primary);
    display: flex;
    flex-direction: column;
    height: 100%;
    min-height: 0;
    overflow: hidden;
    gap: 16px;
}

.top-layout {
    display: grid;
    grid-template-columns: max-content minmax(0, 1fr);
    gap: 16px;
    align-items: start;
    flex: 0 0 auto;
}

.video-stage {
    position: relative;
    display: inline-block;
    justify-self: start;
    align-self: start;
    line-height: 0;
    overflow: hidden;
}

.timeline-wrapper {
    display: grid;
    grid-template-columns: 170px 1fr;
    flex: 1 1 auto;
    min-height: 0;
    overflow-y: auto;
    overflow-x: hidden;
}

.timeline-labels {
    padding: 16px;
}

.timeline-content {
    min-width: 0;
}

.timeline-header-spacer {
    position: sticky;
    top: 0;
    z-index: 20;
    background: var(--mud-palette-surface);
    height: 34px;
    margin-bottom: 12px;
    isolation: isolate;
}

.timeline-time-row {
    position: sticky;
    top: 0;
    z-index: 100;
    background: var(--mud-palette-surface);
    height: 34px;
    border-bottom: 1px solid var(--mud-palette-lines-default);
    margin-bottom: 12px;
    overflow: visible;
}

.timeline-current-time-marker {
    position: absolute;
    top: 50%;
    transform: translate(-50%, -50%);
    display: inline-block;
    z-index: 100;
    white-space: nowrap;
    font-size: 12px;
    line-height: 1;
    pointer-events: none;
    color: var(--mud-palette-text-primary);
    background: var(--mud-palette-surface);
    padding: 2px 8px;
    border-radius: 4px;
}

.timeline-current-time-marker::before {
    content: "";
    z-index: 100;
    position: absolute;
    inset: -2px -6px;
    background: var(--mud-palette-surface);
    border-radius: 4px;
    z-index: -1;
}
</style>
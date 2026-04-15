<script setup lang="ts">
    import { computed, ref, watch } from 'vue';
    import VideoPlayer from './VideoPlayer.vue';
    import ObjectList from './ObjectList.vue';
    import Timeline from './Timeline.vue';
    import TimelineRow from './TimelineRow.vue';
    import BoundingBoxOverlay from './BoundingBoxOverlay.vue';
    import type { VideoEditorProps, TimelineObject, DetectedObjectDto } from './types';

    const props = defineProps<{
        state: VideoEditorProps;
    }>();

    const currentTime = ref(0);
    const videoDuration = ref(6);
    const activeKeys = ref<string[]>([]);

    const frames = computed(() => props.state.frames ?? []);

    // ── Object key helper ──
    function buildObjectKey(obj: DetectedObjectDto): string {
        return obj.trackId != null ? `track-${obj.trackId}` : obj.id;
    }

    // ── Selection logic (keeps state across frames) ──
    watch(frames, () => {
        const selectedFromData = new Set<string>();
        for (const frame of frames.value) {
            for (const obj of frame.detectedObjects) {
                if (obj.selected) selectedFromData.add(buildObjectKey(obj));
            }
        }
        activeKeys.value = selectedFromData.size > 0
            ? Array.from(selectedFromData)
            : timelineObjects.value.map(x => x.key);
    }, { immediate: true });

    const timelineObjects = computed<TimelineObject[]>(() => {
        const map = new Map<string, TimelineObject>();
        for (const frame of frames.value) {
            for (const obj of frame.detectedObjects) {
                const key = buildObjectKey(obj);
                if (!map.has(key)) {
                    map.set(key, {
                        key,
                        label: obj.trackId != null ? `Object ${obj.trackId}` : obj.className ?? 'Object',
                        color: colorForKey(key),
                        trackId: obj.trackId
                    });
                }
            }
        }
        return [...map.values()];
    });

    function colorForKey(key: string): string {
        const colors = ['#3b82f6', '#ef4444', '#10b981', '#f59e0b', '#8b5cf6', '#ec4899'];
        let hash = 0;
        for (let i = 0; i < key.length; i++) {
            hash = (hash << 5) - hash + key.charCodeAt(i);
            hash |= 0;
        }
        return colors[Math.abs(hash) % colors.length];
    }

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
            if (!activeKeys.value.includes(objectKey)) {
                activeKeys.value = [...activeKeys.value, objectKey];
            }
        } else {
            activeKeys.value = activeKeys.value.filter(k => k !== objectKey);
        }
    }

    function getKey(obj: DetectedObjectDto): string {
        return buildObjectKey(obj);
    }

    function getColor(key: string): string {
        return colorForKey(key);
    }

    function buildSegments(objectKey: string) {
        return frames.value.flatMap(frame =>
            frame.detectedObjects
                .filter(obj => buildObjectKey(obj) === objectKey)
                .map(obj => ({
                    timeSeconds: frame.timeSeconds,
                    selected: activeKeys.value.includes(buildObjectKey(obj))
                }))
        );
    }

    function seekTo(time: number) {
        currentTime.value = Math.max(0, Math.min(videoDuration.value, time));
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
            <!-- FIXED: Proper container for video + overlay -->
            <div class="video-stage">
                <VideoPlayer :videoSourceUrl="state.videoSourceUrl"
                             :currentTime="currentTime"
                             @time-update="onTimeUpdate"
                             @loaded="onVideoLoaded" />

                <!-- Overlay must be INSIDE the same relative container as the video -->
                <BoundingBoxOverlay v-if="currentFrame && visibleCurrentFrameObjects.length"
                                    :objects="visibleCurrentFrameObjects"
                                    :getColor="getColor"
                                    :getKey="getKey" />
            </div>

            <ObjectList data-testid="object-list"
                        :objects="currentFrame?.detectedObjects ?? []"
                        @toggle="toggleObject" />
        </div>

        <!-- Timeline section -->
        <div class="timeline-panel">
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

                <Timeline data-testid="timeline"
                          :duration="videoDuration"
                          :currentTime="currentTime"
                          @seek="seekTo">
                    <TimelineRow v-for="obj in timelineObjects"
                                 :key="obj.key"
                                 :objectKey="obj.key"
                                 :color="obj.color"
                                 :duration="videoDuration"
                                 :segments="buildSegments(obj.key)" />
                </Timeline>
            </div>
        </div>
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
        grid-template-columns: 2fr 1fr;
        gap: 16px;
        align-items: start;
    }

    /* === IMPORTANT: This makes the overlay work correctly === */
    .video-stage {
        position: relative;
        width: 100%;
        background: #111827;
        border-radius: 12px;
        overflow: hidden;
    }

    .timeline-panel {
        border: 1px solid #d1d5db;
        border-radius: 12px;
        padding: 12px;
        background: white;
    }

    .timeline-grid {
        display: grid;
        grid-template-columns: 260px minmax(0, 1fr);
        gap: 12px;
    }

    .timeline-left {
        display: flex;
        flex-direction: column;
        gap: 4px;
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
        border-radius: 9999px;
    }
</style>
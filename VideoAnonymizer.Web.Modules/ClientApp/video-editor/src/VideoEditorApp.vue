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

    const videoRef = ref<HTMLVideoElement | null>(null);
    const timelineTrackRef = ref<HTMLElement | null>(null);
    const currentTime = ref(0);
    const videoDuration = ref(10);

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

    type FrameSelectionMap = Record<string, Record<string, boolean>>;
    type TimelineSelectionMap = Record<string, boolean>;

    const frameSelections = ref<FrameSelectionMap>({});
    const timelineSelections = ref<TimelineSelectionMap>({});

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
        () => props.state.frames,
        () => {
            const nextFrameSelections: FrameSelectionMap = {};
            const nextTimelineSelections: TimelineSelectionMap = {};

            for (const frame of orderedFrames.value) {
                nextFrameSelections[frame.id] = {};

                for (const obj of frame.detectedObjects ?? []) {
                    const key = buildObjectKey(obj);
                    nextFrameSelections[frame.id][key] = obj.selected ?? true;

                    if (!(key in nextTimelineSelections)) {
                        nextTimelineSelections[key] = true;
                    }
                }
            }

            frameSelections.value = nextFrameSelections;
            timelineSelections.value = nextTimelineSelections;
        },
        { immediate: true, deep: true }
    );

    const currentFrame = computed<AnalyzedFrameDto | null>(() => {
        if (orderedFrames.value.length === 0) {
            return null;
        }

        let selected = orderedFrames.value[0];

        for (const frame of orderedFrames.value) {
            if (frame.timeSeconds <= currentTime.value + 0.0001) {
                selected = frame;
            } else {
                break;
            }
        }

        return selected;
    });

    function isTimelineObjectActive(objectKey: string): boolean {
        return timelineSelections.value[objectKey] ?? true;
    }

    function isFrameObjectActive(frameId: string, objectKey: string): boolean {
        return frameSelections.value[frameId]?.[objectKey] ?? true;
    }

    function isObjectVisibleInFrame(frameId: string, objectKey: string): boolean {
        return isTimelineObjectActive(objectKey) && isFrameObjectActive(frameId, objectKey);
    }

    const currentFrameObjects = computed(() => {
        const frame = currentFrame.value;
        if (!frame) return [];

        return (frame.detectedObjects ?? []).filter(obj =>
            isObjectVisibleInFrame(frame.id, buildObjectKey(obj))
        );
    });

    function setFrameObjectSelected(frameId: string, objectKey: string, checked: boolean) {
        frameSelections.value = {
            ...frameSelections.value,
            [frameId]: {
                ...(frameSelections.value[frameId] ?? {}),
                [objectKey]: checked
            }
        };
    }

    function setTimelineObjectSelected(objectKey: string, checked: boolean) {
        timelineSelections.value = {
            ...timelineSelections.value,
            [objectKey]: checked
        };
    }

    function colorForKey(objectKey: string): string {
        return timelineObjects.value.find(x => x.key === objectKey)?.color ?? '#3b82f6';
    }

    function onVideoTimeUpdate(event: Event) {
        const video = event.target as HTMLVideoElement;
        currentTime.value = video.currentTime;
    }

    function onLoadedMetadata(event: Event) {
        const video = event.target as HTMLVideoElement;
        if (Number.isFinite(video.duration) && video.duration > 0) {
            videoDuration.value = video.duration;
        } else if (orderedFrames.value.length > 0) {
            videoDuration.value = Math.max(
                orderedFrames.value[orderedFrames.value.length - 1].timeSeconds,
                1
            );
        }
    }

    function seekTo(seconds: number) {
        const clamped = Math.max(0, Math.min(videoDuration.value, seconds));
        currentTime.value = clamped;

        if (videoRef.value) {
            videoRef.value.currentTime = clamped;
            videoRef.value.dispatchEvent(new Event('timeupdate'));
            videoRef.value.dispatchEvent(new Event('seeked'));
        }
    }

    function onTimelineClick(event: MouseEvent) {
        const track = timelineTrackRef.value;
        if (!track) return;

        const rect = track.getBoundingClientRect();
        if (rect.width <= 0) return;

        const ratio = Math.max(0, Math.min(1, (event.clientX - rect.left) / rect.width));
        seekTo(ratio * videoDuration.value);
    }

    function timelineMoments(objectKey: string): number[] {
        return orderedFrames.value
            .filter(frame => (frame.detectedObjects ?? []).some(obj => buildObjectKey(obj) === objectKey))
            .map(frame => frame.timeSeconds);
    }

    function timelineSegments(objectKey: string) {
        return orderedFrames.value
            .filter(frame => (frame.detectedObjects ?? []).some(obj => buildObjectKey(obj) === objectKey))
            .map(frame => ({
                frameId: frame.id,
                timeSeconds: frame.timeSeconds,
                active: isTimelineObjectActive(objectKey) && isFrameObjectActive(frame.id, objectKey)
            }));
    }

    function hasContinuousRange(objectKey: string): boolean {
        const moments = timelineMoments(objectKey);
        if (moments.length < 2) return false;

        for (let i = 1; i < moments.length; i++) {
            if (Math.abs(moments[i] - moments[i - 1]) <= 1.1) {
                return true;
            }
        }

        return false;
    }

    function percentForTime(timeSeconds: number): number {
        if (videoDuration.value <= 0) return 0;
        return Math.max(0, Math.min(100, (timeSeconds / videoDuration.value) * 100));
    }

    const playbackIndicatorLeft = computed(() => `${percentForTime(currentTime.value)}%`);

    const playbackTimestamp = computed(() => `${currentTime.value.toFixed(1)}s`);
</script>

<template>
    <div class="video-editor">
        <div class="top-layout">
            <div class="preview-panel">
                <div class="video-stage">
                    <video ref="videoRef"
                           :src="state.videoSourceUrl"
                           controls
                           @timeupdate="onVideoTimeUpdate"
                           @loadedmetadata="onLoadedMetadata"></video>

                    <div class="overlay">
                        <div v-for="item in currentFrameObjects"
                             :key="item.id"
                             class="bbox"
                             data-testid="bounding-box"
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
                               :checked="currentFrame ? isFrameObjectActive(currentFrame.id, buildObjectKey(item)) : false"
                               @change="
                currentFrame &&
                  setFrameObjectSelected(
                    currentFrame.id,
                    buildObjectKey(item),
                    ($event.target as HTMLInputElement).checked
                  )
              " />
                        {{ buildObjectLabel(item) }}
                    </label>
                </div>
            </aside>
        </div>

        <section class="timeline-panel">
            <div class="timeline-grid">
                <div class="timeline-left" data-testid="timeline-object-list">
                    <div v-for="item in timelineObjects"
                         :key="item.key"
                         class="timeline-object"
                         data-testid="timeline-object"
                         :style="{ backgroundColor: colorForKey(item.key) }">
                        <label class="timeline-object-label">
                            <input type="checkbox"
                                   :checked="isTimelineObjectActive(item.key)"
                                   @change="setTimelineObjectSelected(item.key, ($event.target as HTMLInputElement).checked)" />
                            <span class="timeline-color" :style="{ backgroundColor: item.color }"></span>
                            <span>{{ item.label }}</span>
                        </label>
                    </div>
                </div>

                <div ref="timelineTrackRef"
                     class="timeline-right"
                     data-testid="timeline"
                     @click="onTimelineClick">
                    <div class="playback-indicator"
                         data-testid="playback-indicator"
                         :style="{ left: playbackIndicatorLeft }">
                        <div class="playback-line"></div>
                        <div class="playback-timestamp" data-testid="playback-timestamp">
                            {{ playbackTimestamp }}
                        </div>
                    </div>

                    <div v-for="item in timelineObjects"
                         :key="item.key"
                         class="timeline-row">
                        <div v-if="hasContinuousRange(item.key)"
                             class="timeline-segment-continuous"
                             data-testid="timeline-segment-continuous"
                             :style="{ backgroundColor: colorForKey(item.key) }"></div>

                        <div v-for="segment in timelineSegments(item.key)"
                             :key="`${item.key}-${segment.frameId}`"
                             class="timeline-marker"
                             :data-testid="segment.active ? 'timeline-segment-colored' : 'timeline-segment-gray'"
                             :style="{
                left: `${percentForTime(segment.timeSeconds)}%`,
                backgroundColor: segment.active ? colorForKey(item.key) : '#9ca3af'
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
        white-space: nowrap;
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
        border-radius: 8px;
        margin-bottom: 8px;
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
        border: 1px solid rgba(0, 0, 0, 0.15);
    }

    .timeline-right {
        position: relative;
        display: flex;
        flex-direction: column;
        gap: 8px;
        cursor: pointer;
        user-select: none;
    }

    .timeline-row {
        position: relative;
        height: 34px;
        border-radius: 8px;
        background: #f3f4f6;
        overflow: hidden;
    }

    .timeline-segment-continuous {
        position: absolute;
        inset: 10px 0;
        opacity: 0.18;
        border-radius: 999px;
    }

    .timeline-marker {
        position: absolute;
        top: 4px;
        width: 10px;
        height: 26px;
        border-radius: 999px;
        transform: translateX(-50%);
    }

    .playback-indicator {
        position: absolute;
        top: 0;
        bottom: 0;
        width: 0;
        z-index: 5;
        pointer-events: none;
    }

    .playback-line {
        position: absolute;
        top: 0;
        bottom: 0;
        left: 0;
        width: 2px;
        background: #111827;
        transform: translateX(-50%);
    }

    .playback-timestamp {
        position: absolute;
        top: -24px;
        left: 0;
        transform: translateX(-50%);
        padding: 2px 6px;
        border-radius: 6px;
        background: #111827;
        color: white;
        font-size: 12px;
        white-space: nowrap;
    }

    @media (max-width: 900px) {
        .top-layout,
        .timeline-grid {
            grid-template-columns: 1fr;
        }
    }
</style>
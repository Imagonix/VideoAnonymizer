<script setup lang="ts">
import { computed, ref } from 'vue';
import type { VideoDimensions } from './types';
import type { VideoEditorProps, TimelineObject, SingleTimelineObject, TrackedTimelineObject, DetectedObjectDto, PreviewObject, TimelineObjectCount } from './types';
import { buildObjectKey, getTimelineKey } from './utils/keys';
import { useEditorModes } from './composables/useEditorModes';
import { useMerge } from './composables/useMerge';
import { useOccurrenceSelection } from './composables/useOccurrenceSelection';
import { useSplit } from './composables/useSplit';
import VideoPlayer from './VideoPlayer.vue';
import ObjectList from './ObjectList.vue';
import Timeline from './Timeline.vue';
import TimelineRow from './TimelineRow.vue';
import BoundingBoxOverlay from './BoundingBoxOverlay.vue';
import EditorControls from './EditorControls.vue';
import DetailedView from './DetailedView.vue';
import TimelineRowLabel from './TimelineRowLabel.vue';

const props = defineProps<{ state: VideoEditorProps }>();
defineExpose({ getFrames });

const currentTime = ref(0);
const videoDuration = ref(0);
const isVideoPlaying = ref(false);
const videoVolume = ref(1);
const videoPlayerRef = ref<{
    setVolume: (volume: number) => void;
    togglePlayback: () => Promise<void>;
    videoRef: HTMLVideoElement | null;
    videoDimensions: VideoDimensions | null;
} | null>(null);

const videoDimensions = computed(() => videoPlayerRef.value?.videoDimensions ?? null);
const frames = computed(() => props.state.frames ?? []);
const hoveredTimelineKey = ref<string | null>(null);
const hoveredObjectKey = ref<string | null>(null);

const { activeMode, activate, deactivate, isMerge, isSplit, isMove, isResize, isAdd, isOverlayOpen } = useEditorModes();
const { mergeSelectedKeys: mergeSelectedTimelineKeys, toggle: mergeToggle, execute: mergeExecute } = useMerge();
const { selectedOccurrences, toggle: toggleOccurrence, totalCount, hasAny, hasOnlyTracked, clear: clearOccurrences } = useOccurrenceSelection();
const { splitSourceKey, execute: splitExecute } = useSplit();

function getFrames() {
    return JSON.parse(JSON.stringify(props.state.frames))
}

const timelineObjects = computed<TimelineObject[]>(() => {
    var untracked = frames.value.flatMap(frame => frame.detectedObjects.filter(x => x.trackId == null).map((obj): SingleTimelineObject => ({
        detectedObj: obj, type: 'single', timeSeconds: frame.timeSeconds,
    })))

    const grouped = frames.value.flatMap(frame =>
        frame.detectedObjects.map(obj => ({ trackId: obj.trackId, detectedObj: obj, timeSeconds: frame.timeSeconds }))
    ).reduce((acc, x) => {
        if (x.trackId == null) return acc;
        if (!acc[x.trackId]) { acc[x.trackId] = { type: 'tracked', occurences: [] }; }
        acc[x.trackId].occurences.push([x.timeSeconds, x.detectedObj]);
        return acc;
    }, {} as Record<number, TrackedTimelineObject>);

    return [...Object.values(grouped), ...untracked];
});

const timelineObjectCounts = computed<TimelineObjectCount[]>(() =>
    frames.value.map(frame => ({ timeSeconds: frame.timeSeconds, count: frame.detectedObjects.length }))
        .sort((a, b) => a.timeSeconds - b.timeSeconds)
);

const orderedCurrentFrameObjects = computed(() => {
    const frame = currentFrame.value;
    if (!frame) return [];
    const orderMap = new Map(timelineObjects.value.map((obj, index) => {
        if (obj.type === 'tracked') {
            const first = obj.occurences[0]?.[1];
            return [first?.trackId != null ? `track-${first.trackId}` : first?.id, index];
        }
        return [obj.detectedObj.id, index];
    }));
    return [...frame.detectedObjects].sort((a, b) => {
        const aOrder = orderMap.get(buildObjectKey(a)) ?? Number.MAX_SAFE_INTEGER;
        const bOrder = orderMap.get(buildObjectKey(b)) ?? Number.MAX_SAFE_INTEGER;
        return aOrder - bOrder;
    });
});

const visibleBlurPreviewObjects = computed(() => {
    const bufferSeconds = props.state.anonymizationSettings.timeBufferMs / 1000;
    const result: PreviewObject[] = [];
    const current = currentFrame.value;
    if (!current) return [];

    for (const obj of current.detectedObjects) {
        if (!obj.selected) continue;
        result.push({ detectedObject: obj, activation: 'detected' });
    }
    if (!isMove.value) for (const frame of [...frames.value].sort((a, b) =>
        Math.abs(a.timeSeconds - current.timeSeconds) - Math.abs(b.timeSeconds - current.timeSeconds)
    )) {
        const delta = current.timeSeconds - frame.timeSeconds;
        if (Math.abs(delta) > bufferSeconds) continue;
        for (const obj of frame.detectedObjects) {
            if (!obj.selected) continue;
            const key = buildObjectKey(obj);
            if (result.some(r => buildObjectKey(r.detectedObject) === key)) continue;
            result.push({ detectedObject: obj, activation: delta < 0 ? 'pre' : 'post' });
        }
    }
    return result;
});

const currentFrame = computed(() => {
    if (frames.value.length === 0) return null;
    return [...frames.value].sort((a, b) =>
        Math.abs(a.timeSeconds - currentTime.value) - Math.abs(b.timeSeconds - currentTime.value)
    )[0];
});

function toggleObject(id: string, checked: boolean) {
    frames.value.flatMap(x => x.detectedObjects.filter(y => y.id === id)).forEach(obj => { obj.selected = checked });
}

function toggleTrackedObject(obj: TimelineObject, checked: boolean) {
    if (obj.type === 'single') { obj.detectedObj.selected = checked; return; }
    obj.occurences.forEach(([_, o]) => { o.selected = checked; });
}

function setTrackId(timelineObject: TimelineObject, trackId: number) {
    if (timelineObject.type === 'single') {
        timelineObject.detectedObj.trackId = trackId;
        return;
    }
    const oldTrackId = timelineObject.occurences[0]?.[1].trackId;
    if (oldTrackId == null) return;
    for (const frame of props.state.frames) {
        for (const obj of frame.detectedObjects) {
            if (obj.trackId === oldTrackId) { obj.trackId = trackId; }
        }
    }
}

function mergeAction() {
    mergeExecute(timelineObjects.value, props.state.frames);
    deactivate();
}

function splitAction() {
    const didSplit = splitExecute(selectedOccurrences.value, props.state.frames);
    if (didSplit) {
        clearOccurrences();
        deactivate();
    }
}

function modeToggle(mode: 'merge' | 'split' | 'move' | 'resize' | 'add') {
    if (activeMode.value === mode) { deactivate(); return; }
    if (mode === 'merge' || mode === 'split' || mode === 'move' || mode === 'resize' || mode === 'add') {
        if (mode !== 'merge') clearOccurrences();
        if (mode !== 'split') mergeSelectedTimelineKeys.value = new Set();
        activate(mode);
    }
}

function onTimeUpdate(time: number) { currentTime.value = time; }
function onVideoLoaded(duration: number) {
    if (duration && duration > 0) videoDuration.value = duration;
    videoPlayerRef.value?.setVolume(videoVolume.value);
}
function onVideoPlayStateChange(isPlaying: boolean) { isVideoPlaying.value = isPlaying; }
function onVideoVolumeChange(volume: number) { videoVolume.value = volume; }
function seekTo(time: number) { currentTime.value = time; }
function toggleVideoPlayback() { videoPlayerRef.value?.togglePlayback(); }
function setVideoVolume(volume: number) {
    videoVolume.value = volume;
    videoPlayerRef.value?.setVolume(volume);
}

function addBox(x: number, y: number, width: number, height: number, className: string, trackId: 'new' | number) {
    if (!currentFrame.value) return;
    const frame = currentFrame.value;
    const resolvedTrackId = trackId === 'new'
        ? frames.value.flatMap(f => f.detectedObjects).reduce((max, o) => Math.max(max, o.trackId ?? 0), 0) + 1
        : trackId;
    frame.detectedObjects.push({
        id: crypto.randomUUID(), confidence: 1, className: className || null, selected: true,
        trackId: resolvedTrackId, x, y, width, height, analyzedFrameId: frame.id,
    });
}
</script>

<template>
    <div class="video-editor" data-testid="video-editor">
        <div class="top-layout">
            <div class="video-stage">
                <VideoPlayer ref="videoPlayerRef" :videoSourceUrl="state.videoSourceUrl" :currentTime="currentTime"
                    @time-update="onTimeUpdate" @loaded="onVideoLoaded"
                    @play-state-change="onVideoPlayStateChange" @volume-change="onVideoVolumeChange" />
                <BoundingBoxOverlay v-if="currentFrame && visibleBlurPreviewObjects.length > 0"
                    :objects="visibleBlurPreviewObjects"
                    :anonymization-settings="state.anonymizationSettings"
                    :video-dimensions="videoDimensions"
                    :highlighted-row-key="isMerge ? hoveredTimelineKey : isSplit ? (hoveredTimelineKey ?? splitSourceKey) : hoveredObjectKey"
                    :split-source-key="isSplit ? splitSourceKey : null"
                    :always-show-keys="isMerge && mergeSelectedTimelineKeys.size > 0 ? mergeSelectedTimelineKeys : new Set<string>()" />
            </div>

            <div class="right-panel">
                <ObjectList data-testid="object-list" class="object-list" :objects="orderedCurrentFrameObjects"
                  @toggle="toggleObject"
                  @hover-row="hoveredObjectKey = $event" />
                <EditorControls
                  :move-mode="isMove"
                  :resize-mode="isResize"
                  :add-mode="isAdd"
                  :merge-mode="isMerge"
                  :merge-count="mergeSelectedTimelineKeys.size"
                  :split-mode="isSplit"
                  :can-split="hasOnlyTracked() && hasAny()"
                  :split-count="totalCount()"
                  @toggle-move-mode="modeToggle('move')"
                  @toggle-resize-mode="modeToggle('resize')"
                  @toggle-add-mode="modeToggle('add')"
                  @toggle-merge-mode="modeToggle('merge')"
                  @merge="mergeAction"
                  @toggle-split-mode="modeToggle('split')"
                  @split-out="splitAction"
                />
            </div>
        </div>

        <div class="timeline-wrapper">
            <div class="timeline-labels">
                <div class="timeline-toolbar-spacer"></div>
                <div class="timeline-header-spacer"></div>
                <div class="timeline-overview-spacer"></div>
                <TimelineRowLabel
                  v-for="obj in timelineObjects"
                  :timeline-object="obj"
                  :mode="isMerge ? 'merge' : 'select'"
                  :merge-selected-keys="mergeSelectedTimelineKeys"
                  :hovered-timeline-key="hoveredTimelineKey"
                  @toggle="toggleTrackedObject"
                  @set-track-id="setTrackId"
                  @merge-toggle="mergeToggle"
                  @hover-row="hoveredTimelineKey = $event"
                />
            </div>
            <div class="timeline-content">
                <Timeline :duration="videoDuration" :currentTime="currentTime" :is-playing="isVideoPlaying"
                    :volume="videoVolume" :object-counts="timelineObjectCounts" @seek="seekTo" @toggle-playback="toggleVideoPlayback"
                    @volume-change="setVideoVolume">
                    <TimelineRow v-for="obj in timelineObjects" :timeline-object="obj"
                        :video-duration="videoDuration"
                        :mode="isMerge ? 'merge' : isSplit ? 'split' : 'select'"
                        :merge-selected-keys="mergeSelectedTimelineKeys"
                        :selected-occurrences="selectedOccurrences"
                        :hovered-timeline-key="hoveredTimelineKey"
                        @toggle-occurrence="(k, t, e) => toggleOccurrence(k, t, e, timelineObjects)"
                        @merge-toggle="mergeToggle"
                        @hover-row="hoveredTimelineKey = $event" />
                </Timeline>
            </div>
        </div>
    </div>
    <DetailedView
      v-if="isOverlayOpen && currentFrame"
      :frame="currentFrame"
      :frames="frames"
      :video-ref="videoPlayerRef?.videoRef ?? null"
      :anonymization-settings="state.anonymizationSettings"
      :mode="isAdd ? 'add' : isResize ? 'resize' : 'move'"
      @done="deactivate"
      @mode-change="(m: any) => activate(m)"
      @add-box="addBox"
    />
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
    gap: 0;
}

.top-layout {
    display: grid;
    grid-template-columns: max-content auto;
    gap: 16px;
    align-items: start;
    flex: 0 0 auto;
    padding: 16px;
}

.video-stage {
    position: relative;
    display: inline-block;
    justify-self: start;
    align-self: start;
    line-height: 0;
    overflow: hidden;
}

.right-panel {
    display: flex;
    flex-direction: row;
    gap: 0;
    align-items: flex-start;
}

.object-list {
    width: 120px;
    flex-shrink: 0;
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

.timeline-toolbar-spacer {
    position: sticky;
    top: 0;
    z-index: 20;
    background: var(--mud-palette-surface);
    height: 32px;
    margin-bottom: 12px;
    isolation: isolate;
}

.timeline-header-spacer {
    position: sticky;
    top: 44px;
    z-index: 20;
    background: var(--mud-palette-surface);
    height: 34px;
    margin-bottom: 12px;
    isolation: isolate;
}

.timeline-overview-spacer {
    height: 38px;
    margin-bottom: 9px;
}
</style>

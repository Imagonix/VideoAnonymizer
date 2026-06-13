<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted, watch } from 'vue';
import type { VideoDimensions } from './types';
import type { VideoEditorProps, DetectedObjectChangeSet } from './types';
import { useEditorModes } from './composables/useEditorModes';
import { useMerge } from './composables/useMerge';
import { useOccurrenceSelection } from './composables/useOccurrenceSelection';
import { useSplit } from './composables/useSplit';
import { useKeyboardUndoRedo } from './composables/useKeyboardUndoRedo';
import { useTimelineObjects } from './composables/useTimelineObjects';
import { useBlurPreviewObjects } from './composables/useBlurPreviewObjects';
import { useDetectedObjectActions } from './composables/useDetectedObjectActions';
import VideoPlayer from './VideoPlayer.vue';
import ObjectList from './ObjectList.vue';
import Timeline from './Timeline.vue';
import TimelineRow from './TimelineRow.vue';
import BoundingBoxOverlay from './BoundingBoxOverlay.vue';
import EditorControls from './EditorControls.vue';
import DetailedView from './DetailedView.vue';
import TimelineRowLabel from './TimelineRowLabel.vue';

const props = defineProps<{ state: VideoEditorProps }>();

const currentTime = ref(0);
const videoDuration = ref(0);
const isVideoPlaying = ref(false);
const videoVolume = ref(0);
const videoPlayerRef = ref<{
    setVolume: (volume: number) => void;
    togglePlayback: () => Promise<void>;
    videoRef: HTMLVideoElement | null;
    videoDimensions: VideoDimensions | null;
} | null>(null);

const videoDimensions = computed(() => videoPlayerRef.value?.videoDimensions ?? null);

const topLayoutRef = ref<HTMLElement | null>(null);
const rightPanelRef = ref<HTMLElement | null>(null);
const stageWidth = ref(0);
const stageHeight = ref(0);
const videoNaturalWidth = ref(640);
const videoNaturalHeight = ref(480);

const stageStyle = computed(() => {
    const w = stageWidth.value;
    const h = stageHeight.value;
    return w > 0 && h > 0 ? { width: w + 'px', height: h + 'px' } : undefined;
});

watch(videoDimensions, (dims) => {
    if (dims && dims.videoWidth > 0 && dims.videoHeight > 0) {
        videoNaturalWidth.value = dims.videoWidth;
        videoNaturalHeight.value = dims.videoHeight;
        scheduleStageSizeUpdate();
    }
});

let resizeObserver: ResizeObserver | null = null;

function scheduleStageSizeUpdate() {
    requestAnimationFrame(() => requestAnimationFrame(updateStageSize));
}

function updateStageSize() {
    const top = topLayoutRef.value;
    const right = rightPanelRef.value;
    if (!top || !right) return;

    const topRect = top.getBoundingClientRect();
    const rightRect = right.getBoundingClientRect();

    const gap = 16;
    const paddingY = 32;

    const maxWidth = rightRect.left - topRect.left - gap;
    const maxHeight = topRect.height - paddingY;

    if (maxWidth <= 0 || maxHeight <= 0) return;

    const aspect = videoNaturalWidth.value / videoNaturalHeight.value;
    let w = maxWidth;
    let h = w / aspect;
    if (h > maxHeight) {
        h = maxHeight;
        w = h * aspect;
    }

    stageWidth.value = Math.round(w);
    stageHeight.value = Math.round(h);
}

onMounted(() => {
    scheduleStageSizeUpdate();
    resizeObserver = new ResizeObserver(updateStageSize);
    if (topLayoutRef.value) resizeObserver.observe(topLayoutRef.value);
    if (rightPanelRef.value) resizeObserver.observe(rightPanelRef.value);
});

onUnmounted(() => {
    resizeObserver?.disconnect();
});
const frames = computed(() => props.state.frames ?? []);
const anonymizationSettings = computed(() => props.state.anonymizationSettings);
const hoveredTimelineKey = ref<string | null>(null);
const hoveredObjectKey = ref<string | null>(null);

const { activeMode, activate, deactivate, isMerge, isSplit, isMove, isResize, isAdd, isOverlayOpen } = useEditorModes();
const { mergeSelectedKeys: mergeSelectedTimelineKeys, toggle: mergeToggle, execute: mergeExecute } = useMerge();
const { selectedOccurrences, toggle: toggleOccurrence, totalCount, hasAny, hasOnlyTracked, clear: clearOccurrences } = useOccurrenceSelection();
const { splitSourceKey, execute: splitExecute } = useSplit();
const { currentFrame, timelineObjects, timelineObjectCounts, orderedCurrentFrameObjects } = useTimelineObjects(frames, currentTime);
const visibleBlurPreviewObjects = useBlurPreviewObjects(frames, currentFrame, currentTime, anonymizationSettings, isMove);
const { toggleObject, toggleTrackedObject, setTrackId, deleteObject, trackForward, addBox, onBoxUpdated } = useDetectedObjectActions(
    props.state,
    frames,
    currentFrame,
    activeMode
);

useKeyboardUndoRedo(props.state);

function applyChanges(changes: DetectedObjectChangeSet) {
    for (const obj of changes.objectsToUpdate) {
        const frame = props.state.frames.find(f => f.id === obj.analyzedFrameId);
        if (!frame) continue;
        const existing = frame.detectedObjects.find(o => o.id === obj.id);
        if (existing) Object.assign(existing, obj);
    }
    for (const id of changes.objectsToRemove) {
        for (const frame of props.state.frames) {
            const idx = frame.detectedObjects.findIndex(o => o.id === id);
            if (idx >= 0) frame.detectedObjects.splice(idx, 1);
        }
    }
    for (const obj of changes.objectsToAdd) {
        const frame = props.state.frames.find(f => f.id === obj.analyzedFrameId);
        if (frame) frame.detectedObjects.push(obj);
    }
}

defineExpose({ getFrames, applyChanges });

function getFrames() {
    return JSON.parse(JSON.stringify(props.state.frames))
}

function mergeAction() {
    const { changed, beforeState } = mergeExecute(timelineObjects.value, props.state.frames);
    deactivate();
    if (changed.length > 0) {
        props.state.onDetectedObjectsBulkUpdated?.(props.state.videoId, changed, 'merge', beforeState);
    }
}

function splitAction() {
    const { changed, beforeState } = splitExecute(selectedOccurrences.value, props.state.frames);
    if (changed.length > 0) {
        clearOccurrences();
        deactivate();
        props.state.onDetectedObjectsBulkUpdated?.(props.state.videoId, changed, 'split', beforeState);
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
</script>

<template>
    <div class="video-editor" data-testid="video-editor">
        <div ref="topLayoutRef" class="top-layout">
            <div class="video-stage" :style="stageStyle">
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

            <div ref="rightPanelRef" class="right-panel">
                <ObjectList data-testid="object-list" :objects="orderedCurrentFrameObjects"
                  @toggle="toggleObject"
                  @hover-row="hoveredObjectKey = $event"
                  @delete-object="deleteObject"
                  @track-forward="trackForward" />
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
      @box-updated="onBoxUpdated"
      @track-forward="trackForward"
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
    flex: 1 1 50%;
    max-height: 50%;
    min-height: 0;
    overflow: hidden;
    padding: 16px;
}

.video-stage {
    position: relative;
    display: flex;
    justify-content: center;
    align-items: center;
    line-height: 0;
    overflow: hidden;
}

.right-panel {
    display: flex;
    flex-direction: row;
    gap: 0;
    align-items: flex-start;
}

.timeline-wrapper {
    display: grid;
    grid-template-columns: 170px 1fr;
    flex: 1 1 50%;
    max-height: 50%;
    min-height: 0;
    overflow-y: auto;
    overflow-x: hidden;
}

.timeline-labels {
    padding: 16px;
    background: var(--mud-palette-surface);
}

.timeline-content {
    min-width: 0;
    background: var(--mud-palette-surface);
}

.timeline-toolbar-spacer {
    position: sticky;
    top: 0;
    z-index: 20;
    background: var(--mud-palette-surface);
    height: 48px;
    isolation: isolate;
}

.timeline-header-spacer {
    height: 34px;
    margin-bottom: 12px;
}

.timeline-overview-spacer {
    position: sticky;
    top: 48px;
    z-index: 20;
    background: var(--mud-palette-surface);
    height: 36px;
    margin-bottom: 8px;
    isolation: isolate;
}
</style>

<script setup lang="ts">
import { computed, ref } from 'vue';
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
const videoVolume = ref(1);
const videoPlayerRef = ref<{
    setVolume: (volume: number) => void;
    togglePlayback: () => Promise<void>;
    videoRef: HTMLVideoElement | null;
    videoDimensions: VideoDimensions | null;
} | null>(null);

const videoDimensions = computed(() => videoPlayerRef.value?.videoDimensions ?? null);
const frames = computed(() => props.state.frames ?? []);
const anonymizationSettings = computed(() => props.state.anonymizationSettings);
const hoveredTimelineKey = ref<string | null>(null);
const hoveredObjectKey = ref<string | null>(null);

const { activeMode, activate, deactivate, isMerge, isSplit, isMove, isResize, isAdd, isOverlayOpen } = useEditorModes();
const { mergeSelectedKeys: mergeSelectedTimelineKeys, toggle: mergeToggle, execute: mergeExecute } = useMerge();
const { selectedOccurrences, toggle: toggleOccurrence, totalCount, hasAny, hasOnlyTracked, clear: clearOccurrences } = useOccurrenceSelection();
const { splitSourceKey, execute: splitExecute } = useSplit();
const { currentFrame, timelineObjects, timelineObjectCounts, orderedCurrentFrameObjects } = useTimelineObjects(frames, currentTime);
const visibleBlurPreviewObjects = useBlurPreviewObjects(frames, currentFrame, anonymizationSettings, isMove);
const { toggleObject, toggleTrackedObject, setTrackId, deleteObject, addBox, onBoxUpdated } = useDetectedObjectActions(
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
                  @hover-row="hoveredObjectKey = $event"
                  @delete-object="deleteObject" />
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
    max-width: 1200px;
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
    height: 48px;
    margin-bottom: 12px;
    isolation: isolate;
}

.timeline-header-spacer {
    position: sticky;
    top: 60px;
    z-index: 20;
    background: var(--mud-palette-surface);
    height: 34px;
    margin-bottom: 12px;
    isolation: isolate;
}

.timeline-overview-spacer {
    height: 36px;
    margin-bottom: 8px;
}
</style>

<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted, watch, nextTick } from 'vue';
import type { TimelineObject, VideoDimensions, DetectedObjectDto } from './types';
import type { VideoEditorProps, DetectedObjectChangeSet } from './types';
import { useEditorModes } from './composables/useEditorModes';
import { useMerge } from './composables/useMerge';
import { useOccurrenceSelection } from './composables/useOccurrenceSelection';
import { useSplit } from './composables/useSplit';
import { useKeyboardUndoRedo } from './composables/useKeyboardUndoRedo';
import { useTimelineObjects } from './composables/useTimelineObjects';
import { useBlurPreviewObjects } from './composables/useBlurPreviewObjects';
import { useDetectedObjectActions } from './composables/useDetectedObjectActions';
import { useConsecutiveTrackSegment, getTrackOccurrences } from './composables/useConsecutiveTrackSegment';
import { useTrackSettings } from './composables/useTrackSettings';
import { getTimelineKey, getObjTimelineKey } from './utils/keys';
import { getLabel } from './utils/utils';
import { computeVideoFrameSize, centeredRect, computeInspectorPlacement } from './utils/videoLayout';
import VideoPlayer from './VideoPlayer.vue';
import Timeline from './Timeline.vue';
import TimelineRow from './TimelineRow.vue';
import BoundingBoxOverlay from './BoundingBoxOverlay.vue';
import TimelineRowLabel from './TimelineRowLabel.vue';
import ObjectDetailsPanel from './ObjectDetailsPanel.vue';
import ReviewTools from './ReviewTools.vue';
import AddBoxDialog from './AddBoxDialog.vue';
import CollapsedTimelineBar from './CollapsedTimelineBar.vue';
import PlayCircleOutlineIcon from './icons/PlayCircleOutlineIcon.vue';
import PauseCircleOutlineIcon from './icons/PauseCircleOutlineIcon.vue';

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

const workspaceRef = ref<HTMLElement | null>(null);
const workspaceSize = ref({ width: 0, height: 0 });
const videoFrameSize = ref({ width: 0, height: 0 });
const videoNaturalWidth = ref(640);
const videoNaturalHeight = ref(480);

const videoRect = computed(() => centeredRect(workspaceSize.value.width, workspaceSize.value.height, videoFrameSize.value));

const videoFrameStyle = computed(() => {
    const { width, height } = videoFrameSize.value;
    return width > 0 && height > 0 ? { width: width + 'px', height: height + 'px' } : undefined;
});

watch(videoDimensions, (dims) => {
    if (dims && dims.videoWidth > 0 && dims.videoHeight > 0) {
        videoNaturalWidth.value = dims.videoWidth;
        videoNaturalHeight.value = dims.videoHeight;
        scheduleWorkspaceSizeUpdate();
    }
});

let resizeObserver: ResizeObserver | null = null;

function scheduleWorkspaceSizeUpdate() {
    requestAnimationFrame(() => requestAnimationFrame(updateWorkspaceSize));
}

function updateWorkspaceSize() {
    const el = workspaceRef.value;
    if (!el) return;

    const width = el.clientWidth;
    const height = el.clientHeight;
    if (width <= 0 || height <= 0) return;

    workspaceSize.value = { width, height };
    videoFrameSize.value = computeVideoFrameSize({
        containerWidth: width,
        containerHeight: height,
        videoWidth: videoNaturalWidth.value,
        videoHeight: videoNaturalHeight.value,
        margin: 16
    });
}

onMounted(() => {
    scheduleWorkspaceSizeUpdate();
    resizeObserver = new ResizeObserver(updateWorkspaceSize);
    if (workspaceRef.value) resizeObserver.observe(workspaceRef.value);
    window.addEventListener('keydown', onKeyDown);
});

onUnmounted(() => {
    resizeObserver?.disconnect();
    window.removeEventListener('keydown', onKeyDown);
});
const frames = computed(() => props.state.frames ?? []);
const anonymizationSettings = computed(() => props.state.anonymizationSettings);
const trackingObjectIds = ref(new Set<string>());
const trackingProgressByTrackId = ref(new Map<number, { startMs: number; endMs: number }>());
const hoveredTimelineKey = ref<string | null>(null);
const timelineExpanded = ref(false);
const timelinePanelRef = ref<HTMLElement | null>(null);

const { activeMode, activate, deactivate, isMerge, isSplit, isAdjust } = useEditorModes();
const { mergeSelectedKeys: mergeSelectedTimelineKeys, toggle: mergeToggle, execute: mergeExecute } = useMerge();
const { selectedOccurrences, toggle: toggleOccurrence, totalCount, hasAny, hasOnlyTracked, clear: clearOccurrences } = useOccurrenceSelection();
const { splitSourceKey, execute: splitExecute } = useSplit();
const { currentFrame, timelineObjects, timelineObjectCounts } = useTimelineObjects(frames, currentTime);
const visibleBlurPreviewObjects = useBlurPreviewObjects(frames, currentFrame, currentTime, anonymizationSettings, isAdjust);
const { toggleObject, toggleTrackedObject, setTrackId, deleteObject, addBox } = useDetectedObjectActions(
    props.state,
    frames,
    currentFrame
);
const { findSegmentFor } = useConsecutiveTrackSegment(frames);
const {
    getTrackSettings,
    getSegmentBufferValues,
    applyTrackBlurShape,
    applyTrackBlurSize,
    resetTrackBlurSize,
    applySegmentPre,
    applySegmentPost,
    resetSegmentPre,
    resetSegmentPost
} = useTrackSettings(props.state, frames, anonymizationSettings);

const allObjects = computed(() => frames.value.flatMap(frame => frame.detectedObjects));

function frameTimeFor(obj: DetectedObjectDto): number {
    return frames.value.find(frame => frame.id === obj.analyzedFrameId)?.timeSeconds ?? 0;
}

const selectedKey = ref<string | null>(null);

function selectObject(obj: DetectedObjectDto) {
    selectedKey.value = getObjTimelineKey(obj);
}

function clearSelection() {
    selectedKey.value = null;
}

function toggleTimelineExpanded() {
    timelineExpanded.value = !timelineExpanded.value;
}

function expandTimeline() {
    timelineExpanded.value = true;
}

function selectOccurrenceAndSeek(obj: DetectedObjectDto, time: number) {
    selectObject(obj);
    seekTo(time);
}

const selectedTimelineObject = computed<TimelineObject | null>(() => {
    const key = selectedKey.value;
    if (!key) return null;
    return timelineObjects.value.find(obj => getTimelineKey(obj) === key) ?? null;
});

function isTimelineKeySelected(obj: TimelineObject): boolean {
    return selectedKey.value != null && getTimelineKey(obj) === selectedKey.value;
}

async function scrollSelectedTrackIntoView() {
    if (!timelineExpanded.value || !selectedKey.value || !timelinePanelRef.value) return;
    await nextTick();
    const row = timelinePanelRef.value.querySelector(
        `[data-timeline-key="${selectedKey.value}"]`
    ) as HTMLElement | null;
    if (row && typeof row.scrollIntoView === 'function') {
        row.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
    }
}

watch([timelineExpanded, selectedKey], ([expanded]) => {
    if (expanded) {
        scrollSelectedTrackIntoView();
    }
    scheduleWorkspaceSizeUpdate();
});

watch(activeMode, (mode) => {
    if (mode === 'merge' || mode === 'split') {
        timelineExpanded.value = true;
    }
});

const selectedOccurrence = computed<DetectedObjectDto | null>(() => {
    const key = selectedKey.value;
    if (!key) return null;
    if (key.startsWith('obj-')) {
        return allObjects.value.find(obj => obj.id === key.slice(4)) ?? null;
    }
    const trackId = Number(key.slice('track-'.length));
    const occurrences = getTrackOccurrences(frames.value, trackId);
    if (occurrences.length === 0) return null;
    let nearest = occurrences[0];
    let nearestDistance = Math.abs(frameTimeFor(nearest) - currentTime.value);
    for (const obj of occurrences.slice(1)) {
        const distance = Math.abs(frameTimeFor(obj) - currentTime.value);
        if (distance < nearestDistance) {
            nearest = obj;
            nearestDistance = distance;
        }
    }
    return nearest;
});

const selectedTrackOccurrences = computed<DetectedObjectDto[]>(() => {
    const key = selectedKey.value;
    if (!key) return [];
    if (key.startsWith('obj-')) {
        const id = key.slice(4);
        return allObjects.value.filter(obj => obj.id === id);
    }
    const trackId = Number(key.slice('track-'.length));
    return getTrackOccurrences(frames.value, trackId);
});

const selectedOccurrenceIndex = computed(() => {
    const occurrence = selectedOccurrence.value;
    if (!occurrence) return -1;
    return selectedTrackOccurrences.value.findIndex(obj => obj.id === occurrence.id);
});

const canGoPrevious = computed(() => selectedOccurrenceIndex.value > 0);
const canGoNext = computed(() =>
    selectedOccurrenceIndex.value >= 0
    && selectedOccurrenceIndex.value < selectedTrackOccurrences.value.length - 1
);

function goToPreviousOccurrence() {
    const index = selectedOccurrenceIndex.value;
    if (index <= 0) return;
    seekTo(frameTimeFor(selectedTrackOccurrences.value[index - 1]));
}

function goToNextOccurrence() {
    const index = selectedOccurrenceIndex.value;
    if (index < 0 || index >= selectedTrackOccurrences.value.length - 1) return;
    seekTo(frameTimeFor(selectedTrackOccurrences.value[index + 1]));
}

const selectedTrackSettings = computed(() => {
    const obj = selectedOccurrence.value;
    if (!obj) return null;
    const segment = findSegmentFor(obj);
    if (!segment) return null;
    const buffers = getSegmentBufferValues(segment);
    const trackId = obj.trackId;
    const settings = trackId != null
        ? getTrackSettings(trackId)
        : { shape: obj.blurShape ?? null, blurSizePercentOverride: obj.blurSizePercentOverride ?? null };
    return {
        trackId,
        label: getLabel(obj),
        included: obj.selected,
        globalBlurSizePercent: props.state.anonymizationSettings.blurSizePercent,
        shape: settings.shape,
        blurSizePercentOverride: settings.blurSizePercentOverride,
        pre: buffers.pre,
        preIsCustom: buffers.preIsCustom,
        post: buffers.post,
        postIsCustom: buffers.postIsCustom
    };
});

const inspectorPlacement = computed(() => {
    const obj = selectedOccurrence.value;
    if (!obj) return null;
    const { width: stageWidth, height: stageHeight } = workspaceSize.value;
    if (stageWidth <= 0 || stageHeight <= 0) {
        return { top: 8, left: 8, width: 240 };
    }
    return computeInspectorPlacement({
        stageWidth,
        stageHeight,
        videoRect: videoRect.value,
        box: obj,
        videoWidth: videoNaturalWidth.value,
        videoHeight: videoNaturalHeight.value
    });
});

const inspectorStyle = computed(() => {
    const placement = inspectorPlacement.value;
    return placement ? { top: placement.top + 'px', left: placement.left + 'px', width: placement.width + 'px' } : null;
});

function getSelectedSegment() {
    const obj = selectedOccurrence.value;
    return obj ? findSegmentFor(obj) : null;
}

function handleEmptySpaceClick() {
    if (activeMode.value === 'select') {
        clearSelection();
    }
}

const adjustBeforeState = ref<DetectedObjectDto | null>(null);
const adjustObject = computed(() => (activeMode.value === 'adjust' ? selectedOccurrence.value : null));

function pausePlayback() {
    videoPlayerRef.value?.videoRef?.pause();
}

function handleAdjustDetection() {
    const obj = selectedOccurrence.value;
    if (!obj) return;
    adjustBeforeState.value = JSON.parse(JSON.stringify(obj));
    pausePlayback();
    activate('adjust');
}

function handleAdjustDone() {
    const obj = adjustObject.value;
    const before = adjustBeforeState.value;
    if (obj && before) {
        props.state.onDetectedObjectUpdated?.(props.state.videoId, obj.analyzedFrameId, obj, 'adjust', [before]);
    }
    adjustBeforeState.value = null;
    deactivate();
}

function handleAdjustReset() {
    const obj = adjustObject.value;
    if (obj && adjustBeforeState.value) {
        Object.assign(obj, adjustBeforeState.value);
    }
}

const pendingLabel = ref<{ x: number; y: number; width: number; height: number } | null>(null);

const existingTrackIds = computed(() => {
    const ids = new Set<number>();
    for (const frame of frames.value) {
        for (const obj of frame.detectedObjects) {
            if (obj.trackId != null) ids.add(obj.trackId);
        }
    }
    return [...ids].sort((a, b) => a - b);
});

const trackIdsInCurrentFrame = computed(() => {
    const frame = currentFrame.value;
    if (!frame) return new Set<number>();
    return new Set(frame.detectedObjects.flatMap(obj => obj.trackId == null ? [] : [obj.trackId]));
});

function handleAddObject() {
    activate('add');
}

function handleDrawComplete(box: { x: number; y: number; width: number; height: number }) {
    pendingLabel.value = box;
}

function handleAddConfirm(className: string, trackId: 'new' | number) {
    if (pendingLabel.value) {
        addBox(pendingLabel.value.x, pendingLabel.value.y, pendingLabel.value.width, pendingLabel.value.height, className, trackId);
    }
    pendingLabel.value = null;
    deactivate();
}

function handleAddCancel() {
    pendingLabel.value = null;
    deactivate();
}

function handleTrackForward() {
    const obj = selectedOccurrence.value;
    if (!obj) return;
    if (trackingObjectIds.value.has(obj.id)) return;
    trackForward(obj);
}

function handleMerge() {
    mergeSelectedTimelineKeys.value = new Set();
    clearOccurrences();
    expandTimeline();
    activate('merge');
}

function handleSplit() {
    clearOccurrences();
    expandTimeline();
    activate('split');
}

function handleDelete() {
    const obj = selectedOccurrence.value;
    if (!obj) return;
    deleteObject(obj);
    clearSelection();
}

function handleCancel() {
    if (activeMode.value === 'merge') {
        mergeSelectedTimelineKeys.value = new Set();
        deactivate();
    } else if (activeMode.value === 'split') {
        clearOccurrences();
        deactivate();
    } else if (activeMode.value === 'add') {
        pendingLabel.value = null;
        deactivate();
    }
}

function onKeyDown(event: KeyboardEvent) {
    if (event.key !== 'Escape') return;

    if (activeMode.value === 'adjust') {
        const obj = adjustObject.value;
        if (obj && adjustBeforeState.value) {
            Object.assign(obj, adjustBeforeState.value);
        }
        adjustBeforeState.value = null;
        deactivate();
        return;
    }

    if (activeMode.value === 'add' && pendingLabel.value) {
        pendingLabel.value = null;
        deactivate();
        return;
    }

    if (activeMode.value === 'select') {
        clearSelection();
    }
}

/** Inspector: include/exclude only the currently selected occurrence. */
function handleToggleOccurrenceInclude(checked: boolean) {
    const obj = selectedOccurrence.value;
    if (!obj) return;
    toggleObject(obj.id, checked);
}

/** Timeline track row / collapsed strip: bulk include/exclude every occurrence in the track. */
function handleToggleTrackInclude(checked: boolean) {
    const timelineObject = selectedTimelineObject.value;
    if (timelineObject) {
        toggleTrackedObject(timelineObject, checked);
        return;
    }
    const obj = selectedOccurrence.value;
    if (!obj) return;
    toggleObject(obj.id, checked);
}

function handleUpdateShape(shape: string) {
    const obj = selectedOccurrence.value;
    if (!obj) return;
    if (obj.trackId != null) { applyTrackBlurShape(obj.trackId, shape); return; }
    const before = [JSON.parse(JSON.stringify(obj))];
    obj.blurShape = shape;
    props.state.onDetectedObjectUpdated?.(props.state.videoId, obj.analyzedFrameId, obj, 'track-settings', before);
}

function handleUpdateBlurSize(percent: number) {
    const obj = selectedOccurrence.value;
    if (!obj) return;
    if (obj.trackId != null) { applyTrackBlurSize(obj.trackId, percent); return; }
    const before = [JSON.parse(JSON.stringify(obj))];
    const normalized = percent === props.state.anonymizationSettings.blurSizePercent ? null : percent;
    obj.blurSizePercentOverride = normalized;
    props.state.onDetectedObjectUpdated?.(props.state.videoId, obj.analyzedFrameId, obj, 'track-settings', before);
}

function handleResetBlurSize() {
    const obj = selectedOccurrence.value;
    if (!obj) return;
    if (obj.trackId != null) { resetTrackBlurSize(obj.trackId); return; }
    const before = [JSON.parse(JSON.stringify(obj))];
    obj.blurSizePercentOverride = null;
    props.state.onDetectedObjectUpdated?.(props.state.videoId, obj.analyzedFrameId, obj, 'track-settings', before);
}

function handleUpdatePre(valueMs: number) {
    const segment = getSelectedSegment();
    if (segment) applySegmentPre(segment, valueMs);
}

function handleUpdatePost(valueMs: number) {
    const segment = getSelectedSegment();
    if (segment) applySegmentPost(segment, valueMs);
}

function handleResetPre() {
    const segment = getSelectedSegment();
    if (segment) resetSegmentPre(segment);
}

function handleResetPost() {
    const segment = getSelectedSegment();
    if (segment) resetSegmentPost(segment);
}

function trackForward(obj: DetectedObjectDto) {
    trackingObjectIds.value.add(obj.id);
    trackingObjectIds.value = new Set(trackingObjectIds.value);
    const seedFrame = props.state.frames.find(f => f.id === obj.analyzedFrameId);
    if (seedFrame) {
        const nextFrame = props.state.frames
            .filter(f => f.timeSeconds > seedFrame.timeSeconds)
            .sort((a, b) => a.timeSeconds - b.timeSeconds)[0];
        if (nextFrame) {
            const t = nextFrame.timeSeconds * 1000;
            if (obj.trackId != null) {
                const nextProgress = new Map(trackingProgressByTrackId.value);
                nextProgress.set(obj.trackId, { startMs: t, endMs: t });
                trackingProgressByTrackId.value = nextProgress;
            }
        }
    }
    props.state.onTrackForward?.(props.state.videoId, obj.analyzedFrameId, obj);
}

useKeyboardUndoRedo(props.state);

function applyChanges(changes: DetectedObjectChangeSet) {
    for (const obj of changes.objectsToUpdate) {
        const frame = props.state.frames.find(f => f.id === obj.analyzedFrameId);
        if (!frame) continue;
        const existing = frame.detectedObjects.find(o => o.id === obj.id);
        if (existing) Object.assign(existing, obj);
    }
    const objectIdsToRemove = new Set(changes.objectsToRemove);
    for (const frame of props.state.frames) {
        frame.detectedObjects = frame.detectedObjects.filter(obj => !objectIdsToRemove.has(obj.id));
    }
    for (const obj of changes.objectsToAdd) {
        const frame = props.state.frames.find(f => f.id === obj.analyzedFrameId);
        if (frame) {
            const existing = frame.detectedObjects.find(existingObject => existingObject.id === obj.id);
            if (existing) Object.assign(existing, obj);
            else frame.detectedObjects.push(obj);
        }
    }
    if (selectedKey.value) {
        if (selectedKey.value.startsWith('obj-')) {
            const id = selectedKey.value.slice(4);
            if (objectIdsToRemove.has(id)) selectedKey.value = null;
        } else {
            const trackId = Number(selectedKey.value.slice('track-'.length));
            if (getTrackOccurrences(props.state.frames, trackId).length === 0) selectedKey.value = null;
        }
    }
}

function clearTrackingObjectId(objectId: string) {
    const trackedObject = frames.value
        .flatMap(frame => frame.detectedObjects)
        .find(obj => obj.id === objectId);
    trackingObjectIds.value.delete(objectId);
    trackingObjectIds.value = new Set(trackingObjectIds.value);
    if (trackedObject?.trackId != null) {
        const nextProgress = new Map(trackingProgressByTrackId.value);
        nextProgress.delete(trackedObject.trackId);
        trackingProgressByTrackId.value = nextProgress;
    }
}

function updateTrackingProgress(trackId: number | null, gapStartMs: number, gapEndMs: number) {
    if (trackId == null) return;

    const nextProgress = new Map(trackingProgressByTrackId.value);
    if (gapStartMs === 0 && gapEndMs === 0) {
        nextProgress.delete(trackId);
    } else {
        nextProgress.set(trackId, { startMs: gapStartMs, endMs: gapEndMs });
    }
    trackingProgressByTrackId.value = nextProgress;
}

function getTrackingProgress(timelineObject: TimelineObject) {
    if (timelineObject.type !== 'tracked') return null;
    const trackId = timelineObject.occurences[0]?.[1].trackId;
    return trackId == null ? null : trackingProgressByTrackId.value.get(trackId) ?? null;
}

defineExpose({ getFrames, applyChanges, clearTrackingObjectId, updateTrackingProgress });

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

const overlayMode = computed<'select' | 'adjust' | 'add'>(() =>
    activeMode.value === 'adjust' || activeMode.value === 'add' ? activeMode.value : 'select'
);

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
        <div ref="workspaceRef" class="workspace-main" @click="handleEmptySpaceClick">
            <div class="video-frame" :style="videoFrameStyle">
                <VideoPlayer ref="videoPlayerRef" :videoSourceUrl="state.videoSourceUrl" :currentTime="currentTime"
                    @time-update="onTimeUpdate" @loaded="onVideoLoaded"
                    @play-state-change="onVideoPlayStateChange" @volume-change="onVideoVolumeChange" />
                <BoundingBoxOverlay v-if="currentFrame && (visibleBlurPreviewObjects.length > 0 || adjustObject)"
                    :objects="visibleBlurPreviewObjects"
                    :anonymization-settings="state.anonymizationSettings"
                    :video-dimensions="videoDimensions"
                    :highlighted-row-key="hoveredTimelineKey"
                    :split-source-key="isSplit ? splitSourceKey : null"
                    :always-show-keys="isMerge && mergeSelectedTimelineKeys.size > 0 ? mergeSelectedTimelineKeys : new Set<string>()"
                    :selected-key="selectedKey"
                    :mode="overlayMode"
                    :adjust-object="adjustObject"
                    @select="selectObject"
                    @draw-complete="handleDrawComplete" />
            </div>

            <ObjectDetailsPanel
              v-if="inspectorStyle && selectedTrackSettings && !isAdjust"
              :style="inspectorStyle"
              v-bind="selectedTrackSettings"
              :can-go-previous="canGoPrevious"
              :can-go-next="canGoNext"
              @toggle-include="handleToggleOccurrenceInclude"
              @update-shape="handleUpdateShape"
              @update-blur-size="handleUpdateBlurSize"
              @reset-blur-size="handleResetBlurSize"
              @update-pre="handleUpdatePre"
              @update-post="handleUpdatePost"
              @reset-pre="handleResetPre"
              @reset-post="handleResetPost"
              @previous-occurrence="goToPreviousOccurrence"
              @next-occurrence="goToNextOccurrence"
              @adjust-detection="handleAdjustDetection"
              @track-forward="handleTrackForward"
              @merge="handleMerge"
              @split="handleSplit"
              @delete="handleDelete"
            />

            <div class="editor-tools" @click.stop>
                <ReviewTools
                  :mode="activeMode"
                  :merge-count="mergeSelectedTimelineKeys.size"
                  :split-count="totalCount()"
                  :can-split="hasOnlyTracked() && hasAny()"
                  @add-object="handleAddObject"
                  @adjust-reset="handleAdjustReset"
                  @adjust-done="handleAdjustDone"
                  @cancel="handleCancel"
                  @merge="mergeAction"
                  @split-out="splitAction"
                />
            </div>

            <AddBoxDialog
              v-if="pendingLabel"
              :existing-track-ids="existingTrackIds"
              :track-ids-in-current-frame="trackIdsInCurrentFrame"
              @cancel="handleAddCancel"
              @confirm="handleAddConfirm"
            />
        </div>

        <div
          ref="timelinePanelRef"
          class="timeline-panel"
          :class="timelineExpanded ? 'timeline-panel--expanded' : 'timeline-panel--collapsed'"
          data-testid="timeline-panel"
        >
            <div class="timeline-left-controls" data-testid="timeline-left-controls">
                <button
                  type="button"
                  class="timeline-play-pause"
                  data-testid="timeline-play-pause"
                  :aria-label="isVideoPlaying ? 'Pause video' : 'Play video'"
                  :title="isVideoPlaying ? 'Pause' : 'Play'"
                  @click.stop="toggleVideoPlayback"
                >
                    <PauseCircleOutlineIcon v-if="isVideoPlaying" />
                    <PlayCircleOutlineIcon v-else />
                </button>
            </div>

            <div class="timeline-main-column">
                <CollapsedTimelineBar
                  :expanded="timelineExpanded"
                  :current-time="currentTime"
                  :duration="videoDuration"
                  :object-counts="timelineObjectCounts"
                  :selected-timeline-object="selectedTimelineObject"
                  :selected-occurrence="selectedOccurrence"
                  :active-gap-range="selectedTimelineObject ? getTrackingProgress(selectedTimelineObject) : null"
                  @toggle-expanded="toggleTimelineExpanded"
                  @seek="seekTo"
                  @toggle-include="handleToggleTrackInclude"
                  @select-occurrence="selectOccurrenceAndSeek"
                />

                <div v-if="timelineExpanded" class="timeline-wrapper" data-testid="expanded-timeline">
                    <div class="timeline-labels">
                        <div class="timeline-toolbar-spacer"></div>
                        <div class="timeline-overview-spacer"></div>
                        <div class="timeline-header-spacer"></div>
                        <TimelineRowLabel
                          v-for="obj in timelineObjects"
                          :key="getTimelineKey(obj)"
                          :timeline-object="obj"
                          :mode="isMerge ? 'merge' : 'select'"
                          :merge-selected-keys="mergeSelectedTimelineKeys"
                          :hovered-timeline-key="hoveredTimelineKey"
                          :is-track-selected="isTimelineKeySelected(obj)"
                          @toggle="toggleTrackedObject"
                          @set-track-id="setTrackId"
                          @merge-toggle="mergeToggle"
                          @select="selectObject"
                          @hover-row="hoveredTimelineKey = $event"
                        />
                    </div>
                    <div class="timeline-content">
                        <Timeline
                          :duration="videoDuration"
                          :currentTime="currentTime"
                          :volume="videoVolume"
                          :object-counts="timelineObjectCounts"
                          @seek="seekTo"
                          @volume-change="setVideoVolume"
                        >
                            <TimelineRow v-for="obj in timelineObjects" :key="getTimelineKey(obj)" :timeline-object="obj"
                                :video-duration="videoDuration"
                                :mode="isMerge ? 'merge' : isSplit ? 'split' : 'select'"
                                :merge-selected-keys="mergeSelectedTimelineKeys"
                                :selected-occurrences="selectedOccurrences"
                                :hovered-timeline-key="hoveredTimelineKey"
                                :active-gap-range="getTrackingProgress(obj)"
                                :is-track-selected="isTimelineKeySelected(obj)"
                                @toggle-occurrence="(k, t, e) => toggleOccurrence(k, t, e, timelineObjects)"
                                @merge-toggle="mergeToggle"
                                @select-occurrence="selectOccurrenceAndSeek"
                                @hover-row="hoveredTimelineKey = $event" />
                        </Timeline>
                    </div>
                </div>
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
    gap: 0;
}

.workspace-main {
    flex: 1 1 auto;
    min-height: 0;
    position: relative;
    overflow: hidden;
    display: flex;
    align-items: center;
    justify-content: center;
}

.video-frame {
    position: relative;
    line-height: 0;
    background: #000;
}

.editor-tools {
    position: absolute;
    left: 12px;
    bottom: 12px;
    z-index: 25;
}

.timeline-panel {
    flex-shrink: 0;
    display: grid;
    grid-template-columns: 48px minmax(0, 1fr);
    align-items: stretch;
    min-height: 0;
    background: var(--mud-palette-surface);
}

.timeline-panel--collapsed {
    flex: 0 0 auto;
}

.timeline-panel--expanded {
    flex: 0 1 auto;
    max-height: min(42vh, 360px);
}

.timeline-left-controls {
    display: flex;
    align-items: flex-start;
    justify-content: center;
    padding-top: 6px;
    border-top: 1px solid var(--mud-palette-lines-default);
    border-right: 1px solid var(--mud-palette-lines-default);
    background: var(--mud-palette-surface);
}

.timeline-play-pause {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 40px;
    height: 40px;
    padding: 0;
    border: 0;
    border-radius: 50%;
    color: var(--mud-palette-primary);
    background: transparent;
    cursor: pointer;
}

.timeline-play-pause:hover {
    background: var(--mud-palette-primary-hover);
}

.timeline-play-pause:focus-visible {
    outline: 2px solid color-mix(in srgb, var(--mud-palette-primary) 70%, transparent);
    outline-offset: 2px;
}

.timeline-play-pause :deep(svg) {
    width: 24px;
    height: 24px;
    fill: currentColor;
}

.timeline-main-column {
    min-width: 0;
    min-height: 0;
    display: flex;
    flex-direction: column;
}

.timeline-wrapper {
    flex: 1 1 auto;
    min-height: 0;
    display: grid;
    grid-template-columns: 160px 1fr;
    overflow-y: auto;
    overflow-x: hidden;
    border-top: 1px solid var(--mud-palette-lines-default);
}

.timeline-labels {
    padding: 8px 8px 8px 6px;
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
    height: 36px;
    isolation: isolate;
}

.timeline-header-spacer {
    height: 22px;
    margin-bottom: 6px;
}

.timeline-overview-spacer {
    position: sticky;
    top: 36px;
    z-index: 20;
    background: var(--mud-palette-surface);
    height: 28px;
    margin-bottom: 4px;
    isolation: isolate;
}
</style>

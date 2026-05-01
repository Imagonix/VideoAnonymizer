<script setup lang="ts">
import { computed, ref } from 'vue';
import VideoPlayer from './VideoPlayer.vue';
import ObjectList from './ObjectList.vue';
import Timeline from './Timeline.vue';
import TimelineRow from './TimelineRow.vue';
import BoundingBoxOverlay from './BoundingBoxOverlay.vue';
import EditorToolbar from './EditorToolbar.vue';
import type { VideoEditorProps, TimelineObject, SingleTimelineObject, TrackedTimelineObject, DetectedObjectDto, PreviewObject, TimelineObjectCount } from './types';
import TimelineRowLabel from './TimelineRowLabel.vue';

const props = defineProps<{
    state: VideoEditorProps;
}>();
defineExpose({
    getFrames
})

const currentTime = ref(0);
const videoDuration = ref(0);
const isVideoPlaying = ref(false);
const videoVolume = ref(1);
const videoPlayerRef = ref<{
    setVolume: (volume: number) => void;
    togglePlayback: () => Promise<void>;
} | null>(null);

const mergeMode = ref(false);
const mergeSelectedTimelineKeys = ref(new Set<string>());

const splitMode = ref(false);
const selectedOccurrences = ref(new Map<string, Set<number>>());
const lastClickedTimes = ref(new Map<string, number>());

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

const timelineObjectCounts = computed<TimelineObjectCount[]>(() => {
    return frames.value
        .map(frame => ({
            timeSeconds: frame.timeSeconds,
            count: frame.detectedObjects.length
        }))
        .sort((a, b) => a.timeSeconds - b.timeSeconds);
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
    for (const frame of frames.value) {
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

function getTimelineKey(obj: TimelineObject): string {
    if (obj.type === 'single') return `obj-${obj.detectedObj.id}`;
    const tid = obj.occurences[0]?.[1].trackId;
    return tid != null ? `track-${tid}` : `obj-${obj.occurences[0]?.[1].id}`;
}

function mergeToggle(key: string) {
    const set = mergeSelectedTimelineKeys.value;
    if (set.has(key)) {
        set.delete(key);
    } else {
        set.add(key);
    }
    mergeSelectedTimelineKeys.value = new Set(set);
}

function mergeSelected() {
    const keys = [...mergeSelectedTimelineKeys.value];
    if (keys.length < 2) return;

    const selectedTrackIds = new Set<number>();
    const selectedObjIds = new Set<string>();

    for (const obj of timelineObjects.value) {
        const key = getTimelineKey(obj);
        if (!keys.includes(key)) continue;
        if (obj.type === 'single') {
            selectedObjIds.add(obj.detectedObj.id);
            if (obj.detectedObj.trackId != null) {
                selectedTrackIds.add(obj.detectedObj.trackId);
            }
        } else {
            const tid = obj.occurences[0]?.[1].trackId;
            if (tid != null) selectedTrackIds.add(tid);
        }
    }

    const targetTrackId = selectedTrackIds.size > 0
        ? Math.min(...selectedTrackIds)
        : frames.value.flatMap(f => f.detectedObjects)
            .reduce((max, o) => Math.max(max, o.trackId ?? 0), 0) + 1;

    const allTrackIdsToMerge = new Set([targetTrackId, ...selectedTrackIds]);

    for (const frame of props.state.frames) {
        for (const obj of frame.detectedObjects) {
            if (selectedObjIds.has(obj.id)) {
                obj.trackId = targetTrackId;
            } else if (obj.trackId != null && allTrackIdsToMerge.has(obj.trackId)) {
                obj.trackId = targetTrackId;
            }
        }
    }

    mergeSelectedTimelineKeys.value = new Set();
    mergeMode.value = false;
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
            if (obj.trackId === oldTrackId) {
                obj.trackId = trackId;
            }
        }
    }
}

function toggleMergeMode() {
    mergeMode.value = !mergeMode.value;
    if (!mergeMode.value) {
        mergeSelectedTimelineKeys.value = new Set();
    }
    if (mergeMode.value) {
        splitMode.value = false;
        selectedOccurrences.value = new Map();
    }
}

function toggleSplitMode() {
    splitMode.value = !splitMode.value;
    if (!splitMode.value) {
        selectedOccurrences.value = new Map();
        lastClickedTimes.value = new Map();
    }
    if (splitMode.value) {
        mergeMode.value = false;
        mergeSelectedTimelineKeys.value = new Set();
    }
}

function toggleOccurrence(rowKey: string, time: number, event: MouseEvent) {
    const map = selectedOccurrences.value;
    if (!map.has(rowKey)) {
        map.set(rowKey, new Set());
    }
    let times = map.get(rowKey)!;

    if (event.ctrlKey || event.metaKey) {
        if (times.has(time)) {
            times.delete(time);
        } else {
            times.add(time);
        }
    } else if (event.shiftKey) {
        const last = lastClickedTimes.value.get(rowKey);
        if (last != null) {
            const row = timelineObjects.value.find(o => getTimelineKey(o) === rowKey);
            if (row && row.type === 'tracked') {
                const sortedTimes = row.occurences.map(([t]) => t).sort((a, b) => a - b);
                const lastIdx = sortedTimes.indexOf(last);
                const currIdx = sortedTimes.indexOf(time);
                if (lastIdx !== -1 && currIdx !== -1) {
                    const start = Math.min(lastIdx, currIdx);
                    const end = Math.max(lastIdx, currIdx);
                    for (let i = start; i <= end; i++) {
                        times.add(sortedTimes[i]);
                    }
                }
            }
        } else {
            times.add(time);
        }
    } else {
        map.clear();
        map.set(rowKey, new Set([time]));
        times = map.get(rowKey)!;
    }

    if (!times.has(time)) {
        lastClickedTimes.value.delete(rowKey);
    } else {
        lastClickedTimes.value.set(rowKey, time);
    }

    selectedOccurrences.value = new Map(map);
}

function hasSelectedOccurrences(): boolean {
    for (const times of selectedOccurrences.value.values()) {
        if (times.size > 0) return true;
    }
    return false;
}

function selectedOccurrenceCount(): number {
    let count = 0;
    for (const times of selectedOccurrences.value.values()) {
        count += times.size;
    }
    return count;
}

function splitOut() {
    const map = selectedOccurrences.value;
    let anySplit = false;

    const maxTrackId = frames.value.flatMap(f => f.detectedObjects)
        .reduce((max, o) => Math.max(max, o.trackId ?? 0), 0);
    const newTrackId = maxTrackId + 1;

    for (const [rowKey, times] of map) {
        if (times.size === 0) continue;
        const sourceTrackId = parseInt(rowKey.replace('track-', ''), 10);
        if (isNaN(sourceTrackId)) continue;

        for (const frame of props.state.frames) {
            if (!times.has(frame.timeSeconds)) continue;
            for (const obj of frame.detectedObjects) {
                if (obj.trackId === sourceTrackId) {
                    obj.trackId = newTrackId;
                    anySplit = true;
                }
            }
        }
    }

    if (anySplit) {
        selectedOccurrences.value = new Map();
        lastClickedTimes.value = new Map();
    }
}

function hasSelectedOnlyTracked(): boolean {
    for (const rowKey of selectedOccurrences.value.keys()) {
        if (!rowKey.startsWith('track-')) return false;
    }
    return selectedOccurrences.value.size > 0;
}

function seekTo(time: number) {
    currentTime.value = time;
}

function onTimeUpdate(time: number) {
    currentTime.value = time;
}

function onVideoLoaded(duration: number) {
    if (duration && duration > 0) videoDuration.value = duration;
    videoPlayerRef.value?.setVolume(videoVolume.value);
}

function onVideoPlayStateChange(isPlaying: boolean) {
    isVideoPlaying.value = isPlaying;
}

function onVideoVolumeChange(volume: number) {
    videoVolume.value = volume;
}

function toggleVideoPlayback() {
    videoPlayerRef.value?.togglePlayback();
}

function setVideoVolume(volume: number) {
    videoVolume.value = volume;
    videoPlayerRef.value?.setVolume(volume);
}
</script>

<template>
    <div class="video-editor" data-testid="video-editor">
        <EditorToolbar
          :merge-mode="mergeMode"
          :merge-count="mergeSelectedTimelineKeys.size"
          :split-mode="splitMode"
          :selected-occurrence-count="selectedOccurrenceCount()"
          :can-split="hasSelectedOnlyTracked() && hasSelectedOccurrences()"
          @toggle-merge-mode="toggleMergeMode"
          @merge="mergeSelected"
          @toggle-split-mode="toggleSplitMode"
          @split-out="splitOut"
        />
        <div class="top-layout">
            <div class="video-stage">
                <VideoPlayer ref="videoPlayerRef" :videoSourceUrl="state.videoSourceUrl" :currentTime="currentTime"
                    @time-update="onTimeUpdate" @loaded="onVideoLoaded"
                    @play-state-change="onVideoPlayStateChange" @volume-change="onVideoVolumeChange" />
                <BoundingBoxOverlay v-if="currentFrame && visibleBlurPreviewObjects.length > 0"
                    :objects="visibleBlurPreviewObjects" :anonymization-settings="state.anonymizationSettings" />
            </div>

            <ObjectList data-testid="object-list" :objects="orderedCurrentFrameObjects" @toggle="toggleObject" />
        </div>

        <div class="timeline-wrapper">
            <div class="timeline-labels">
                <div class="timeline-toolbar-spacer"></div>
                <div class="timeline-header-spacer"></div>
                <div class="timeline-overview-spacer"></div>
                <TimelineRowLabel
                  v-for="obj in timelineObjects"
                  :timeline-object="obj"
                  :mode="mergeMode ? 'merge' : 'select'"
                  :merge-selected-keys="mergeSelectedTimelineKeys"
                  @toggle="toggleTrackedObject"
                  @set-track-id="setTrackId"
                  @merge-toggle="mergeToggle"
                />
            </div>
            <div class="timeline-content">
                <Timeline :duration="videoDuration" :currentTime="currentTime" :is-playing="isVideoPlaying"
                    :volume="videoVolume" :object-counts="timelineObjectCounts" @seek="seekTo" @toggle-playback="toggleVideoPlayback"
                    @volume-change="setVideoVolume">
                    <TimelineRow v-for="obj in timelineObjects" :timeline-object="obj"
                        :video-duration="videoDuration"
                        :mode="mergeMode ? 'merge' : splitMode ? 'split' : 'select'"
                        :merge-selected-keys="mergeSelectedTimelineKeys"
                        :selected-occurrences="selectedOccurrences"
                        @toggle-occurrence="toggleOccurrence" />
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
    gap: 0;
}

.top-layout {
    display: grid;
    grid-template-columns: max-content minmax(0, 1fr);
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

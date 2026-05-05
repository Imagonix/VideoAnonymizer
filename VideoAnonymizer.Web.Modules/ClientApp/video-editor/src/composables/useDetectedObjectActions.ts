import type { ComputedRef, Ref } from 'vue';
import type { AnalyzedFrameDto, DetectedObjectDto, TimelineObject, VideoEditorProps } from '../types';
import type { EditorMode } from './useEditorModes';

function cloneObjects(objects: DetectedObjectDto[]): DetectedObjectDto[] {
    return JSON.parse(JSON.stringify(objects));
}

export function useDetectedObjectActions(
    state: VideoEditorProps,
    frames: ComputedRef<AnalyzedFrameDto[]>,
    currentFrame: ComputedRef<AnalyzedFrameDto | null>,
    activeMode: Ref<EditorMode>
) {
    function toggleObject(id: string, checked: boolean) {
        const matched = frames.value.flatMap(x => x.detectedObjects.filter(y => y.id === id));
        const before = cloneObjects(matched);
        matched.forEach(obj => { obj.selected = checked });
        if (matched.length === 1) {
            state.onDetectedObjectUpdated?.(state.videoId, matched[0].analyzedFrameId, matched[0], 'toggle', before);
        } else if (matched.length > 1) {
            state.onDetectedObjectsBulkUpdated?.(state.videoId, matched, 'toggle', before);
        }
    }

    function toggleTrackedObject(obj: TimelineObject, checked: boolean) {
        if (obj.type === 'single') {
            const before = cloneObjects([obj.detectedObj]);
            obj.detectedObj.selected = checked;
            state.onDetectedObjectUpdated?.(state.videoId, obj.detectedObj.analyzedFrameId, obj.detectedObj, 'toggle', before);
            return;
        }

        const changed: DetectedObjectDto[] = [];
        obj.occurences.forEach(([_, o]) => { changed.push(o); });
        const before = cloneObjects(changed);
        obj.occurences.forEach(([_, o]) => { o.selected = checked; });
        state.onDetectedObjectsBulkUpdated?.(state.videoId, changed, 'toggle', before);
    }

    function setTrackId(timelineObject: TimelineObject, trackId: number) {
        if (timelineObject.type === 'single') {
            const before = cloneObjects([timelineObject.detectedObj]);
            timelineObject.detectedObj.trackId = trackId;
            state.onDetectedObjectUpdated?.(state.videoId, timelineObject.detectedObj.analyzedFrameId, timelineObject.detectedObj, 'reassign', before);
            return;
        }

        const oldTrackId = timelineObject.occurences[0]?.[1].trackId;
        if (oldTrackId == null) return;

        const changed: DetectedObjectDto[] = [];
        for (const frame of state.frames) {
            for (const obj of frame.detectedObjects) {
                if (obj.trackId === oldTrackId) { changed.push(obj); }
            }
        }

        const before = cloneObjects(changed);
        changed.forEach(obj => { obj.trackId = trackId; });
        state.onDetectedObjectsBulkUpdated?.(state.videoId, changed, 'reassign', before);
    }

    function deleteObject(obj: DetectedObjectDto) {
        const frame = state.frames.find(f => f.id === obj.analyzedFrameId);
        if (!frame) return;

        const idx = frame.detectedObjects.findIndex(o => o.id === obj.id);
        if (idx < 0) return;

        frame.detectedObjects.splice(idx, 1);
        state.onDetectedObjectDeleted?.(state.videoId, obj.analyzedFrameId, obj);
    }

    function addBox(x: number, y: number, width: number, height: number, className: string, trackId: 'new' | number) {
        if (!currentFrame.value) return;

        const frame = currentFrame.value;
        const nextTrackId = frames.value.flatMap(f => f.detectedObjects).reduce((max, o) => Math.max(max, o.trackId ?? 0), 0) + 1;
        const frameTrackIds = new Set(frame.detectedObjects.flatMap(o => o.trackId == null ? [] : [o.trackId]));
        const resolvedTrackId = trackId === 'new' || frameTrackIds.has(trackId)
            ? nextTrackId
            : trackId;
        const newObj: DetectedObjectDto = {
            id: crypto.randomUUID(),
            confidence: 1,
            className: className || null,
            selected: true,
            trackId: resolvedTrackId,
            x,
            y,
            width,
            height,
            analyzedFrameId: frame.id,
        };

        frame.detectedObjects.push(newObj);
        state.onDetectedObjectAdded?.(state.videoId, frame.id, newObj);
    }

    function onBoxUpdated(obj: DetectedObjectDto, beforeState: DetectedObjectDto[]) {
        state.onDetectedObjectUpdated?.(state.videoId, obj.analyzedFrameId, obj, activeMode.value, beforeState);
    }

    return {
        toggleObject,
        toggleTrackedObject,
        setTrackId,
        deleteObject,
        addBox,
        onBoxUpdated,
    };
}

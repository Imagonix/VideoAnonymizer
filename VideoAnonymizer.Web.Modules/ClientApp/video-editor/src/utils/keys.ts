import type { TimelineObject, DetectedObjectDto } from '../types';

export function buildObjectKey(obj: DetectedObjectDto): string {
    return obj.trackId != null ? `track-${obj.trackId}` : obj.id;
}

export function getTimelineKey(obj: TimelineObject): string {
    if (obj.type === 'single') return `obj-${obj.detectedObj.id}`;
    const tid = obj.occurences[0]?.[1].trackId;
    return tid != null ? `track-${tid}` : `obj-${obj.occurences[0]?.[1].id}`;
}

export function getObjTimelineKey(dto: DetectedObjectDto): string {
    return dto.trackId != null ? `track-${dto.trackId}` : `obj-${dto.id}`;
}

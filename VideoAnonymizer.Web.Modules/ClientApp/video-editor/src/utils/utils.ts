import type { DetectedObjectDto } from '../types'
export function getLabel(obj: DetectedObjectDto): string {
    if (obj.trackId == null) return obj.className == null ? obj.id : obj.className;
    if (obj.trackId != null) return "" + obj.trackId;
    return obj.id;
}
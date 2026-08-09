import type { DetectedObjectDto } from '../types';

/**
 * Returns a copy holding only the in-frame intersection, or null when the raw region
 * is fully outside. Does not mutate the input. When video dimensions are unknown
 * (width/height <= 0), the box is returned unchanged so clipping never collapses
 * regions before the video loads.
 */
export function clipProjectedRegionToFrame(
    obj: DetectedObjectDto,
    videoWidth: number,
    videoHeight: number
): DetectedObjectDto | null {
    if (videoWidth <= 0 || videoHeight <= 0) {
        return { ...obj };
    }

    const left = Math.max(0, obj.x);
    const top = Math.max(0, obj.y);
    const right = Math.min(videoWidth, obj.x + obj.width);
    const bottom = Math.min(videoHeight, obj.y + obj.height);

    if (right <= left || bottom <= top) {
        return null;
    }

    return {
        ...obj,
        x: left,
        y: top,
        width: right - left,
        height: bottom - top,
    };
}

/** True when the raw region has no intersection with the frame. */
export function isProjectedRegionFullyOutside(
    obj: DetectedObjectDto,
    videoWidth: number,
    videoHeight: number
): boolean {
    if (videoWidth <= 0 || videoHeight <= 0) {
        return false;
    }

    const left = Math.max(0, obj.x);
    const top = Math.max(0, obj.y);
    const right = Math.min(videoWidth, obj.x + obj.width);
    const bottom = Math.min(videoHeight, obj.y + obj.height);
    return right <= left || bottom <= top;
}

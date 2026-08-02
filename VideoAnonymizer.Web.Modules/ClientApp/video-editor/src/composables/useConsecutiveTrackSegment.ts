import { computed, type ComputedRef } from 'vue';
import type { AnalyzedFrameDto, DetectedObjectDto } from '../types';

export type ConsecutiveSegment = {
    occurrences: DetectedObjectDto[];
    first: DetectedObjectDto;
    last: DetectedObjectDto;
};

export function frameOrderKey(frame: AnalyzedFrameDto): number {
    return frame.frameIndex ?? frame.timeSeconds;
}

export function orderedAnalyzedFrames(frames: AnalyzedFrameDto[]): AnalyzedFrameDto[] {
    return [...frames].sort((a, b) => frameOrderKey(a) - frameOrderKey(b));
}

export function getTrackOccurrences(frames: AnalyzedFrameDto[], trackId: number): DetectedObjectDto[] {
    const occurrences: DetectedObjectDto[] = [];
    for (const frame of orderedAnalyzedFrames(frames)) {
        const match = frame.detectedObjects.find(obj => obj.trackId === trackId);
        if (match) occurrences.push(match);
    }
    return occurrences;
}

export function buildAllSegments(frames: AnalyzedFrameDto[]): ConsecutiveSegment[] {
    const ordered = orderedAnalyzedFrames(frames);
    const segments: ConsecutiveSegment[] = [];

    for (const frame of ordered) {
        for (const obj of frame.detectedObjects) {
            if (obj.trackId == null) {
                segments.push({ occurrences: [obj], first: obj, last: obj });
            }
        }
    }

    const trackIds = new Set<number>();
    for (const frame of ordered) {
        for (const obj of frame.detectedObjects) {
            if (obj.trackId != null) trackIds.add(obj.trackId);
        }
    }

    for (const trackId of trackIds) {
        const entries: { key: number; obj: DetectedObjectDto }[] = [];
        for (const frame of ordered) {
            const match = frame.detectedObjects.find(obj => obj.trackId === trackId);
            if (match) entries.push({ key: frameOrderKey(frame), obj: match });
        }

        let runStart = 0;
        for (let index = 1; index <= entries.length; index++) {
            if (index < entries.length && entries[index].key === entries[index - 1].key + 1) {
                continue;
            }

            const run = entries.slice(runStart, index).map(entry => entry.obj);
            segments.push({ occurrences: run, first: run[0], last: run[run.length - 1] });
            runStart = index;
        }
    }

    return segments;
}

export function findSegment(
    segments: ConsecutiveSegment[],
    object: DetectedObjectDto
): ConsecutiveSegment | null {
    return segments.find(segment => segment.occurrences.some(obj => obj.id === object.id)) ?? null;
}

/**
 * Keeps the boundary-storage invariant for every consecutive segment: only its first
 * occurrence may store a pre-buffer override and only its last may store a post-buffer
 * override. Interior boundary overrides are cleared in-place.
 */
export function normalizeSegmentBoundaries(frames: AnalyzedFrameDto[]): void {
    for (const segment of buildAllSegments(frames)) {
        for (const obj of segment.occurrences) {
            if (obj.id !== segment.first.id) obj.preBufferMsOverride = null;
            if (obj.id !== segment.last.id) obj.postBufferMsOverride = null;
        }
    }
}

/**
 * Applies the boundary-storage invariant after a newly added occurrence. When the
 * addition extends a consecutive run, the previous outer boundary override is moved to
 * the new outer boundary; a new run keeps null overrides. Mirrors the server-side
 * track-forward transfer behavior. Returns the neighboring occurrences whose boundary
 * overrides were cleared, together with their pre-transfer state, so callers can persist
 * those changes.
 */
export function applyBoundaryTransferOnAdd(
    frames: AnalyzedFrameDto[],
    addedObject: DetectedObjectDto
): { obj: DetectedObjectDto; before: DetectedObjectDto }[] {
    const segment = findSegment(buildAllSegments(frames), addedObject);
    if (!segment || segment.occurrences.length < 2) return [];

    const cleared: { obj: DetectedObjectDto; before: DetectedObjectDto }[] = [];
    const indexInSegment = segment.occurrences.findIndex(obj => obj.id === addedObject.id);
    if (indexInSegment === segment.occurrences.length - 1) {
        const previous = segment.occurrences[indexInSegment - 1];
        if (previous.postBufferMsOverride != null) {
            cleared.push({ obj: previous, before: JSON.parse(JSON.stringify(previous)) });
            addedObject.postBufferMsOverride = previous.postBufferMsOverride;
            previous.postBufferMsOverride = null;
        }
    } else if (indexInSegment === 0) {
        const next = segment.occurrences[indexInSegment + 1];
        if (next.preBufferMsOverride != null) {
            cleared.push({ obj: next, before: JSON.parse(JSON.stringify(next)) });
            addedObject.preBufferMsOverride = next.preBufferMsOverride;
            next.preBufferMsOverride = null;
        }
    }

    normalizeSegmentBoundaries(frames);
    return cleared;
}

export function useConsecutiveTrackSegment(frames: ComputedRef<AnalyzedFrameDto[]>) {
    const segments = computed(() => buildAllSegments(frames.value));

    function findSegmentFor(object: DetectedObjectDto): ConsecutiveSegment | null {
        return findSegment(segments.value, object);
    }

    return { segments, findSegmentFor };
}

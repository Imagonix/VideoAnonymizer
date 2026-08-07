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

/**
 * Builds consecutive segments from the complete analyzed-frame sequence ordered by
 * FrameIndex. FrameIndex is only the ordering key — adjacency means successive entries
 * in that ordered list, not a numeric FrameIndex difference of 1. A missing occurrence
 * of the track in any intervening analyzed frame ends the segment.
 */
export function buildAllSegments(frames: AnalyzedFrameDto[]): ConsecutiveSegment[] {
    const ordered = orderedAnalyzedFrames(frames);
    const segments: ConsecutiveSegment[] = [];

    // Untracked occurrences are always one-object segments.
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
        // Walk every analyzed frame in order. Continue the run when this frame has the
        // track; an intervening analyzed frame without the track breaks it.
        let currentRun: DetectedObjectDto[] = [];
        for (const frame of ordered) {
            const match = frame.detectedObjects.find(obj => obj.trackId === trackId);
            if (match) {
                currentRun.push(match);
                continue;
            }

            if (currentRun.length > 0) {
                segments.push({
                    occurrences: currentRun,
                    first: currentRun[0],
                    last: currentRun[currentRun.length - 1],
                });
                currentRun = [];
            }
        }

        if (currentRun.length > 0) {
            segments.push({
                occurrences: currentRun,
                first: currentRun[0],
                last: currentRun[currentRun.length - 1],
            });
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
 * Ordered same-track consecutive segments that have a real gap between them.
 */
export function getTrackSegments(frames: AnalyzedFrameDto[], trackId: number): ConsecutiveSegment[] {
    return buildAllSegments(frames)
        .filter(segment => segment.first.trackId === trackId)
        .sort((a, b) => {
            const aKey = a.first.analyzedFrameId;
            const bKey = b.first.analyzedFrameId;
            const ordered = orderedAnalyzedFrames(frames);
            return ordered.findIndex(f => f.id === aKey) - ordered.findIndex(f => f.id === bKey);
        });
}

/**
 * True when this segment has a real following same-track gap (another later segment).
 */
export function hasFollowingGap(
    frames: AnalyzedFrameDto[],
    segment: ConsecutiveSegment
): boolean {
    const trackId = segment.first.trackId;
    if (trackId == null) return false;
    const trackSegments = getTrackSegments(frames, trackId);
    const index = trackSegments.findIndex(s => s.occurrences.some(o => o.id === segment.first.id));
    return index >= 0 && index < trackSegments.length - 1;
}

/**
 * True when this segment has a real preceding same-track gap.
 */
export function hasPrecedingGap(
    frames: AnalyzedFrameDto[],
    segment: ConsecutiveSegment
): boolean {
    const trackId = segment.first.trackId;
    if (trackId == null) return false;
    const trackSegments = getTrackSegments(frames, trackId);
    const index = trackSegments.findIndex(s => s.occurrences.some(o => o.id === segment.first.id));
    return index > 0;
}

/**
 * The previous segment's last occurrence that owns "Gap before" for the selected segment.
 */
export function getPrecedingGapBoundary(
    frames: AnalyzedFrameDto[],
    segment: ConsecutiveSegment
): DetectedObjectDto | null {
    const trackId = segment.first.trackId;
    if (trackId == null) return null;
    const trackSegments = getTrackSegments(frames, trackId);
    const index = trackSegments.findIndex(s => s.occurrences.some(o => o.id === segment.first.id));
    if (index <= 0) return null;
    return trackSegments[index - 1].last;
}

/**
 * Keeps the boundary-storage invariant for every consecutive segment: only its first
 * occurrence may store a pre-buffer override and only its last may store a post-buffer
 * override or next-gap handling mode. Gap modes are retained only on last occurrences
 * that still have a real following same-track gap.
 */
export function normalizeSegmentBoundaries(frames: AnalyzedFrameDto[]): void {
    const segments = buildAllSegments(frames);
    const lastBeforeGap = new Set<string>();

    const byTrack = new Map<number, ConsecutiveSegment[]>();
    for (const segment of segments) {
        if (segment.first.trackId == null) continue;
        const list = byTrack.get(segment.first.trackId) ?? [];
        list.push(segment);
        byTrack.set(segment.first.trackId, list);
    }

    for (const trackSegments of byTrack.values()) {
        const ordered = getTrackSegments(
            frames,
            trackSegments[0].first.trackId!
        );
        for (let i = 0; i < ordered.length - 1; i++) {
            lastBeforeGap.add(ordered[i].last.id);
        }
    }

    for (const segment of segments) {
        for (const obj of segment.occurrences) {
            if (obj.id !== segment.first.id) obj.preBufferMsOverride = null;
            if (obj.id !== segment.last.id) {
                obj.postBufferMsOverride = null;
                obj.nextGapHandlingMode = null;
            } else if (!lastBeforeGap.has(obj.id)) {
                obj.nextGapHandlingMode = null;
            }
        }
    }
}

/**
 * Applies the boundary-storage invariant after a newly added occurrence. When the
 * addition extends a consecutive run, the previous outer boundary override and gap
 * mode are moved to the new outer boundary; a new run keeps null overrides. Returns
 * the neighboring occurrences whose boundary overrides were cleared, together with
 * their pre-transfer state, so callers can persist those changes.
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
        if (previous.postBufferMsOverride != null || previous.nextGapHandlingMode != null) {
            cleared.push({ obj: previous, before: JSON.parse(JSON.stringify(previous)) });
            addedObject.postBufferMsOverride = previous.postBufferMsOverride ?? null;
            addedObject.nextGapHandlingMode = previous.nextGapHandlingMode ?? null;
            previous.postBufferMsOverride = null;
            previous.nextGapHandlingMode = null;
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

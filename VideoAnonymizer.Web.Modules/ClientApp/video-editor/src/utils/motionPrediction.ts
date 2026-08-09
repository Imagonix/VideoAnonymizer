import type { AnalyzedFrameDto, DetectedObjectDto, PreviewObject } from '../types';
import { buildAllSegments, orderedAnalyzedFrames } from '../composables/useConsecutiveTrackSegment';
import { isUseBuffersGap } from './gapHandling';
import { isProjectedRegionFullyOutside } from './projectedRegion';

type TimedDetectedObject = {
    timeSeconds: number;
    detectedObject: DetectedObjectDto;
};

type SegmentView = {
    segment: ReturnType<typeof buildAllSegments>[number];
    selected: TimedDetectedObject[];
    trackId: number | null;
};

/**
 * Predicts preview regions for the current video time using the same consecutive-segment
 * state machine as export: pre-buffer extrapolation, in-segment interpolation, post-buffer
 * extrapolation, and default Interpolate gap bridging. Projection keeps raw unbounded
 * geometry; the overlay clips visually via overflow. Fully outside raw regions are omitted.
 */
export function getPredictedBlurPreviewObjects(
    frames: AnalyzedFrameDto[],
    currentTimeSeconds: number,
    timeBufferSeconds: number,
    videoWidth = 0,
    videoHeight = 0
): PreviewObject[] {
    if (frames.length === 0) return [];

    const timeByFrameId = new Map(
        orderedAnalyzedFrames(frames).map(frame => [frame.id, frame.timeSeconds])
    );
    const globalBufferMs = Math.round(timeBufferSeconds * 1000);
    const segments = buildAllSegments(frames);
    const result: PreviewObject[] = [];

    const views: SegmentView[] = segments.map(segment => ({
        segment,
        selected: segment.occurrences
            .filter(obj => obj.selected)
            .map(obj => toTimed(obj, timeByFrameId))
            .sort((a, b) => a.timeSeconds - b.timeSeconds),
        trackId: segment.first.trackId ?? null,
    }));

    const trackedGroups = new Map<number, SegmentView[]>();
    for (const view of views) {
        if (view.trackId == null) continue;
        const list = trackedGroups.get(view.trackId) ?? [];
        list.push(view);
        trackedGroups.set(view.trackId, list);
    }
    for (const list of trackedGroups.values()) {
        list.sort((a, b) => frameOrder(a.segment.first, timeByFrameId) - frameOrder(b.segment.first, timeByFrameId));
    }

    for (const view of views) {
        if (view.selected.length === 0) continue;

        const preBufferSeconds = Math.max(0, (view.segment.first.preBufferMsOverride ?? globalBufferMs) / 1000);
        const postBufferSeconds = Math.max(0, (view.segment.last.postBufferMsOverride ?? globalBufferMs) / 1000);
        const firstTime = view.selected[0].timeSeconds;
        const lastTime = view.selected[view.selected.length - 1].timeSeconds;

        let precedingGapInterpolates = false;
        let followingGapInterpolates = false;
        if (view.trackId != null) {
            const trackSegments = trackedGroups.get(view.trackId) ?? [];
            const index = trackSegments.indexOf(view);
            if (index > 0) {
                precedingGapInterpolates = !isUseBuffersGap(
                    trackSegments[index - 1].segment.last.nextGapHandlingMode
                );
            }
            if (index >= 0 && index < trackSegments.length - 1) {
                followingGapInterpolates = !isUseBuffersGap(view.segment.last.nextGapHandlingMode);
            }
        }

        const effectivePreSeconds = precedingGapInterpolates ? 0 : preBufferSeconds;
        const effectivePostSeconds = followingGapInterpolates ? 0 : postBufferSeconds;

        if (currentTimeSeconds < firstTime - effectivePreSeconds) {
            continue;
        }

        let projected: DetectedObjectDto;
        let activation: PreviewObject['activation'];

        if (currentTimeSeconds < firstTime) {
            if (effectivePreSeconds <= 0) continue;
            projected = projectPreBufferObject(view.selected, currentTimeSeconds);
            activation = 'pre';
        } else if (currentTimeSeconds > lastTime) {
            if (effectivePostSeconds <= 0 || currentTimeSeconds > lastTime + effectivePostSeconds) {
                continue;
            }

            projected = projectPostBufferObject(view.selected, currentTimeSeconds);
            activation = isAtSampleTime(lastTime, currentTimeSeconds) ? 'detected' : 'post';
        } else {
            projected = interpolateInSegment(view.selected, currentTimeSeconds);
            activation = view.selected.some(sample => isAtSampleTime(sample.timeSeconds, currentTimeSeconds))
                ? 'detected'
                : 'interpolated';
        }

        if (isProjectedRegionFullyOutside(projected, videoWidth, videoHeight)) {
            continue;
        }

        if (projected.width <= 0 || projected.height <= 0) {
            continue;
        }

        result.push({ detectedObject: projected, activation });
    }

    // Default Interpolate bridges each real same-track gap with one current-time region.
    for (const trackSegments of trackedGroups.values()) {
        for (let index = 0; index < trackSegments.length - 1; index++) {
            const previous = trackSegments[index];
            const next = trackSegments[index + 1];
            if (isUseBuffersGap(previous.segment.last.nextGapHandlingMode)) {
                continue;
            }

            const previousBoundary = previous.segment.last;
            const nextBoundary = next.segment.first;
            if (!previousBoundary.selected || !nextBoundary.selected) {
                continue;
            }

            const previousTime = timeByFrameId.get(previousBoundary.analyzedFrameId) ?? 0;
            const nextTime = timeByFrameId.get(nextBoundary.analyzedFrameId) ?? 0;
            if (currentTimeSeconds <= previousTime || currentTimeSeconds >= nextTime) {
                continue;
            }

            const projected = projectObject(
                { timeSeconds: previousTime, detectedObject: previousBoundary },
                { timeSeconds: nextTime, detectedObject: nextBoundary },
                currentTimeSeconds,
                true
            );

            if (isProjectedRegionFullyOutside(projected, videoWidth, videoHeight)) {
                continue;
            }

            if (projected.width <= 0 || projected.height <= 0) {
                continue;
            }

            result.push({ detectedObject: projected, activation: 'interpolated' });
        }
    }

    return result;
}

function frameOrder(obj: DetectedObjectDto, timeByFrameId: Map<string, number>): number {
    return timeByFrameId.get(obj.analyzedFrameId) ?? 0;
}

function toTimed(
    detectedObject: DetectedObjectDto,
    timeByFrameId: Map<string, number>
): TimedDetectedObject {
    return {
        timeSeconds: timeByFrameId.get(detectedObject.analyzedFrameId) ?? 0,
        detectedObject,
    };
}

function interpolateInSegment(
    selected: TimedDetectedObject[],
    currentTimeSeconds: number
): DetectedObjectDto {
    const previous = findPreviousSample(selected, currentTimeSeconds);
    if (!previous) {
        return { ...selected[0].detectedObject };
    }

    if (isAtSampleTime(previous.timeSeconds, currentTimeSeconds)) {
        return { ...previous.detectedObject };
    }

    const next = selected.find(sample => sample.timeSeconds > currentTimeSeconds);
    if (!next) {
        return { ...previous.detectedObject };
    }

    if (previous.detectedObject.trackId == null) {
        return { ...previous.detectedObject };
    }

    return projectObject(previous, next, currentTimeSeconds, true);
}

function projectPreBufferObject(
    selected: TimedDetectedObject[],
    currentTimeSeconds: number
): DetectedObjectDto {
    const first = selected[0];
    if (first.detectedObject.trackId == null || selected.length === 1) {
        return { ...first.detectedObject };
    }

    return projectObject(first, selected[1], currentTimeSeconds, false);
}

/**
 * Continues segment-boundary motion for the full post-buffer. Never snaps back to the
 * last stored box once extrapolation has started.
 */
function projectPostBufferObject(
    selected: TimedDetectedObject[],
    currentTimeSeconds: number
): DetectedObjectDto {
    const last = selected[selected.length - 1];
    if (last.detectedObject.trackId == null || selected.length === 1) {
        return { ...last.detectedObject };
    }

    return projectObject(selected[selected.length - 2], last, currentTimeSeconds, false);
}

function findPreviousSample(samples: TimedDetectedObject[], currentTimeSeconds: number) {
    let previous: TimedDetectedObject | null = null;
    for (const sample of samples) {
        if (sample.timeSeconds <= currentTimeSeconds) {
            previous = sample;
            continue;
        }

        break;
    }

    return previous;
}

function projectObject(
    previous: TimedDetectedObject,
    next: TimedDetectedObject,
    currentTimeSeconds: number,
    clampAlpha: boolean
): DetectedObjectDto {
    const duration = next.timeSeconds - previous.timeSeconds;
    if (duration <= 0) return { ...previous.detectedObject };

    let alpha = (currentTimeSeconds - previous.timeSeconds) / duration;
    if (clampAlpha) {
        alpha = clamp(alpha, 0, 1);
    }

    const previousBox = previous.detectedObject;
    const nextBox = next.detectedObject;
    const centerX = lerp(previousBox.x + previousBox.width / 2, nextBox.x + nextBox.width / 2, alpha);
    const centerY = lerp(previousBox.y + previousBox.height / 2, nextBox.y + nextBox.height / 2, alpha);
    const width = lerp(previousBox.width, nextBox.width, alpha);
    const height = lerp(previousBox.height, nextBox.height, alpha);
    const left = Math.floor(centerX - width / 2);
    const top = Math.floor(centerY - height / 2);
    const right = Math.ceil(centerX + width / 2);
    const bottom = Math.ceil(centerY + height / 2);
    const metadataSource = alpha < 0.5 ? previousBox : nextBox;

    return {
        ...metadataSource,
        confidence: clamp(lerp(previousBox.confidence, nextBox.confidence, alpha), 0, 1),
        selected: true,
        trackId: previousBox.trackId,
        blurShape: metadataSource.blurShape ?? previousBox.blurShape ?? nextBox.blurShape,
        x: left,
        y: top,
        width: Math.max(0, right - left),
        height: Math.max(0, bottom - top),
    };
}

function isAtSampleTime(sampleTimeSeconds: number, currentTimeSeconds: number) {
    return Math.abs(sampleTimeSeconds - currentTimeSeconds) < 0.000001;
}

function lerp(start: number, end: number, alpha: number) {
    return start + (end - start) * alpha;
}

function clamp(value: number, min: number, max: number) {
    return Math.min(max, Math.max(min, value));
}

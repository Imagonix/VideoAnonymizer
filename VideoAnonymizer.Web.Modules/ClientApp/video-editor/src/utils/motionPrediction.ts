import type { AnalyzedFrameDto, DetectedObjectDto, PreviewObject } from '../types';
import { buildObjectKey } from './keys';

type TimedDetectedObject = {
    timeSeconds: number;
    detectedObject: DetectedObjectDto;
};

export function getPredictedBlurPreviewObjects(
    frames: AnalyzedFrameDto[],
    currentTimeSeconds: number,
    timeBufferSeconds: number,
    interpolateTrackedObjects = true
): PreviewObject[] {
    if (frames.length === 0) return [];

    const sortedTimes = [...new Set(frames.map(frame => frame.timeSeconds))]
        .sort((a, b) => a - b);

    if (!interpolateTrackedObjects) {
        return getBufferedBlurPreviewObjects(frames, sortedTimes, currentTimeSeconds, timeBufferSeconds);
    }

    const samplesByKey = new Map<string, TimedDetectedObject[]>();

    for (const frame of frames) {
        for (const detectedObject of frame.detectedObjects) {
            if (!detectedObject.selected) continue;

            const key = buildObjectKey(detectedObject);
            const samples = samplesByKey.get(key) ?? [];
            samples.push({ timeSeconds: frame.timeSeconds, detectedObject });
            samplesByKey.set(key, samples);
        }
    }

    const result: PreviewObject[] = [];

    for (const samples of samplesByKey.values()) {
        const orderedSamples = [...samples].sort((a, b) => a.timeSeconds - b.timeSeconds);
        const previous = findPreviousSample(orderedSamples, currentTimeSeconds);
        if (!previous) {
            const upcoming = orderedSamples.find(sample => sample.timeSeconds > currentTimeSeconds);
            if (upcoming && isWithinPreBuffer(currentTimeSeconds, upcoming.timeSeconds, timeBufferSeconds)) {
                result.push({
                    detectedObject: { ...upcoming.detectedObject },
                    activation: 'pre',
                });
            }

            continue;
        }

        const next = orderedSamples.find(sample => sample.timeSeconds > currentTimeSeconds);
        if (next && previous.detectedObject.trackId != null) {
            result.push({
                detectedObject: interpolateObject(previous, next, currentTimeSeconds),
                activation: isAtSampleTime(previous.timeSeconds, currentTimeSeconds)
                    ? 'detected'
                    : 'interpolated',
            });
            continue;
        }

        const coverageEnd = getCoverageEnd(sortedTimes, previous.timeSeconds, timeBufferSeconds);
        if (currentTimeSeconds >= previous.timeSeconds && currentTimeSeconds < coverageEnd) {
            result.push({
                detectedObject: { ...previous.detectedObject },
                activation: isAtSampleTime(previous.timeSeconds, currentTimeSeconds)
                    ? 'detected'
                    : 'post',
            });
        }
    }

    return result;
}

function getBufferedBlurPreviewObjects(
    frames: AnalyzedFrameDto[],
    sortedTimes: number[],
    currentTimeSeconds: number,
    timeBufferSeconds: number
): PreviewObject[] {
    const framesByTime = new Map<number, AnalyzedFrameDto[]>();
    for (const frame of frames) {
        const existing = framesByTime.get(frame.timeSeconds) ?? [];
        existing.push(frame);
        framesByTime.set(frame.timeSeconds, existing);
    }

    const result = new Map<string, PreviewObject>();

    for (let index = sortedTimes.length - 1; index >= 0; index--) {
        const analyzedTime = sortedTimes[index];
        const nextTime = index + 1 < sortedTimes.length
            ? sortedTimes[index + 1]
            : Number.POSITIVE_INFINITY;
        const coverageEnd = nextTime + timeBufferSeconds;

        if (currentTimeSeconds < analyzedTime || currentTimeSeconds >= coverageEnd) {
            continue;
        }

        const framesAtTime = framesByTime.get(analyzedTime) ?? [];
        for (const frame of framesAtTime) {
            for (const detectedObject of frame.detectedObjects) {
                if (!detectedObject.selected) continue;

                const key = buildObjectKey(detectedObject);
                if (!result.has(key)) {
                    result.set(key, {
                        detectedObject: { ...detectedObject },
                        activation: isAtSampleTime(analyzedTime, currentTimeSeconds)
                            ? 'detected'
                            : 'post',
                    });
                }
            }
        }
    }

    return [...result.values()];
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

function getCoverageEnd(
    sortedTimes: number[],
    analyzedTimeSeconds: number,
    timeBufferSeconds: number
) {
    const nextAnalyzedTime = sortedTimes.find(time => time > analyzedTimeSeconds);
    return nextAnalyzedTime == null
        ? Number.POSITIVE_INFINITY
        : nextAnalyzedTime + timeBufferSeconds;
}

function isWithinPreBuffer(
    currentTimeSeconds: number,
    analyzedTimeSeconds: number,
    timeBufferSeconds: number
) {
    return timeBufferSeconds > 0
        && currentTimeSeconds >= analyzedTimeSeconds - timeBufferSeconds
        && currentTimeSeconds < analyzedTimeSeconds;
}

function interpolateObject(
    previous: TimedDetectedObject,
    next: TimedDetectedObject,
    currentTimeSeconds: number
): DetectedObjectDto {
    const duration = next.timeSeconds - previous.timeSeconds;
    if (duration <= 0) return { ...previous.detectedObject };

    const alpha = clamp((currentTimeSeconds - previous.timeSeconds) / duration, 0, 1);
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
        confidence: lerp(previousBox.confidence, nextBox.confidence, alpha),
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

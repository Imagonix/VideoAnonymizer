import type { AnalyzedFrameDto, DetectedObjectDto, PreviewObject } from '../types';
import { buildObjectKey } from './keys';

type TimedDetectedObject = {
    timeSeconds: number;
    detectedObject: DetectedObjectDto;
};

export function getPredictedBlurPreviewObjects(
    frames: AnalyzedFrameDto[],
    currentTimeSeconds: number,
    timeBufferSeconds: number
): PreviewObject[] {
    if (frames.length === 0) return [];

    const sortedTimes = [...new Set(frames.map(frame => frame.timeSeconds))]
        .sort((a, b) => a - b);

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
                    detectedObject: projectPreBufferObject(orderedSamples, upcoming, currentTimeSeconds),
                    activation: 'pre',
                });
            }

            continue;
        }

        const next = orderedSamples.find(sample => sample.timeSeconds > currentTimeSeconds);
        if (next && previous.detectedObject.trackId != null) {
            result.push({
                detectedObject: projectObject(previous, next, currentTimeSeconds, true),
                activation: isAtSampleTime(previous.timeSeconds, currentTimeSeconds)
                    ? 'detected'
                    : 'interpolated',
            });
            continue;
        }

        const coverageEnd = getCoverageEnd(sortedTimes, previous.timeSeconds, timeBufferSeconds);
        if (currentTimeSeconds >= previous.timeSeconds && currentTimeSeconds < coverageEnd) {
            result.push({
                detectedObject: projectPostBufferObject(
                    orderedSamples,
                    previous,
                    currentTimeSeconds,
                    timeBufferSeconds),
                activation: isAtSampleTime(previous.timeSeconds, currentTimeSeconds)
                    ? 'detected'
                    : 'post',
            });
        }
    }

    return result;
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

function projectPreBufferObject(
    orderedSamples: TimedDetectedObject[],
    upcoming: TimedDetectedObject,
    currentTimeSeconds: number
) {
    if (upcoming.detectedObject.trackId == null) {
        return { ...upcoming.detectedObject };
    }

    const next = orderedSamples.find(sample => sample.timeSeconds > upcoming.timeSeconds);
    return next
        ? projectObject(upcoming, next, currentTimeSeconds, false)
        : { ...upcoming.detectedObject };
}

function projectPostBufferObject(
    orderedSamples: TimedDetectedObject[],
    previous: TimedDetectedObject,
    currentTimeSeconds: number,
    timeBufferSeconds: number
) {
    if (previous.detectedObject.trackId == null
        || timeBufferSeconds <= 0
        || currentTimeSeconds > previous.timeSeconds + timeBufferSeconds) {
        return { ...previous.detectedObject };
    }

    const prior = findPriorSample(orderedSamples, previous.timeSeconds);
    return prior
        ? projectObject(prior, previous, currentTimeSeconds, false)
        : { ...previous.detectedObject };
}

function findPriorSample(samples: TimedDetectedObject[], timeSeconds: number) {
    let prior: TimedDetectedObject | null = null;
    for (const sample of samples) {
        if (sample.timeSeconds < timeSeconds) {
            prior = sample;
            continue;
        }

        break;
    }

    return prior;
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

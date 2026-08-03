import type { DetectedObjectDto, TimelineObject } from '../types';

export type TimedOccurrence = {
  timeSeconds: number;
  detectedObject: DetectedObjectDto;
};

/**
 * Deterministic representative occurrence for identification thumbnails:
 * highest confidence, then occurrence nearest the temporal middle as tie-breaker.
 */
export function selectRepresentativeOccurrence(
  occurrences: TimedOccurrence[]
): TimedOccurrence | null {
  if (occurrences.length === 0) return null;
  if (occurrences.length === 1) return occurrences[0];

  const times = occurrences.map(o => o.timeSeconds);
  const mid = (Math.min(...times) + Math.max(...times)) / 2;

  let best = occurrences[0];
  let bestConf = best.detectedObject.confidence;
  let bestMidDist = Math.abs(best.timeSeconds - mid);

  for (const candidate of occurrences.slice(1)) {
    const conf = candidate.detectedObject.confidence;
    const midDist = Math.abs(candidate.timeSeconds - mid);

    if (conf > bestConf + 1e-9) {
      best = candidate;
      bestConf = conf;
      bestMidDist = midDist;
      continue;
    }

    if (Math.abs(conf - bestConf) <= 1e-9 && midDist < bestMidDist) {
      best = candidate;
      bestMidDist = midDist;
    }
  }

  return best;
}

export function getTimedOccurrences(timelineObject: TimelineObject): TimedOccurrence[] {
  if (timelineObject.type === 'single') {
    return [{
      timeSeconds: timelineObject.timeSeconds,
      detectedObject: timelineObject.detectedObj,
    }];
  }

  return timelineObject.occurences.map(([timeSeconds, detectedObject]) => ({
    timeSeconds,
    detectedObject,
  }));
}

export function selectRepresentativeForTimelineObject(
  timelineObject: TimelineObject
): TimedOccurrence | null {
  return selectRepresentativeOccurrence(getTimedOccurrences(timelineObject));
}

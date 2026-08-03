import { computed, onUnmounted, ref, watch, type ComputedRef, type Ref } from 'vue';
import type { TimelineObject } from '../types';
import { getTimelineKey } from '../utils/keys';
import { buildThumbnailCacheKey } from '../utils/thumbnailCrop';
import { selectRepresentativeForTimelineObject } from '../utils/representativeOccurrence';
import {
  TrackThumbnailService,
  type ThumbnailEntry,
  type TrackThumbnailServiceOptions,
} from '../services/TrackThumbnailService';
import { colorManager } from '../services/ColorManager';
import { getLabel } from '../utils/utils';

export type TrackThumbnailView = {
  timelineKey: string;
  cacheKey: string;
  objectUrl: string | null;
  status: ThumbnailEntry['status'] | 'idle';
  fallbackLabel: string;
  fallbackColor: string;
};

/**
 * Reactive wrapper around TrackThumbnailService.
 * Thumbnails are requested only for keys that callers mark visible (IntersectionObserver / selection).
 */
export function useTrackThumbnails(
  videoUrl: ComputedRef<string> | Ref<string>,
  options: TrackThumbnailServiceOptions = {}
) {
  const service = new TrackThumbnailService(options);
  const revision = ref(0);
  const visibleKeys = ref(new Set<string>());

  const unsubscribe = service.subscribe(() => {
    revision.value += 1;
  });

  watch(
    videoUrl,
    (url) => {
      service.setVideoSource(url);
      revision.value += 1;
    },
    { immediate: true }
  );

  onUnmounted(() => {
    unsubscribe();
    service.dispose();
  });

  function markVisible(timelineKey: string, visible: boolean) {
    const next = new Set(visibleKeys.value);
    if (visible) next.add(timelineKey);
    else next.delete(timelineKey);
    visibleKeys.value = next;
  }

  function requestForTimelineObject(timelineObject: TimelineObject, force = false) {
    const timelineKey = getTimelineKey(timelineObject);
    if (!force && !visibleKeys.value.has(timelineKey)) {
      return;
    }

    const representative = selectRepresentativeForTimelineObject(timelineObject);
    if (!representative) return;

    const obj = representative.detectedObject;
    const cacheKey = buildThumbnailCacheKey(
      videoUrl.value,
      timelineKey,
      obj.id,
      representative.timeSeconds
    );

    service.request({
      cacheKey,
      videoUrl: videoUrl.value,
      timeSeconds: representative.timeSeconds,
      box: {
        x: obj.x,
        y: obj.y,
        width: obj.width,
        height: obj.height,
      },
    });
  }

  function getView(timelineObject: TimelineObject): TrackThumbnailView {
    // Depend on revision so Vue re-renders when the service notifies.
    void revision.value;

    const timelineKey = getTimelineKey(timelineObject);
    const representative = selectRepresentativeForTimelineObject(timelineObject);
    const sample = representative?.detectedObject
      ?? (timelineObject.type === 'single'
        ? timelineObject.detectedObj
        : timelineObject.occurences[0]?.[1]);

    const fallbackLabel = sample ? shortFallbackLabel(getLabel(sample)) : '?';
    const fallbackColor = sample ? colorManager.getColor(sample) : 'transparent';

    if (!representative) {
      return {
        timelineKey,
        cacheKey: '',
        objectUrl: null,
        status: 'idle',
        fallbackLabel,
        fallbackColor,
      };
    }

    const cacheKey = buildThumbnailCacheKey(
      videoUrl.value,
      timelineKey,
      representative.detectedObject.id,
      representative.timeSeconds
    );
    const entry = service.get(cacheKey);

    return {
      timelineKey,
      cacheKey,
      objectUrl: entry?.status === 'ready' ? entry.objectUrl : null,
      status: entry?.status ?? 'idle',
      fallbackLabel,
      fallbackColor,
    };
  }

  function requestSelected(timelineObject: TimelineObject | null) {
    if (!timelineObject) return;
    const key = getTimelineKey(timelineObject);
    markVisible(key, true);
    requestForTimelineObject(timelineObject, true);
  }

  return {
    service,
    revision: computed(() => revision.value),
    markVisible,
    requestForTimelineObject,
    requestSelected,
    getView,
  };
}

function shortFallbackLabel(label: string): string {
  const trimmed = label.trim();
  if (!trimmed) return '?';
  // Prefer class initial + track id digits when present (e.g. "face 3" -> "F3").
  const parts = trimmed.split(/\s+/);
  if (parts.length >= 2 && /^\d+$/.test(parts[parts.length - 1])) {
    return `${parts[0].slice(0, 1).toUpperCase()}${parts[parts.length - 1]}`;
  }
  return trimmed.slice(0, 2).toUpperCase();
}

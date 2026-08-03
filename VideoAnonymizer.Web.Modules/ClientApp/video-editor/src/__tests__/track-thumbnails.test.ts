import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { nextTick, ref } from 'vue';
import { selectRepresentativeOccurrence } from '../utils/representativeOccurrence';
import { buildThumbnailCacheKey, computeCropRect } from '../utils/thumbnailCrop';
import { TrackThumbnailService } from '../services/TrackThumbnailService';
import { useTrackThumbnails } from '../composables/useTrackThumbnails';
import type { DetectedObjectDto, TimelineObject } from '../types';

function createObject(overrides: Partial<DetectedObjectDto> = {}): DetectedObjectDto {
  return {
    id: overrides.id ?? 'o1',
    confidence: overrides.confidence ?? 0.9,
    className: overrides.className ?? 'face',
    selected: overrides.selected ?? true,
    trackId: overrides.trackId ?? 1,
    x: overrides.x ?? 10,
    y: overrides.y ?? 20,
    width: overrides.width ?? 40,
    height: overrides.height ?? 50,
    analyzedFrameId: overrides.analyzedFrameId ?? 'f1',
    blurShape: overrides.blurShape ?? null,
    blurSizePercentOverride: overrides.blurSizePercentOverride ?? null,
    preBufferMsOverride: overrides.preBufferMsOverride ?? null,
    postBufferMsOverride: overrides.postBufferMsOverride ?? null,
  };
}

describe('selectRepresentativeOccurrence', () => {
  it('picks the highest-confidence occurrence', () => {
    const result = selectRepresentativeOccurrence([
      { timeSeconds: 0, detectedObject: createObject({ id: 'a', confidence: 0.5 }) },
      { timeSeconds: 1, detectedObject: createObject({ id: 'b', confidence: 0.95 }) },
      { timeSeconds: 2, detectedObject: createObject({ id: 'c', confidence: 0.7 }) },
    ]);
    expect(result?.detectedObject.id).toBe('b');
  });

  it('breaks confidence ties with the occurrence nearest the temporal middle', () => {
    const result = selectRepresentativeOccurrence([
      { timeSeconds: 0, detectedObject: createObject({ id: 'early', confidence: 0.8 }) },
      { timeSeconds: 5, detectedObject: createObject({ id: 'mid', confidence: 0.8 }) },
      { timeSeconds: 10, detectedObject: createObject({ id: 'late', confidence: 0.8 }) },
    ]);
    expect(result?.detectedObject.id).toBe('mid');
  });

  it('returns null for an empty list', () => {
    expect(selectRepresentativeOccurrence([])).toBeNull();
  });
});

describe('computeCropRect', () => {
  it('pads the box and clamps to video bounds', () => {
    const crop = computeCropRect({ x: 10, y: 20, width: 100, height: 50 }, 200, 100, 0.1);
    // padX=10, padY=5 → x=0,y=15,w=120,h=60 then clamp height to 85 remaining? y=15 height=50+10=60 → 15+60=75 ok
    expect(crop.x).toBe(0);
    expect(crop.y).toBe(15);
    expect(crop.width).toBe(120);
    expect(crop.height).toBe(60);
  });

  it('clamps crops that extend past the right and bottom edges', () => {
    const crop = computeCropRect({ x: 180, y: 80, width: 40, height: 40 }, 200, 100, 0.15);
    expect(crop.x + crop.width).toBeLessThanOrEqual(200);
    expect(crop.y + crop.height).toBeLessThanOrEqual(100);
    expect(crop.width).toBeGreaterThan(0);
    expect(crop.height).toBeGreaterThan(0);
  });

  it('handles boxes already outside the frame without throwing', () => {
    const crop = computeCropRect({ x: -50, y: -50, width: 10, height: 10 }, 100, 80, 0.15);
    expect(crop.x).toBeGreaterThanOrEqual(0);
    expect(crop.y).toBeGreaterThanOrEqual(0);
    expect(crop.x + crop.width).toBeLessThanOrEqual(100);
    expect(crop.y + crop.height).toBeLessThanOrEqual(80);
  });
});

describe('TrackThumbnailService', () => {
  const originalCreateObjectURL = URL.createObjectURL;
  const originalRevokeObjectURL = URL.revokeObjectURL;
  const originalAtob = globalThis.atob;

  beforeEach(() => {
    vi.stubGlobal('URL', {
      ...URL,
      createObjectURL: vi.fn(() => 'blob:thumb-1'),
      revokeObjectURL: vi.fn(),
    });
    vi.stubGlobal('atob', (value: string) => value);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    URL.createObjectURL = originalCreateObjectURL;
    URL.revokeObjectURL = originalRevokeObjectURL;
    globalThis.atob = originalAtob;
  });

  function createMockVideo() {
    const listeners = new Map<string, Set<EventListener>>();
    const video = {
      muted: false,
      playsInline: false,
      preload: '',
      crossOrigin: '',
      src: '',
      readyState: 0,
      videoWidth: 200,
      videoHeight: 100,
      currentTime: 0,
      addEventListener(type: string, handler: EventListener) {
        if (!listeners.has(type)) listeners.set(type, new Set());
        listeners.get(type)!.add(handler);
      },
      removeEventListener(type: string, handler: EventListener) {
        listeners.get(type)?.delete(handler);
      },
      load() {
        this.readyState = 4;
        queueMicrotask(() => {
          listeners.get('loadedmetadata')?.forEach(fn => fn(new Event('loadedmetadata')));
        });
      },
      removeAttribute() {
        this.src = '';
      },
    } as unknown as HTMLVideoElement & { readyState: number };

    Object.defineProperty(video, 'currentTime', {
      configurable: true,
      get() {
        return (this as any)._t ?? 0;
      },
      set(value: number) {
        (this as any)._t = value;
        this.readyState = 4;
        queueMicrotask(() => {
          listeners.get('seeked')?.forEach(fn => fn(new Event('seeked')));
        });
      },
    });

    return video;
  }

  function createMockCanvas() {
    return {
      width: 0,
      height: 0,
      getContext: () => ({
        clearRect: vi.fn(),
        drawImage: vi.fn(),
      }),
      toDataURL: () => 'data:image/jpeg;base64,QQ==',
    } as unknown as HTMLCanvasElement;
  }

  it('queues jobs sequentially and caches the ready result', async () => {
    const service = new TrackThumbnailService({
      createVideo: () => createMockVideo(),
      createCanvas: () => createMockCanvas(),
    });

    service.setVideoSource('http://example.com/v.mp4');
    const key = buildThumbnailCacheKey('http://example.com/v.mp4', 'track-1', 'o1', 1.5);

    service.request({
      cacheKey: key,
      videoUrl: 'http://example.com/v.mp4',
      timeSeconds: 1.5,
      box: { x: 10, y: 10, width: 40, height: 40 },
    });

    expect(service.get(key)?.status).toBe('pending');

    await vi.waitFor(() => {
      expect(service.get(key)?.status).toBe('ready');
    });

    expect(service.get(key)).toEqual({ status: 'ready', objectUrl: 'blob:thumb-1' });

    // Second request should hit cache and not re-queue.
    service.request({
      cacheKey: key,
      videoUrl: 'http://example.com/v.mp4',
      timeSeconds: 1.5,
      box: { x: 10, y: 10, width: 40, height: 40 },
    });
    expect(service.getQueueLength()).toBe(0);

    service.dispose();
  });

  it('falls back when canvas extraction fails', async () => {
    const service = new TrackThumbnailService({
      createVideo: () => createMockVideo(),
      createCanvas: () => ({
        width: 0,
        height: 0,
        getContext: () => null,
        toDataURL: () => {
          throw new Error('security');
        },
      }) as unknown as HTMLCanvasElement,
    });

    const key = 'v|track-1|o1|0.000';
    service.request({
      cacheKey: key,
      videoUrl: 'http://example.com/v.mp4',
      timeSeconds: 0,
      box: { x: 0, y: 0, width: 20, height: 20 },
    });

    await vi.waitFor(() => {
      expect(service.get(key)?.status).toBe('fallback');
    });

    service.dispose();
  });

  it('invalidates cache when the video source changes', async () => {
    const revoke = vi.fn();
    vi.stubGlobal('URL', {
      ...URL,
      createObjectURL: vi.fn(() => 'blob:old'),
      revokeObjectURL: revoke,
    });

    const service = new TrackThumbnailService({
      createVideo: () => createMockVideo(),
      createCanvas: () => createMockCanvas(),
    });

    const key = buildThumbnailCacheKey('http://example.com/a.mp4', 'track-1', 'o1', 0);
    service.setVideoSource('http://example.com/a.mp4');
    service.request({
      cacheKey: key,
      videoUrl: 'http://example.com/a.mp4',
      timeSeconds: 0,
      box: { x: 0, y: 0, width: 20, height: 20 },
    });

    await vi.waitFor(() => {
      expect(service.get(key)?.status).toBe('ready');
    });

    service.setVideoSource('http://example.com/b.mp4');
    expect(service.get(key)).toBeUndefined();
    expect(revoke).toHaveBeenCalledWith('blob:old');
    expect(service.getCachedKeys()).toEqual([]);

    service.dispose();
  });
});

describe('useTrackThumbnails lazy visibility', () => {
  it('does not request thumbnails for non-visible rows until marked visible', async () => {
    const { mount } = await import('@vue/test-utils');
    const { defineComponent, h } = await import('vue');
    const requestSpy = vi.fn();

    let api: ReturnType<typeof useTrackThumbnails> | null = null;

    const Host = defineComponent({
      setup() {
        const videoUrl = ref('http://example.com/v.mp4');
        api = useTrackThumbnails(videoUrl, {
          createVideo: () => document.createElement('video'),
          createCanvas: () => document.createElement('canvas'),
        });
        api.service.request = requestSpy as any;
        return () => h('div');
      },
    });

    const wrapper = mount(Host);
    expect(api).toBeTruthy();

    const row: TimelineObject = {
      type: 'tracked',
      occurences: [
        [0, createObject({ id: 'o1', trackId: 3, confidence: 0.4 })],
        [1, createObject({ id: 'o2', trackId: 3, confidence: 0.9 })],
      ],
    };

    api!.requestForTimelineObject(row);
    expect(requestSpy).not.toHaveBeenCalled();

    api!.markVisible('track-3', true);
    api!.requestForTimelineObject(row);
    expect(requestSpy).toHaveBeenCalledTimes(1);
    expect(requestSpy.mock.calls[0][0].timeSeconds).toBe(1);
    expect(requestSpy.mock.calls[0][0].box).toMatchObject({ width: 40, height: 50 });

    const view = api!.getView(row);
    expect(view.fallbackLabel).toBeTruthy();
    expect(view.fallbackColor).toContain('hsl');

    // Force selected request bypasses visibility gate.
    requestSpy.mockClear();
    api!.markVisible('track-3', false);
    api!.requestSelected(row);
    expect(requestSpy).toHaveBeenCalledTimes(1);

    await nextTick();
    wrapper.unmount();
  });
});

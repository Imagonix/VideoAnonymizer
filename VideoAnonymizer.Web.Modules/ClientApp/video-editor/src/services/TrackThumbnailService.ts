import { computeCropRect, type BoxRect } from '../utils/thumbnailCrop';

export type ThumbnailJob = {
  cacheKey: string;
  videoUrl: string;
  timeSeconds: number;
  box: BoxRect;
  outputSize?: number;
};

export type ThumbnailEntry =
  | { status: 'pending' }
  | { status: 'ready'; objectUrl: string }
  | { status: 'fallback'; reason: string };

export type TrackThumbnailServiceOptions = {
  createVideo?: () => HTMLVideoElement;
  createCanvas?: () => HTMLCanvasElement;
  seekTimeoutMs?: number;
};

/**
 * Lazy identification thumbnails using one detached video element + canvas.
 * Never touches the main playback video element.
 */
export class TrackThumbnailService {
  private readonly createVideo: () => HTMLVideoElement;
  private readonly createCanvas: () => HTMLCanvasElement;
  private readonly seekTimeoutMs: number;

  private video: HTMLVideoElement | null = null;
  private canvas: HTMLCanvasElement | null = null;
  private currentVideoUrl: string | null = null;
  private loadedUrl: string | null = null;

  private readonly cache = new Map<string, ThumbnailEntry>();
  private readonly queue: ThumbnailJob[] = [];
  private queuedKeys = new Set<string>();
  private processing = false;
  private disposed = false;
  private listeners = new Set<() => void>();

  constructor(options: TrackThumbnailServiceOptions = {}) {
    this.createVideo = options.createVideo ?? (() => document.createElement('video'));
    this.createCanvas = options.createCanvas ?? (() => document.createElement('canvas'));
    this.seekTimeoutMs = options.seekTimeoutMs ?? 8000;
  }

  subscribe(listener: () => void): () => void {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  get(cacheKey: string): ThumbnailEntry | undefined {
    return this.cache.get(cacheKey);
  }

  /** Snapshot of cache keys currently stored (for tests). */
  getCachedKeys(): string[] {
    return [...this.cache.keys()];
  }

  getQueueLength(): number {
    return this.queue.length + (this.processing ? 1 : 0);
  }

  setVideoSource(videoUrl: string): void {
    if (this.currentVideoUrl === videoUrl) return;
    this.currentVideoUrl = videoUrl;
    this.clearCache({ keepVideoElement: false });
  }

  request(job: ThumbnailJob): void {
    if (this.disposed) return;

    if (this.currentVideoUrl != null && this.currentVideoUrl !== job.videoUrl) {
      this.setVideoSource(job.videoUrl);
    } else if (this.currentVideoUrl == null) {
      this.currentVideoUrl = job.videoUrl;
    }

    const existing = this.cache.get(job.cacheKey);
    if (existing?.status === 'ready' || existing?.status === 'fallback') {
      return;
    }
    if (existing?.status === 'pending' || this.queuedKeys.has(job.cacheKey)) {
      return;
    }

    this.cache.set(job.cacheKey, { status: 'pending' });
    this.queuedKeys.add(job.cacheKey);
    this.queue.push(job);
    this.notify();
    void this.processQueue();
  }

  clearCache(options: { keepVideoElement?: boolean } = {}): void {
    for (const entry of this.cache.values()) {
      if (entry.status === 'ready') {
        URL.revokeObjectURL(entry.objectUrl);
      }
    }
    this.cache.clear();
    this.queue.length = 0;
    this.queuedKeys.clear();
    this.loadedUrl = null;

    if (!options.keepVideoElement) {
      this.teardownVideo();
    }

    this.notify();
  }

  dispose(): void {
    this.disposed = true;
    this.clearCache({ keepVideoElement: false });
    this.listeners.clear();
    this.canvas = null;
  }

  private notify() {
    for (const listener of this.listeners) {
      listener();
    }
  }

  private teardownVideo() {
    if (!this.video) return;
    try {
      this.video.removeAttribute('src');
      this.safeLoad(this.video);
    } catch {
      // ignore non-media environments (jsdom)
    }
    this.video = null;
  }

  private safeLoad(video: HTMLVideoElement) {
    try {
      video.load();
    } catch {
      // jsdom throws "Not implemented" for HTMLMediaElement.load
    }
  }

  private ensureVideo(videoUrl: string): Promise<HTMLVideoElement> {
    if (!this.video) {
      const video = this.createVideo();
      video.muted = true;
      video.playsInline = true;
      video.preload = 'auto';
      // crossOrigin must be set before src for canvas extraction.
      video.crossOrigin = 'anonymous';
      this.video = video;
    }

    if (this.loadedUrl === videoUrl && this.video.readyState >= 1) {
      return Promise.resolve(this.video);
    }

    const video = this.video;
    video.crossOrigin = 'anonymous';

    return new Promise((resolve, reject) => {
      let settled = false;
      const finish = (fn: () => void) => {
        if (settled) return;
        settled = true;
        clearTimeout(timeoutId);
        video.removeEventListener('loadedmetadata', onLoaded);
        video.removeEventListener('error', onError);
        fn();
      };

      const onLoaded = () => {
        finish(() => {
          this.loadedUrl = videoUrl;
          resolve(video);
        });
      };
      const onError = () => {
        finish(() => reject(new Error('thumbnail-video-load-failed')));
      };
      const timeoutId = setTimeout(
        () => finish(() => reject(new Error('thumbnail-video-load-timeout'))),
        this.seekTimeoutMs
      );

      video.addEventListener('loadedmetadata', onLoaded);
      video.addEventListener('error', onError);

      if (video.src !== videoUrl) {
        try {
          video.src = videoUrl;
          this.safeLoad(video);
        } catch {
          finish(() => reject(new Error('thumbnail-video-load-failed')));
          return;
        }
      } else if (video.readyState >= 1) {
        finish(() => {
          this.loadedUrl = videoUrl;
          resolve(video);
        });
      } else {
        this.safeLoad(video);
      }
    });
  }

  private seekTo(video: HTMLVideoElement, timeSeconds: number): Promise<void> {
    return new Promise((resolve, reject) => {
      const target = Math.max(0, timeSeconds);
      let settled = false;

      const finish = (fn: () => void) => {
        if (settled) return;
        settled = true;
        clearTimeout(timeoutId);
        video.removeEventListener('seeked', onSeeked);
        video.removeEventListener('error', onError);
        fn();
      };

      const onSeeked = () => finish(() => resolve());
      const onError = () => finish(() => reject(new Error('thumbnail-seek-failed')));
      const timeoutId = setTimeout(
        () => finish(() => reject(new Error('thumbnail-seek-timeout'))),
        this.seekTimeoutMs
      );

      video.addEventListener('seeked', onSeeked);
      video.addEventListener('error', onError);

      if (Math.abs(video.currentTime - target) < 0.001 && video.readyState >= 2) {
        finish(() => resolve());
        return;
      }

      try {
        video.currentTime = target;
      } catch (error) {
        finish(() => reject(error instanceof Error ? error : new Error('thumbnail-seek-failed')));
      }
    });
  }

  private extractFrame(video: HTMLVideoElement, box: BoxRect, outputSize: number): string {
    const crop = computeCropRect(box, video.videoWidth || 0, video.videoHeight || 0);
    if (crop.width <= 0 || crop.height <= 0) {
      throw new Error('thumbnail-empty-crop');
    }

    if (!this.canvas) {
      this.canvas = this.createCanvas();
    }
    const canvas = this.canvas;
    canvas.width = outputSize;
    canvas.height = outputSize;

    const ctx = canvas.getContext('2d');
    if (!ctx) {
      throw new Error('thumbnail-canvas-unavailable');
    }

    // Identification-only crop — never draw blur previews into thumbnails.
    ctx.clearRect(0, 0, outputSize, outputSize);
    ctx.drawImage(
      video,
      crop.x,
      crop.y,
      crop.width,
      crop.height,
      0,
      0,
      outputSize,
      outputSize
    );

    // Prefer object URLs for cache lifecycle control; fall back to data URL if blob fails.
    try {
      const dataUrl = canvas.toDataURL('image/jpeg', 0.85);
      // Convert data URL to blob URL for consistent revoke semantics in clearCache.
      const binary = atob(dataUrl.split(',')[1] ?? '');
      const bytes = new Uint8Array(binary.length);
      for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
      const blob = new Blob([bytes], { type: 'image/jpeg' });
      return URL.createObjectURL(blob);
    } catch (error) {
      // Likely a SecurityError from tainted canvas (cross-origin without CORS).
      throw error instanceof Error ? error : new Error('thumbnail-canvas-tainted');
    }
  }

  private async processQueue(): Promise<void> {
    if (this.processing || this.disposed) return;
    this.processing = true;

    try {
      while (this.queue.length > 0 && !this.disposed) {
        const job = this.queue.shift()!;
        this.queuedKeys.delete(job.cacheKey);

        const existing = this.cache.get(job.cacheKey);
        if (existing?.status === 'ready') {
          continue;
        }

        try {
          const video = await this.ensureVideo(job.videoUrl);
          await this.seekTo(video, job.timeSeconds);
          const objectUrl = this.extractFrame(video, job.box, job.outputSize ?? 64);
          this.cache.set(job.cacheKey, { status: 'ready', objectUrl });
        } catch (error) {
          const reason = error instanceof Error ? error.message : 'thumbnail-failed';
          this.cache.set(job.cacheKey, { status: 'fallback', reason });
        }

        this.notify();
      }
    } finally {
      this.processing = false;
      if (this.queue.length > 0 && !this.disposed) {
        void this.processQueue();
      }
    }
  }
}

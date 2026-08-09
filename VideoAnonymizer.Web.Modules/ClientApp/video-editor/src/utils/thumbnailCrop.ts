export type BoxRect = {
  x: number;
  y: number;
  width: number;
  height: number;
};

/**
 * Crop the detection box with modest padding, clamped to video bounds.
 * Identification only — no blur enlargement.
 */
export function computeCropRect(
  box: BoxRect,
  videoWidth: number,
  videoHeight: number,
  paddingRatio = 0.15
): BoxRect {
  const safeVideoWidth = Math.max(0, videoWidth);
  const safeVideoHeight = Math.max(0, videoHeight);

  if (safeVideoWidth <= 0 || safeVideoHeight <= 0) {
    return { x: 0, y: 0, width: 0, height: 0 };
  }

  const padX = Math.max(0, box.width) * paddingRatio;
  const padY = Math.max(0, box.height) * paddingRatio;

  let x = box.x - padX;
  let y = box.y - padY;
  let width = box.width + padX * 2;
  let height = box.height + padY * 2;

  // Clamp into video bounds while preserving as much of the padded region as possible.
  if (x < 0) {
    width += x;
    x = 0;
  }
  if (y < 0) {
    height += y;
    y = 0;
  }
  if (x + width > safeVideoWidth) {
    width = safeVideoWidth - x;
  }
  if (y + height > safeVideoHeight) {
    height = safeVideoHeight - y;
  }

  width = Math.max(0, width);
  height = Math.max(0, height);

  // Degenerate boxes: fall back to a small centered clamp or full frame corner pixel.
  if (width < 1 || height < 1) {
    const fallback = Math.min(32, safeVideoWidth, safeVideoHeight);
    x = Math.min(Math.max(0, box.x), Math.max(0, safeVideoWidth - fallback));
    y = Math.min(Math.max(0, box.y), Math.max(0, safeVideoHeight - fallback));
    width = Math.min(fallback, safeVideoWidth - x);
    height = Math.min(fallback, safeVideoHeight - y);
  }

  return {
    x: Math.floor(x),
    y: Math.floor(y),
    width: Math.max(1, Math.floor(width)),
    height: Math.max(1, Math.floor(height)),
  };
}

export function buildThumbnailCacheKey(
  videoUrl: string,
  timelineKey: string,
  occurrenceId: string,
  timeSeconds: number
): string {
  return `${videoUrl}|${timelineKey}|${occurrenceId}|${timeSeconds.toFixed(3)}`;
}

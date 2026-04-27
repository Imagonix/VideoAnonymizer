export type TimelineTick = {
  time: number;
  left: string;
  label: string;
};

const tickIntervals = [
  0.1, 0.2, 0.5,
  1, 2, 5, 10, 15, 30,
  60, 120, 300, 600, 900, 1800,
  3600, 7200
];

export function clamp(value: number, min: number, max: number) {
  return Math.max(min, Math.min(max, value));
}

export function formatTimelineTime(value: number, forceTenths = true) {
  const safeValue = Math.max(0, Number.isFinite(value) ? value : 0);
  const wholeSeconds = Math.floor(safeValue);
  const tenths = Math.floor((safeValue - wholeSeconds) * 10);
  const hours = Math.floor(wholeSeconds / 3600);
  const minutes = Math.floor((wholeSeconds % 3600) / 60);
  const seconds = wholeSeconds % 60;
  const secondsText = seconds.toString().padStart(2, '0');

  if (hours > 0) {
    const minutesText = minutes.toString().padStart(2, '0');
    return forceTenths
      ? `${hours}:${minutesText}:${secondsText}.${tenths}`
      : `${hours}:${minutesText}:${secondsText}`;
  }

  return forceTenths
    ? `${minutes}:${secondsText}.${tenths}`
    : `${minutes}:${secondsText}`;
}

export function buildTimelineTicks(duration: number, pixelsPerSecond: number): TimelineTick[] {
  if (!duration || duration <= 0 || pixelsPerSecond <= 0) {
    return [];
  }

  const targetSpacing = 88;
  const interval = tickIntervals.find(x => x * pixelsPerSecond >= targetSpacing)
    ?? tickIntervals[tickIntervals.length - 1];
  const result: TimelineTick[] = [];
  const maxTicks = 500;

  for (let time = 0, index = 0; time <= duration && index < maxTicks; time += interval, index++) {
    const normalizedTime = Number(time.toFixed(3));
    result.push({
      time: normalizedTime,
      left: `${(normalizedTime / duration) * 100}%`,
      label: formatTimelineTime(normalizedTime, interval < 1)
    });
  }

  const last = result[result.length - 1];
  if (!last || Math.abs(last.time - duration) > 0.001) {
    result.push({
      time: duration,
      left: '100%',
      label: formatTimelineTime(duration)
    });
  }

  return result;
}

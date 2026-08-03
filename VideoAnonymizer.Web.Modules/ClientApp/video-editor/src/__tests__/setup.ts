import { vi } from 'vitest';

global.ResizeObserver = vi.fn().mockImplementation(() => ({
    observe: vi.fn(),
    unobserve: vi.fn(),
    disconnect: vi.fn(),
}));

HTMLVideoElement.prototype.requestVideoFrameCallback = vi.fn() as any;

// jsdom does not implement media loading. Stub load() so detached thumbnail
// videos fail closed without console noise; UI uses the class/color fallback.
HTMLVideoElement.prototype.load = vi.fn(function (this: HTMLVideoElement) {
    queueMicrotask(() => {
        this.dispatchEvent(new Event('error'));
    });
}) as any;

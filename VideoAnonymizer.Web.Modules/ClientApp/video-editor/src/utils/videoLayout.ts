export type Rect = {
    left: number;
    top: number;
    width: number;
    height: number;
};

export type Size = {
    width: number;
    height: number;
};

export type InspectorPlacement = {
    top: number;
    left: number;
    width: number;
};

/**
 * Contain-style scaling: the largest frame that fits inside the container while
 * preserving the video's intrinsic aspect ratio. The frame is centered in the
 * container; the surrounding area is letterbox.
 */
export function computeVideoFrameSize(input: {
    containerWidth: number;
    containerHeight: number;
    videoWidth: number;
    videoHeight: number;
    margin?: number;
}): Size {
    const margin = input.margin ?? 0;
    const maxWidth = Math.max(1, input.containerWidth - margin * 2);
    const maxHeight = Math.max(1, input.containerHeight - margin * 2);

    if (input.videoWidth <= 0 || input.videoHeight <= 0) {
        return { width: maxWidth, height: maxHeight };
    }

    const aspect = input.videoWidth / input.videoHeight;
    let width = maxWidth;
    let height = width / aspect;
    if (height > maxHeight) {
        height = maxHeight;
        width = height * aspect;
    }

    return { width: Math.round(width), height: Math.round(height) };
}

export function centeredRect(containerWidth: number, containerHeight: number, frame: Size): Rect {
    return {
        left: (containerWidth - frame.width) / 2,
        top: (containerHeight - frame.height) / 2,
        width: frame.width,
        height: frame.height
    };
}

function clamp(value: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, value));
}

/**
 * Positions the floating inspector with exactly two initial candidates:
 * vertically centered on the left side or vertically centered on the right side
 * of the editor stage. It prefers the side horizontally opposite the selected
 * box and is never automatically placed above or below the video. The result is
 * always clamped within the stage so the initial placement is usable without
 * dragging.
 */
export function computeInspectorPlacement(input: {
    stageWidth: number;
    stageHeight: number;
    videoRect: Rect;
    box: { x: number; y: number; width: number; height: number };
    videoWidth: number;
    videoHeight: number;
    inspectorWidth?: number;
    inspectorHeight?: number;
    margin?: number;
}): InspectorPlacement {
    const inspectorWidth = input.inspectorWidth ?? 320;
    const inspectorHeight = input.inspectorHeight ?? 210;
    const margin = input.margin ?? 8;

    const scaleX = input.videoRect.width / Math.max(1, input.videoWidth);
    const boxCenterX = input.videoRect.left + (input.box.x + input.box.width / 2) * scaleX;

    const boxOnRightHalf = boxCenterX >= input.stageWidth / 2;
    const left = boxOnRightHalf
        ? margin
        : input.stageWidth - inspectorWidth - margin;

    const top = (input.stageHeight - inspectorHeight) / 2;

    return {
        top: clamp(top, margin, Math.max(margin, input.stageHeight - inspectorHeight - margin)),
        left: clamp(left, margin, Math.max(margin, input.stageWidth - inspectorWidth - margin)),
        width: inspectorWidth
    };
}

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
 * Positions the floating inspector inside the stage on the side opposite the
 * selected box. The inspector prefers available letterbox space, falls back to
 * overlaying the video away from the box, and is always clamped within the stage.
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
    const inspectorWidth = input.inspectorWidth ?? 240;
    const inspectorHeight = input.inspectorHeight ?? 210;
    const margin = input.margin ?? 8;

    const scaleX = input.videoRect.width / Math.max(1, input.videoWidth);
    const scaleY = input.videoRect.height / Math.max(1, input.videoHeight);
    const boxLeft = input.videoRect.left + input.box.x * scaleX;
    const boxTop = input.videoRect.top + input.box.y * scaleY;
    const boxRight = boxLeft + input.box.width * scaleX;
    const boxBottom = boxTop + input.box.height * scaleY;
    const boxCenterX = (boxLeft + boxRight) / 2;
    const boxCenterY = (boxTop + boxBottom) / 2;

    const rightSide = boxCenterX >= input.stageWidth / 2;
    const rightMargin = input.stageWidth - input.videoRect.left - input.videoRect.width;
    const leftMargin = input.videoRect.left;

    let left: number;
    if (rightSide) {
        // The box is on the right: prefer the left letterbox, else overlay left.
        left = leftMargin >= inspectorWidth + margin * 2
            ? input.videoRect.left - inspectorWidth - margin
            : Math.min(input.stageWidth - inspectorWidth - margin, input.videoRect.left + margin);
    } else {
        // The box is on the left: prefer the right letterbox, else overlay right.
        left = rightMargin >= inspectorWidth + margin * 2
            ? input.videoRect.left + input.videoRect.width + margin
            : Math.max(margin, input.videoRect.left + input.videoRect.width - inspectorWidth - margin);
    }

    const topMargin = input.videoRect.top;
    const bottomMargin = input.stageHeight - input.videoRect.top - input.videoRect.height;
    const preferTop = boxCenterY >= input.stageHeight / 2;

    let top: number;
    if (preferTop && topMargin >= inspectorHeight + margin) {
        top = Math.max(margin, input.videoRect.top - inspectorHeight - margin);
    } else if (!preferTop && bottomMargin >= inspectorHeight + margin) {
        top = input.videoRect.top + input.videoRect.height + margin;
    } else if (preferTop) {
        top = Math.max(margin, input.videoRect.top + margin);
    } else {
        top = Math.min(
            input.stageHeight - inspectorHeight - margin,
            input.videoRect.top + input.videoRect.height - inspectorHeight - margin
        );
    }

    left = clamp(left, margin, Math.max(margin, input.stageWidth - inspectorWidth - margin));
    top = clamp(top, margin, Math.max(margin, input.stageHeight - inspectorHeight - margin));

    return { top, left, width: inspectorWidth };
}

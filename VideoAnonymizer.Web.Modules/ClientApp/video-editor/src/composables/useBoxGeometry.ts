import type { ComputedRef } from 'vue';
import type { AnonymizationSettings, DetectedObjectDto } from '../types';
import { colorManager } from '../services/ColorManager';

export function useBoxGeometry(
    videoWidth: ComputedRef<number>,
    videoHeight: ComputedRef<number>,
    anonymizationSettings: ComputedRef<AnonymizationSettings>
) {
    const minBoxSize = 10;

    function clamp(value: number, min: number, max: number) {
        return Math.min(Math.max(value, min), max);
    }

    function clampPct(value: number) {
        return clamp(value, 0, 100);
    }

    function clampBox(x: number, y: number, width: number, height: number) {
        const maxWidth = Math.max(1, videoWidth.value);
        const maxHeight = Math.max(1, videoHeight.value);
        const minWidth = Math.min(minBoxSize, maxWidth);
        const minHeight = Math.min(minBoxSize, maxHeight);
        const nextWidth = clamp(Math.round(width), minWidth, maxWidth);
        const nextHeight = clamp(Math.round(height), minHeight, maxHeight);

        return {
            x: clamp(Math.round(x), 0, maxWidth - nextWidth),
            y: clamp(Math.round(y), 0, maxHeight - nextHeight),
            width: nextWidth,
            height: nextHeight,
        };
    }

    function applyClampedBox(obj: DetectedObjectDto, x: number, y: number, width: number, height: number) {
        const box = clampBox(x, y, width, height);
        obj.x = box.x;
        obj.y = box.y;
        obj.width = box.width;
        obj.height = box.height;
    }

    function getBlurPct(obj: DetectedObjectDto) {
        const scale = anonymizationSettings.value.blurSizePercent / 100;
        const cx = (obj.x + obj.width / 2) / videoWidth.value * 100;
        const cy = (obj.y + obj.height / 2) / videoHeight.value * 100;
        const ew = (obj.width * scale) / videoWidth.value * 100;
        const eh = (obj.height * scale) / videoHeight.value * 100;
        const color = colorManager.getColor(obj);
        const fill = color.replace('hsl(', 'hsla(').replace(')', ', 0.3)');
        return {
            left: `${cx - ew / 2}%`,
            top: `${cy - eh / 2}%`,
            width: `${ew}%`,
            height: `${eh}%`,
            backgroundColor: fill,
        };
    }

    function getBoxPct(obj: DetectedObjectDto) {
        return {
            left: `${(obj.x / videoWidth.value) * 100}%`,
            top: `${(obj.y / videoHeight.value) * 100}%`,
            width: `${(obj.width / videoWidth.value) * 100}%`,
            height: `${(obj.height / videoHeight.value) * 100}%`,
            borderColor: colorManager.getColor(obj),
        };
    }

    function getResizeHandleStyle(obj: DetectedObjectDto, position: string) {
        const px = (obj.x / videoWidth.value) * 100;
        const py = (obj.y / videoHeight.value) * 100;
        const pw = (obj.width / videoWidth.value) * 100;
        const ph = (obj.height / videoHeight.value) * 100;
        const size = 10;
        const corner = (left: string, top: string, cursor: string) => ({
            left,
            top,
            width: `${size}px`,
            height: `${size}px`,
            transform: 'translate(-50%, -50%)',
            cursor,
        });

        switch (position) {
            case 'n':
                return { left: `${px}%`, top: `${py}%`, width: `${pw}%`, height: `${size}px`, transform: 'translateY(-50%)', cursor: 'ns-resize' };
            case 's':
                return { left: `${px}%`, top: `${py + ph}%`, width: `${pw}%`, height: `${size}px`, transform: 'translateY(-50%)', cursor: 'ns-resize' };
            case 'w':
                return { left: `${px}%`, top: `${py}%`, width: `${size}px`, height: `${ph}%`, transform: 'translateX(-50%)', cursor: 'ew-resize' };
            case 'e':
                return { left: `${px + pw}%`, top: `${py}%`, width: `${size}px`, height: `${ph}%`, transform: 'translateX(-50%)', cursor: 'ew-resize' };
            case 'ne':
                return corner(`${px + pw}%`, `${py}%`, 'nesw-resize');
            case 'nw':
                return corner(`${px}%`, `${py}%`, 'nwse-resize');
            case 'se':
                return corner(`${px + pw}%`, `${py + ph}%`, 'nwse-resize');
            case 'sw':
                return corner(`${px}%`, `${py + ph}%`, 'nesw-resize');
            default:
                return {};
        }
    }

    return {
        minBoxSize,
        clampPct,
        clampBox,
        applyClampedBox,
        getBlurPct,
        getBoxPct,
        getResizeHandleStyle,
    };
}

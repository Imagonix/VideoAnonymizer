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

    function getEdgeHandleStyle(obj: DetectedObjectDto, position: string) {
        const px = (obj.x / videoWidth.value) * 100;
        const py = (obj.y / videoHeight.value) * 100;
        const pw = (obj.width / videoWidth.value) * 100;
        const ph = (obj.height / videoHeight.value) * 100;
        if (position === 'n') return { left: `${px}%`, top: `calc(${py}% - 3px)`, width: `${pw}%`, height: '6px' };
        if (position === 's') return { left: `${px}%`, top: `calc(${py + ph}% - 3px)`, width: `${pw}%`, height: '6px' };
        if (position === 'w') return { left: `calc(${px}% - 3px)`, top: `${py}%`, width: '6px', height: `${ph}%` };
        if (position === 'e') return { left: `calc(${px + pw}% - 3px)`, top: `${py}%`, width: '6px', height: `${ph}%` };
        return {};
    }

    return {
        minBoxSize,
        clampPct,
        clampBox,
        applyClampedBox,
        getBlurPct,
        getBoxPct,
        getEdgeHandleStyle,
    };
}

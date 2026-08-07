/** Contract values for DetectedObject.nextGapHandlingMode. */
export const GapHandlingModes = {
    Interpolate: 'Interpolate',
    UseBuffers: 'UseBuffers',
} as const;

export type GapHandlingMode =
    typeof GapHandlingModes.Interpolate | typeof GapHandlingModes.UseBuffers;

export function resolveGapHandlingMode(stored: string | null | undefined): GapHandlingMode {
    return stored === GapHandlingModes.UseBuffers
        ? GapHandlingModes.UseBuffers
        : GapHandlingModes.Interpolate;
}

export function isUseBuffersGap(stored: string | null | undefined): boolean {
    return stored === GapHandlingModes.UseBuffers;
}

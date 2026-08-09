import type { DetectedObjectDto } from '../types';

export function cloneObjects(objects: DetectedObjectDto[]): DetectedObjectDto[] {
    return JSON.parse(JSON.stringify(objects));
}

/**
 * Given a set of affected occurrences and their pre-mutation snapshots, returns the
 * subset that actually changed together with matching before-state clones, so bulk
 * updates and their undo/redo history stay in lockstep.
 */
export function getChangedObjects(
    affected: DetectedObjectDto[],
    before: DetectedObjectDto[]
): { changed: DetectedObjectDto[]; beforeState: DetectedObjectDto[] } {
    const changed: DetectedObjectDto[] = [];
    const beforeState: DetectedObjectDto[] = [];
    affected.forEach((obj, index) => {
        if (JSON.stringify(obj) !== JSON.stringify(before[index])) {
            changed.push(obj);
            beforeState.push(before[index]);
        }
    });
    return { changed, beforeState };
}

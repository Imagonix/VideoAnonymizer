import type { DetectedObjectDto } from '../types'
export function getLabel(obj: DetectedObjectDto): string {
    var label = "";
    if (obj.className != null){
        label += obj.className;
    }
    if (obj.trackId != null){
        label = addSPaceIfNeeded(label);
        label += obj.trackId
    } else {
        label = addSPaceIfNeeded(label);
        label += obj.id
    }
    return label;
}
function addSPaceIfNeeded(str: string): string{
    if (str.length > 0){
        str = str + " "
    }
    return str;
}
<script setup lang="ts">
import { ref, watchEffect, computed } from 'vue'
import type { TimelineObject } from './types';
import { colorManager } from './services/ColorManager'
import { getLabel } from './utils/utils'
import MudLikeCheckbox from './MudLikeCheckbox.vue';
import ColorDot from './ColorDot.vue';
const props = defineProps<{
    timelineObject: TimelineObject
}>()
const emit = defineEmits<{ (e: 'toggle', timelineObject: TimelineObject, checked: boolean): void }>();
function getSelectionState(obj: TimelineObject): 'checked' | 'unchecked' | 'indeterminate' {
    if (obj.type === 'single') {
        return obj.detectedObj.selected ? 'checked' : 'unchecked';
    }

    const selections = obj.occurences.map(([_, o]) => o.selected);

    const all = selections.every(x => x);
    const none = selections.every(x => !x);

    if (all) return 'checked';
    if (none) return 'unchecked';
    return 'indeterminate';
}
const checkboxRef = ref<HTMLInputElement | null>(null)
watchEffect(() => {
    if (!checkboxRef.value) return;

    checkboxRef.value.indeterminate =
        getSelectionState(props.timelineObject) === 'indeterminate';
});

function getTimelineLabel(obj: TimelineObject): string {
    if (obj.type === 'single') return getLabel(obj.detectedObj);
    if (obj.type === 'tracked') return getLabel(obj.occurences[0][1]);
    return '';
}

function getTimelineColor(obj: TimelineObject): string {
    if (obj.type === 'single') return colorManager.getColor(obj.detectedObj);
    if (obj.type === 'tracked') return colorManager.getColor(obj.occurences[0][1]);
    return '';
}
const sampleDetectedObject = computed(() => {
    if (props.timelineObject.type === 'single') {
        return props.timelineObject.detectedObj
    } else {
        return props.timelineObject.occurences[0][1]
    }
})
const selectionState = computed<'checked' | 'unchecked' | 'indeterminate'>(() => {
    if (props.timelineObject.type === 'single') {
        return props.timelineObject.detectedObj.selected ? 'checked' : 'unchecked'
    }

    const allSelected = props.timelineObject.occurences.every(x => x[1].selected === true)
    const noneSelected = props.timelineObject.occurences.every(x => x[1].selected === false)

    if (allSelected) return 'checked'
    if (noneSelected) return 'unchecked'
    return 'indeterminate'
})
const checked = computed(() => selectionState.value === 'checked')
const indeterminate = computed(() => selectionState.value === 'indeterminate')
</script>
<template>
    <div class="label-container">
        <MudLikeCheckbox :checked="checked" :indeterminate="indeterminate"
            @change="(value: boolean) => emit('toggle', timelineObject, value)">{{
                getTimelineLabel(props.timelineObject) }}</MudLikeCheckbox>
        <ColorDot :detected-object="sampleDetectedObject" :alignRight="true" />
    </div>
</template>

<style scoped>
.label-container {
    display: flex;
    align-items: center;
    height: 34px;
    margin-bottom: 12px;
}
</style>
<script setup lang="ts">
import { ref, watchEffect, computed } from 'vue'
import type { TimelineObject, SingleTimelineObject } from './types';
import { colorManager } from './services/ColorManager'
import { getLabel } from './utils/utils'
const props = defineProps<{
    timelineObject: TimelineObject
}>()
const emit = defineEmits<{ (e: 'toggle', timelineObject: TimelineObject, checked: boolean): void }>();
const checked = computed(() => getSelectionState(props.timelineObject) === 'checked')
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
</script>
<template>
    <div class="label-container">
        <input type="checkbox" :checked="checked" ref="checkboxRef"
            @change="emit('toggle', timelineObject, ($event.target as HTMLInputElement).checked)" />
        <span>{{ getTimelineLabel(props.timelineObject) }}</span>
        <div class="color-dot" :style="{ background: getTimelineColor(props.timelineObject) }"></div>
    </div>
</template>

<style scoped>
.label-container {
    display: flex;
    align-items: center;
    height: 34px;
    margin-bottom: 12px;
}

.color-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    margin-left: auto;

}

.checkbox {
    accent-color: var(--mud-palette-primary);
    width: 16px;
    height: 16px;
}
</style>
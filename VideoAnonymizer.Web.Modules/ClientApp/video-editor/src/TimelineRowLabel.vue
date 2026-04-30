<script setup lang="ts">
import { ref, computed } from 'vue'
import type { TimelineObject, EditorMode } from './types';
import { colorManager } from './services/ColorManager'
import { getLabel } from './utils/utils'
import MudLikeCheckbox from './MudLikeCheckbox.vue';
import ColorDot from './ColorDot.vue';

const props = defineProps<{
    timelineObject: TimelineObject;
    mode: EditorMode;
    mergeSelectedKeys: Set<string>;
}>()
const emit = defineEmits<{
    (e: 'toggle', timelineObject: TimelineObject, checked: boolean): void;
    (e: 'set-track-id', timelineObject: TimelineObject, trackId: number): void;
    (e: 'merge-toggle', key: string): void;
}>();

const isEditing = ref(false);
const editValue = ref('');

function getTimelineKey(obj: TimelineObject): string {
    if (obj.type === 'single') return `obj-${obj.detectedObj.id}`;
    const tid = obj.occurences[0]?.[1].trackId;
    return tid != null ? `track-${tid}` : `obj-${obj.occurences[0]?.[1].id}`;
}

const timelineKey = computed(() => getTimelineKey(props.timelineObject));
const isMergeSelected = computed(() => props.mode === 'merge' && props.mergeSelectedKeys.has(timelineKey.value));

const mergeHighlightStyle = computed(() => {
    if (!isMergeSelected.value) return {};
    const color = colorManager.getColor(sampleDetectedObject.value);
    const bg = color.replace('hsl(', 'hsla(').replace(')', ', 0.3)');
    return { backgroundColor: bg };
});

function getTimelineLabel(obj: TimelineObject): string {
    if (obj.type === 'single') return getLabel(obj.detectedObj);
    if (obj.type === 'tracked') return getLabel(obj.occurences[0][1]);
    return '';
}

function getTrackId(obj: TimelineObject): number | null {
    if (obj.type === 'single') return obj.detectedObj.trackId;
    return obj.occurences[0][1].trackId;
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

function startEdit() {
    const id = getTrackId(props.timelineObject);
    editValue.value = id != null ? String(id) : '';
    isEditing.value = true;
}

function confirmEdit() {
    const trimmed = editValue.value.trim();
    if (trimmed !== '') {
        const num = parseInt(trimmed, 10);
        if (!isNaN(num) && num > 0) {
            emit('set-track-id', props.timelineObject, num);
        }
    }
    isEditing.value = false;
}

function cancelEdit() {
    isEditing.value = false;
}

function onRowClick() {
    if (props.mode === 'merge') {
        emit('merge-toggle', timelineKey.value);
    }
}
</script>
<template>
    <div
      class="label-container"
      :class="{ 'label-container--merge-mode': mode === 'merge' }"
      :style="mergeHighlightStyle"
      @click="onRowClick"
    >
        <MudLikeCheckbox
          :checked="checked"
          :indeterminate="indeterminate"
          :disabled="mode === 'merge'"
          @change="(value: boolean) => emit('toggle', timelineObject, value)"
        >
          <span v-if="!isEditing" class="label-text" @dblclick.stop="startEdit">
            {{ getTimelineLabel(props.timelineObject) }}
          </span>
          <input
            v-else
            v-model="editValue"
            class="track-id-input"
            placeholder="Track ID"
            @keydown.enter="confirmEdit"
            @keydown.escape="cancelEdit"
            @blur="confirmEdit"
            @click.stop
          />
        </MudLikeCheckbox>
        <ColorDot :detected-object="sampleDetectedObject" :alignRight="true" />
    </div>
</template>

<style scoped>
.label-container {
    display: flex;
    align-items: center;
    height: 34px;
    margin-bottom: 12px;
    padding: 0 4px;
    border-radius: 4px;
    transition: background 0.1s;
}

.label-container--merge-mode {
    cursor: pointer;
}

.label-container--merge-mode:hover {
    filter: brightness(1.3);
}

.label-text {
    cursor: text;
    border-bottom: 1px dashed transparent;
    transition: border-color 0.15s;
}

.label-text:hover {
    border-bottom-color: var(--mud-palette-action-default);
}

.track-id-input {
    background: var(--mud-palette-background);
    border: 1px solid var(--mud-palette-lines-inputs);
    border-radius: 3px;
    padding: 2px 6px;
    color: var(--mud-palette-text-primary);
    font-size: 0.85rem;
    outline: none;
    width: 80px;
}

.track-id-input:focus {
    border-color: var(--mud-palette-primary);
}
</style>

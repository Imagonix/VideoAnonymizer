import { ref, computed } from 'vue';

export type EditorMode = 'select' | 'merge' | 'split' | 'adjust' | 'add';

export function useEditorModes() {
    const activeMode = ref<EditorMode>('select');

    function activate(mode: EditorMode) {
        activeMode.value = mode;
    }

    function deactivate() {
        activeMode.value = 'select';
    }

    const isSelect = computed(() => activeMode.value === 'select');
    const isMerge = computed(() => activeMode.value === 'merge');
    const isSplit = computed(() => activeMode.value === 'split');
    const isAdjust = computed(() => activeMode.value === 'adjust');
    const isAdd = computed(() => activeMode.value === 'add');

    return {
        activeMode,
        activate,
        deactivate,
        isSelect,
        isMerge,
        isSplit,
        isAdjust,
        isAdd,
    };
}

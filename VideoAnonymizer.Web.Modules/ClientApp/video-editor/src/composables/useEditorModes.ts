import { ref, computed } from 'vue';

export type EditorMode = 'select' | 'merge' | 'split' | 'move' | 'resize' | 'add';

export function useEditorModes() {
    const activeMode = ref<EditorMode>('select');

    function activate(mode: EditorMode) {
        activeMode.value = mode;
    }

    function deactivate() {
        activeMode.value = 'select';
    }

    const isMerge = computed(() => activeMode.value === 'merge');
    const isSplit = computed(() => activeMode.value === 'split');
    const isMove = computed(() => activeMode.value === 'move');
    const isResize = computed(() => activeMode.value === 'resize');
    const isAdd = computed(() => activeMode.value === 'add');
    const isOverlayOpen = computed(() =>
        activeMode.value === 'move' || activeMode.value === 'resize' || activeMode.value === 'add'
    );

    return {
        activeMode,
        activate,
        deactivate,
        isMerge,
        isSplit,
        isMove,
        isResize,
        isAdd,
        isOverlayOpen,
    };
}

import { onMounted, onUnmounted } from 'vue';
import type { VideoEditorProps } from '../types';

export function useKeyboardUndoRedo(state: VideoEditorProps) {
    function onKeyDown(e: KeyboardEvent) {
        if ((e.ctrlKey || e.metaKey) && e.key === 'z') {
            e.preventDefault();
            if (e.shiftKey) {
                state.onRedo?.();
            } else {
                state.onUndo?.();
            }
        } else if ((e.ctrlKey || e.metaKey) && e.key === 'y') {
            e.preventDefault();
            state.onRedo?.();
        }
    }

    onMounted(() => document.addEventListener('keydown', onKeyDown));
    onUnmounted(() => document.removeEventListener('keydown', onKeyDown));
}

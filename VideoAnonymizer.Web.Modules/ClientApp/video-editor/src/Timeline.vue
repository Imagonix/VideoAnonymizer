<script setup lang="ts">
    import { ref } from 'vue';
    import PlaybackIndicator from './PlaybackIndicator.vue';

    const props = defineProps<{
        duration: number;
        currentTime: number;
    }>();

    const emit = defineEmits<{
        (e: 'seek', time: number): void;
    }>();

    const trackRef = ref<HTMLElement | null>(null);

    function onClick(e: MouseEvent) {
        if (!trackRef.value || props.duration <= 0) return;

        const rect = trackRef.value.getBoundingClientRect();
        const ratio = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
        emit('seek', ratio * props.duration);
    }
</script>

<template>
    <div class="timeline" data-testid="timeline" ref="trackRef" @click="onClick">
        <PlaybackIndicator :time="currentTime" :duration="duration" />
        <slot />
    </div>
</template>

<style scoped>
    .timeline {
        border-radius: 12px;
        margin: 16px;
        position: relative;
    }

        .timeline :deep(.timeline-row) {
            margin-bottom: 12px;
        }
</style>
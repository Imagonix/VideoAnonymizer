<script setup lang="ts">
    import { computed } from 'vue';

    const props = defineProps<{
        time: number;
        duration: number;
    }>();

    const left = computed(() => {
        if (!props.duration || props.duration <= 0) return '0%';
        return `${(props.time / props.duration) * 100}%`;
    });
</script>

<template>
    <div class="playback-indicator"
         data-testid="playback-indicator"
         :style="{ left }">
        <div class="playback-indicator-line"></div>
        <div class="playback-timestamp" data-testid="playback-timestamp">
            {{ time.toFixed(1) }}s
        </div>
    </div>
</template>

<style scoped>
    .playback-indicator {
        position: absolute;
        top: 0;
        bottom: 0;
        transform: translateX(-50%);
        pointer-events: none;
        z-index: 10;
    }

    .playback-indicator-line {
        position: absolute;
        top: 0;
        bottom: 0;
        left: 50%;
        width: 2px;
        transform: translateX(-50%);
        background: #ef4444;
    }

    .playback-timestamp {
        position: absolute;
        top: -1.5rem;
        left: 50%;
        transform: translateX(-50%);
        white-space: nowrap;
    }
</style>
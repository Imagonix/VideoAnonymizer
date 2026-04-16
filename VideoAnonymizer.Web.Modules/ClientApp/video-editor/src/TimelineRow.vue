<script setup lang="ts">
import { ref } from 'vue'
defineProps<{
  objectKey: string
  color: string
  duration: number
  segments: any[]
}>()
const checked = ref(true)
</script>
<template>
    <div class="timeline-row-wrapper">
        <div class="timeline-row">
            <div class="label-container">
                <input type="checkbox" v-model="checked" class="checkbox" />
                <span>{{ objectKey }}</span>
                <div class="color-dot" :style="{ background: color }"></div>
            </div>
            <!-- <div v-if="hasContinuous"
                 class="timeline-segment-continuous"
                 data-testid="timeline-segment-continuous"
                 :style="{ backgroundColor: color }" /> -->
            <TimelineMarker v-for="s in segments"
                            :key="s.timeSeconds"
                            :time="s.timeSeconds"
                            :duration="duration"
                            :active="s.selected" />
        </div>
    </div>
</template>

<style scoped>
    .timeline-row-wrapper {
        position: relative;
    }

    .label-container{
        display: flex;
        align-items: center;
    }

    .timeline-row {
        position: relative;
        height: 34px;
        background: var(--mud-palette-surface);
        border-radius: 8px;
        overflow: hidden;
    }

    .color-dot {
        width: 10px;
        height: 10px;
        border-radius: 50%;
    }
    .checkbox {
        accent-color: var(--mud-palette-primary);
        width: 16px;
        height: 16px;
    }
</style>
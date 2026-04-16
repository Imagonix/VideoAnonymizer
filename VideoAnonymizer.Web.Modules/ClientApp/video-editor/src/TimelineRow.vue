<script setup lang="ts">
defineProps<{
  objectKey: string
  color: string
  duration: number
  segments: any[]
}>()
</script>
<template>
    <div class="timeline-row-wrapper">
        <div class="timeline-row">
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

    .timeline-row {
        position: relative;
        height: 34px;
        background: var(--mud-palette-surface);
        border-radius: 8px;
        overflow: hidden;
    }
</style>
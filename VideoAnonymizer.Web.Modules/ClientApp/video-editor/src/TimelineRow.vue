<script setup lang="ts">
const props = defineProps<{
  objectKey: string;
  color: string;
  segments: { time: number; active: boolean }[];
  duration: number;
}>();

function percent(time: number) {
  return `${(time / props.duration) * 100}%`;
}

const hasContinuous = computed(() => {
  if (props.segments.length < 2) return false;

  for (let i = 1; i < props.segments.length; i++) {
    if (Math.abs(props.segments[i].time - props.segments[i - 1].time) < 1.1) {
      return true;
    }
  }
  return false;
});
</script>

<template>
  <div class="timeline-row">
    <!-- continuous line -->
    <div
      v-if="hasContinuous"
      class="timeline-segment-continuous"
      data-testid="timeline-segment-continuous"
      :style="{ backgroundColor: color }"
    />

    <!-- markers -->
    <TimelineMarker
      v-for="s in segments"
      :key="s.time"
      :time="s.time"
      :duration="duration"
      :active="s.active"
      :color="color"
    />
  </div>
</template>
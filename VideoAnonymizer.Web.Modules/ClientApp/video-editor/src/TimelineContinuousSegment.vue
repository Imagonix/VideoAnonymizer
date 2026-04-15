<script setup lang="ts">
defineProps<{ color: string }>();
</script>

<template>
  <div
    class="timeline-segment-continuous"
    data-testid="timeline-segment-continuous"
    :style="{ backgroundColor: color }"
  />
</template>
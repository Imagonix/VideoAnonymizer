<script setup lang="ts">
import { ref } from 'vue'
defineProps<{
  objectKey: string
  color: string
}>()
const checked = ref(true)
</script>
<template>
    <div class="label-container">
        <input type="checkbox" v-model="checked" class="checkbox" />
        <span>{{ objectKey }}</span>
        <div class="color-dot" :style="{ background: color }"></div>
    </div>
</template>

<style scoped>
    .label-container{
        display: flex;
        align-items: center;
        height: 34px;
        margin-bottom: 12px;
    }
    .color-dot {
        width: 10px;
        height: 10px;
        border-radius: 50%;
        margin-left: auto;

    }
    .checkbox {
        accent-color: var(--mud-palette-primary);
        width: 16px;
        height: 16px;
    }
</style>
<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue';

const props = withDefaults(defineProps<{
  objectUrl?: string | null;
  fallbackLabel: string;
  fallbackColor: string;
  size?: number;
  ariaLabel?: string;
  /** When true, notify parent as soon as mounted (selected strip / inspector). */
  eager?: boolean;
}>(), {
  objectUrl: null,
  size: 28,
  ariaLabel: 'Track representative image',
  eager: false,
});

const emit = defineEmits<{
  (e: 'visibility-change', visible: boolean): void;
}>();

const rootRef = ref<HTMLElement | null>(null);
let observer: IntersectionObserver | null = null;
let lastVisible = false;

function emitVisibility(visible: boolean) {
  if (lastVisible === visible) return;
  lastVisible = visible;
  emit('visibility-change', visible);
}

onMounted(() => {
  if (props.eager) {
    emitVisibility(true);
    return;
  }

  if (typeof IntersectionObserver === 'undefined') {
    emitVisibility(true);
    return;
  }

  observer = new IntersectionObserver(
    (entries) => {
      const entry = entries[0];
      emitVisibility(!!entry?.isIntersecting);
    },
    { root: null, threshold: 0.01 }
  );

  if (rootRef.value) {
    observer.observe(rootRef.value);
  }
});

watch(
  () => props.eager,
  (eager) => {
    if (eager) emitVisibility(true);
  }
);

onBeforeUnmount(() => {
  observer?.disconnect();
  emitVisibility(false);
});
</script>

<template>
  <div
    ref="rootRef"
    class="track-thumbnail"
    data-testid="track-thumbnail"
    :style="{
      width: size + 'px',
      height: size + 'px',
      background: objectUrl ? 'transparent' : fallbackColor,
    }"
    :aria-label="ariaLabel"
    role="img"
  >
    <img
      v-if="objectUrl"
      class="track-thumbnail__image"
      data-testid="track-thumbnail-image"
      :src="objectUrl"
      alt=""
      draggable="false"
    />
    <span
      v-else
      class="track-thumbnail__fallback"
      data-testid="track-thumbnail-fallback"
      aria-hidden="true"
    >{{ fallbackLabel }}</span>
  </div>
</template>

<style scoped>
.track-thumbnail {
  position: relative;
  flex-shrink: 0;
  border-radius: 4px;
  border: 1px solid var(--mud-palette-lines-default);
  overflow: hidden;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: color-mix(in srgb, var(--mud-palette-primary) 18%, transparent);
}

.track-thumbnail__image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
  pointer-events: none;
}

.track-thumbnail__fallback {
  font-size: 0.65rem;
  font-weight: 700;
  line-height: 1;
  color: #fff;
  text-shadow: 0 0 2px rgba(0, 0, 0, 0.65);
  user-select: none;
}
</style>

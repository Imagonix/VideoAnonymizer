<script setup lang="ts">
import { computed, ref, watch } from 'vue'

const props = defineProps<{
  checked?: boolean
  disabled?: boolean
  indeterminate?: boolean
}>()

const emit = defineEmits<{
  (e: 'update:checked', value: boolean): void
  (e: 'change', value: boolean): void
}>()

const inputRef = ref<HTMLInputElement | null>(null)

const model = computed({
  get: () => !!props.checked,
  set: (value: boolean) => {
    emit('update:checked', value)
    emit('change', value)
  }
})

watch(
  () => props.indeterminate,
  (value) => {
    if (inputRef.value) inputRef.value.indeterminate = !!value
  },
  { immediate: true }
)
</script>

<template>
  <label
    class="mud-checkbox"
    :class="{
      'mud-checkbox--checked': checked,
      'mud-checkbox--disabled': disabled,
      'mud-checkbox--indeterminate': indeterminate
    }"
  >
    <input
      ref="inputRef"
      v-model="model"
      type="checkbox"
      class="mud-checkbox__native"
      :disabled="disabled"
    />

    <span class="mud-checkbox__box">
      <svg v-if="checked && !indeterminate" class="mud-checkbox__icon" viewBox="0 0 24 24" aria-hidden="true">
        <path
          d="M9 16.17 4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"
          fill="currentColor"
        />
      </svg>

      <span v-else-if="indeterminate" class="mud-checkbox__indeterminate"></span>
    </span>

    <span v-if="$slots.default" class="mud-checkbox__label">
      <slot />
    </span>
  </label>
</template>

<style scoped>
.mud-checkbox {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  user-select: none;
  color: var(--mud-palette-text-primary, inherit);
}

.mud-checkbox--disabled {
  cursor: default;
  opacity: 0.6;
}

.mud-checkbox__native {
  position: absolute;
  opacity: 0;
  pointer-events: none;
}

.mud-checkbox__box {
  width: 18px;
  height: 18px;
  border: 2px solid var(--mud-palette-action-default, #9e9e9e);
  border-radius: 2px;
  background: transparent;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  transition:
    background-color 0.15s ease,
    border-color 0.15s ease,
    box-shadow 0.15s ease;
  flex: 0 0 auto;
}

.mud-checkbox:hover .mud-checkbox__box {
  box-shadow: 0 0 0 6px color-mix(in srgb, var(--mud-palette-primary, #594ae2) 12%, transparent);
}

.mud-checkbox--checked .mud-checkbox__box,
.mud-checkbox--indeterminate .mud-checkbox__box {
  background: var(--mud-palette-primary, #594ae2);
  border-color: var(--mud-palette-primary, #594ae2);
}

.mud-checkbox__icon {
  width: 16px;
  height: 16px;
  color: white;
}

.mud-checkbox__indeterminate {
  width: 10px;
  height: 2px;
  background: white;
  border-radius: 1px;
}

.mud-checkbox__label {
  display: inline-flex;
  align-items: center;
}
</style>
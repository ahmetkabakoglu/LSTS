<template>
  <div class="d-flex flex-wrap align-center justify-space-between ga-3 mb-6">
    <div>
      <div class="text-h5 font-weight-bold">{{ title }}</div>
      <div v-if="subtitle" class="text-body-2 text-medium-emphasis mt-1">
        {{ subtitle }}
      </div>
    </div>

    <div class="d-flex align-center ga-2">
      <slot name="actions" />
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  title: string;
  subtitle?: string;
}>();
</script>
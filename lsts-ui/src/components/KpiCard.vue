<template>
  <v-card class="kpi" variant="tonal">
    <v-card-text class="d-flex align-start justify-space-between">
      <div>
        <div class="text-caption text-medium-emphasis">{{ label }}</div>
        <div class="text-h5 font-weight-bold mt-1">{{ value }}</div>
        <div v-if="hint" class="text-caption text-medium-emphasis mt-2">
          {{ hint }}
        </div>
      </div>

      <v-avatar size="42" :color="color" variant="flat">
        <v-icon :icon="icon" />
      </v-avatar>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
withDefaults(
  defineProps<{
    label: string;
    value: string | number;
    hint?: string;
    icon: string;
    color?: string;
  }>(),
  { color: "primary" }
);
</script>

<style scoped>
.kpi {
  border: 1px solid rgba(255, 255, 255, 0.06);
  transition: transform 140ms ease, box-shadow 140ms ease;
}
.kpi:hover {
  transform: translateY(-2px);
  box-shadow: 0 10px 30px rgba(0,0,0,.35);
}
</style>
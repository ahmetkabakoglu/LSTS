<template>
  <v-card class="rounded-xl" variant="tonal">
    <v-card-title class="text-subtitle-1 font-weight-semibold d-flex align-center">
      <v-icon icon="mdi-laptop" class="mr-2" />
      Cihaz Bilgileri
    </v-card-title>

    <v-divider />

    <v-card-text>
      <div v-for="r in topRows" :key="r.label" class="info-row">
        <div class="left">
          <v-icon :icon="r.icon" size="18" class="mr-2" />
          <div class="text-caption text-medium-emphasis">{{ r.label }}</div>
        </div>
        <div class="text-body-2 font-weight-medium value">{{ r.value }}</div>
      </div>

      <v-divider class="my-3" />

      <div v-for="r in specRows" :key="r.label" class="info-row">
        <div class="left">
          <v-icon :icon="r.icon" size="18" class="mr-2" />
          <div class="text-caption text-medium-emphasis">{{ r.label }}</div>
        </div>
        <div class="text-body-2 font-weight-medium value">{{ r.value }}</div>
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { computed } from "vue";

const props = defineProps<{
  device: {
    serialNo?: string | null;
    brandCode?: string | null;
    modelCode?: string | null;
    modelName?: string | null;
    deviceType?: string | null;
    customerMasked?: string | null;
  } | null;
  specs: {
    cpu?: string | null;
    gpu?: string | null;
    ram?: string | null;
    display?: string | null;
    storage?: string | null;
  } | null;
}>();

function val(v: any) {
  const s = v === null || v === undefined ? "" : String(v);
  return s.trim().length ? s : "—";
}

const modelLine = computed(() => {
  const code = props.device?.modelCode ?? "";
  const name = props.device?.modelName ?? "";
  if (code && name) return `${code} — ${name}`;
  return code || name || "—";
});

const topRows = computed(() => [
  { label: "Serial No", icon: "mdi-barcode", value: val(props.device?.serialNo) },
  { label: "Brand", icon: "mdi-tag", value: val(props.device?.brandCode) },
  { label: "Model", icon: "mdi-laptop", value: modelLine.value },
  { label: "Device Type", icon: "mdi-shape", value: val(props.device?.deviceType) },
  { label: "Customer", icon: "mdi-account", value: val(props.device?.customerMasked) },
]);

const specRows = computed(() => {
  const rows = [
    { label: "CPU", icon: "mdi-cpu-64-bit", value: val(props.specs?.cpu) },
    { label: "GPU", icon: "mdi-expansion-card", value: val(props.specs?.gpu) },
    { label: "RAM", icon: "mdi-memory", value: val(props.specs?.ram) },
    { label: "Display", icon: "mdi-monitor", value: val(props.specs?.display) },
  ];

  const storage = (props.specs?.storage ?? "").toString().trim();
  if (storage.length) {
    rows.push({ label: "Storage", icon: "mdi-harddisk", value: storage });
  }

  return rows;
});
</script>

<style scoped>
.info-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 9px 0;
  border-bottom: 1px dashed rgba(255, 255, 255, 0.08);
}
.info-row:last-child {
  border-bottom: none;
}
.left {
  display: flex;
  align-items: center;
  min-width: 140px;
}
.value {
  text-align: right;
  max-width: 66%;
  word-break: break-word;
}
</style>
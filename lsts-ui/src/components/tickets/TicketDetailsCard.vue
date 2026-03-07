<template>
  <v-card class="rounded-xl" variant="tonal">
    <v-card-title class="text-subtitle-1 font-weight-semibold d-flex align-center">
      <v-icon icon="mdi-clipboard-text" class="mr-2" />
      Ticket Detayları
    </v-card-title>

    <v-divider />

    <v-card-text>
      <v-row dense>
        <v-col cols="12" md="6">
          <div v-for="r in leftRows" :key="r.label" class="info-row">
            <div class="left">
              <v-icon :icon="r.icon" size="18" class="mr-2" />
              <div class="text-caption text-medium-emphasis">{{ r.label }}</div>
            </div>
            <div class="text-body-2 font-weight-medium value">
              {{ r.value }}
            </div>
          </div>
        </v-col>

        <v-col cols="12" md="6">
          <div v-for="r in rightRows" :key="r.label" class="info-row">
            <div class="left">
              <v-icon :icon="r.icon" size="18" class="mr-2" />
              <div class="text-caption text-medium-emphasis">{{ r.label }}</div>
            </div>
            <div class="text-body-2 font-weight-medium value">
              {{ r.value }}
            </div>
          </div>
        </v-col>
      </v-row>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { computed } from "vue";

const props = defineProps<{
  ticket: {
    serviceNo?: string | null;
    status?: string | null;
    statusUpdatedAt?: string | null;
    faultDesc?: string | null;
    internalNotes?: string | null;
    publicNote?: string | null;
    estimatedDeliveryDate?: string | null;
    deviceId?: number | null;
  } | null;
}>();

function dt(v?: string | null) {
  if (!v) return "—";
  const d = new Date(v);
  if (Number.isNaN(d.getTime())) return String(v);
  return new Intl.DateTimeFormat("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(d);
}
function val(v: any) {
  const s = v === null || v === undefined ? "" : String(v);
  return s.trim().length ? s : "—";
}

const leftRows = computed(() => {
  const t = props.ticket;
  return [
    { label: "Service No", icon: "mdi-pound", value: val(t?.serviceNo) },
    { label: "Arıza", icon: "mdi-alert-circle-outline", value: val(t?.faultDesc) },
    { label: "Public Note", icon: "mdi-account-voice", value: val(t?.publicNote) },
  ];
});

const rightRows = computed(() => {
  const t = props.ticket;
  return [
    { label: "Current Status", icon: "mdi-shield-sync", value: val(t?.status) },
    { label: "Status Updated", icon: "mdi-clock-outline", value: dt(t?.statusUpdatedAt) },
    { label: "Internal Notes", icon: "mdi-note-text", value: val(t?.internalNotes) },
  ];
});
</script>

<style scoped>
.info-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 0;
  border-bottom: 1px dashed rgba(255, 255, 255, 0.08);
}
.info-row:last-child {
  border-bottom: none;
}
.left {
  display: flex;
  align-items: center;
  min-width: 170px;
}
.value {
  text-align: right;
  max-width: 62%;
  word-break: break-word;
}
</style>
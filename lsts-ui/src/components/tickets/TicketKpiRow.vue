<template>
  <v-row dense>
    <v-col v-for="c in cards" :key="c.title" cols="12" md="6" lg="3">
      <v-card class="rounded-xl kpi" variant="tonal">
        <v-card-text class="d-flex align-center justify-space-between">
          <div>
            <div class="text-caption text-medium-emphasis">{{ c.title }}</div>
            <div class="text-subtitle-1 font-weight-semibold mt-1">{{ c.value }}</div>
            <div v-if="c.sub" class="text-caption text-medium-emphasis mt-1">{{ c.sub }}</div>
          </div>
          <v-avatar size="40" variant="flat" color="primary">
            <v-icon :icon="c.icon" />
          </v-avatar>
        </v-card-text>
      </v-card>
    </v-col>
  </v-row>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { getTicketStatusMeta } from "../../domain/ticketStatus";

const props = defineProps<{
  ticket: {
    status?: string | null;
    statusUpdatedAt?: string | null;
    estimatedDeliveryDate?: string | null;
    deviceId?: number | null;
  } | null;
  device: {
    modelCode?: string | null;
    serialNo?: string | null;
  } | null;
}>();

function formatDateTime(v?: string | null) {
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

const cards = computed(() => {
  const t = props.ticket;
  const d = props.device;

  const status = (t?.status ?? "").toString();
  const statusLabel = getTicketStatusMeta(status).label;

  return [
    { title: "Durum", icon: "mdi-shield-sync", value: statusLabel, sub: status || "—" },
    { title: "Son Güncelleme", icon: "mdi-clock-outline", value: formatDateTime(t?.statusUpdatedAt), sub: "statusUpdatedAt" },
    { title: "Tahmini Teslim", icon: "mdi-calendar", value: formatDateTime(t?.estimatedDeliveryDate), sub: "estimatedDeliveryDate" },
    {
      title: "Cihaz",
      icon: "mdi-laptop",
      value: d?.modelCode || "—",
      sub: d?.serialNo || (t?.deviceId ? `DeviceId: ${t.deviceId}` : "—"),
    },
  ];
});
</script>

<style scoped>
.kpi {
  border: 1px solid rgba(255, 255, 255, 0.06);
}
</style>
<template>
  <v-chip size="small" :color="chipColor" variant="tonal" class="font-weight-medium">
    <v-icon start size="16" :icon="chipIcon" />
    {{ label }}
  </v-chip>
</template>

<script setup lang="ts">
import { computed } from "vue";

const props = defineProps<{ status?: string | null }>();

const s = computed(() => (props.status ?? "").toUpperCase().trim());

const map: Record<string, { color: string; icon: string; label: string }> = {
  NEW: { color: "info", icon: "mdi-star-outline", label: "NEW" },
  RECEIVED: { color: "secondary", icon: "mdi-inbox-arrow-down", label: "RECEIVED" },
  DIAGNOSING: { color: "warning", icon: "mdi-stethoscope", label: "DIAGNOSING" },
  WAITING_APPROVAL: { color: "warning", icon: "mdi-account-clock", label: "WAITING_APPROVAL" },
  WAITING_PARTS: { color: "warning", icon: "mdi-truck-fast", label: "WAITING_PARTS" },
  IN_REPAIR: { color: "primary", icon: "mdi-tools", label: "IN_REPAIR" },
  READY_FOR_PICKUP: { color: "success", icon: "mdi-package-variant-closed", label: "READY_FOR_PICKUP" },
  DELIVERED: { color: "success", icon: "mdi-check-circle-outline", label: "DELIVERED" },
  CLOSED: { color: "success", icon: "mdi-lock-check", label: "CLOSED" },
  CANCELED: { color: "error", icon: "mdi-close-circle-outline", label: "CANCELED" },
};

const chipColor = computed(() => map[s.value]?.color ?? "grey");
const chipIcon = computed(() => map[s.value]?.icon ?? "mdi-help-circle-outline");
const label = computed(() => map[s.value]?.label ?? (props.status ?? "UNKNOWN"));
</script>
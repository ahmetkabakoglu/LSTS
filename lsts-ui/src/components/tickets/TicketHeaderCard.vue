<template>
  <v-card class="rounded-xl" variant="tonal">
    <v-card-text class="d-flex flex-column flex-md-row gap-4 align-md-center">
      <div class="d-flex align-center gap-3 flex-1">
        <v-avatar size="42" color="primary" variant="flat">
          <v-icon icon="mdi-ticket" />
        </v-avatar>

        <div>
          <div class="text-caption text-medium-emphasis">Ticket</div>
          <div class="text-h6 font-weight-semibold">
            #{{ ticketId }}
          </div>
          <div class="text-caption text-medium-emphasis" v-if="subtitle">
            {{ subtitle }}
          </div>
        </div>
      </div>

      <div class="d-flex align-center gap-2 flex-wrap">
        <v-chip :color="statusMeta.color" variant="flat" class="font-weight-medium">
          <v-icon start :icon="statusMeta.icon" />
          {{ statusMeta.label }}
        </v-chip>

        <v-chip v-if="device?.modelCode" variant="tonal">
          <v-icon start icon="mdi-laptop" />
          {{ device.modelCode }}
        </v-chip>

        <v-chip v-if="device?.serialNo" variant="tonal">
          <v-icon start icon="mdi-barcode" />
          {{ device.serialNo }}
        </v-chip>

        <v-chip v-if="device?.customerMasked" variant="tonal">
          <v-icon start icon="mdi-account" />
          {{ device.customerMasked }}
        </v-chip>
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { getTicketStatusMeta } from "../../domain/ticketStatus";

const props = defineProps<{
  ticketId: number;
  status: string;
  subtitle?: string;
  device?: {
    modelCode?: string | null;
    serialNo?: string | null;
    customerMasked?: string | null;
  } | null;
}>();

const statusMeta = computed(() => getTicketStatusMeta(props.status));
</script>

<style scoped>
.gap-2 {
  gap: 8px;
}
.gap-3 {
  gap: 12px;
}
.gap-4 {
  gap: 16px;
}
</style>
<template>
  <v-card class="rounded-xl">
    <v-card-title class="text-subtitle-1 font-weight-semibold d-flex align-center justify-space-between">
      <div class="d-flex align-center">
        <v-icon icon="mdi-shield-sync" class="mr-2" />
        Status & Notes
      </div>

      <v-chip variant="tonal" size="small">
        Actor: {{ actorUserId }}
      </v-chip>
    </v-card-title>

    <v-divider />

    <v-card-text>
      <v-row dense>
        <v-col cols="12" md="4">
          <v-select
            v-model="statusLocal"
            :items="statusOptions"
            item-title="title"
            item-value="value"
            label="Ticket Status"
            variant="outlined"
            density="comfortable"
          />
        </v-col>

        <v-col cols="12" md="8">
          <v-textarea
            v-model="notesLocal"
            label="Notes"
            variant="outlined"
            rows="2"
            auto-grow
          />
        </v-col>
      </v-row>

      <div class="d-flex justify-end gap-2">
        <v-btn variant="text" :disabled="busy" @click="reset">Sıfırla</v-btn>
        <v-btn
          color="primary"
          variant="flat"
          :loading="busy"
          @click="save"
        >
          Kaydet
        </v-btn>
      </div>

      <div v-if="hint" class="text-caption text-medium-emphasis mt-2">
        {{ hint }}
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { TICKET_STATUS_OPTIONS } from "../../domain/ticketStatus";

const props = defineProps<{
  actorUserId: number;
  status: string;
  notes?: string | null;
  busy?: boolean;
  hint?: string;
}>();

const emit = defineEmits<{
  (e: "save", payload: { status: string; notes: string }): void;
}>();

const busy = computed(() => props.busy ?? false);
const statusOptions = computed(() => TICKET_STATUS_OPTIONS);

const statusLocal = ref(props.status ?? "");
const notesLocal = ref(props.notes ?? "");

watch(
  () => props.status,
  (v) => (statusLocal.value = v ?? "")
);

watch(
  () => props.notes,
  (v) => (notesLocal.value = v ?? "")
);

function reset() {
  statusLocal.value = props.status ?? "";
  notesLocal.value = props.notes ?? "";
}

function save() {
  emit("save", { status: statusLocal.value, notes: notesLocal.value ?? "" });
}
</script>

<style scoped>
.gap-2 {
  gap: 8px;
}
</style>
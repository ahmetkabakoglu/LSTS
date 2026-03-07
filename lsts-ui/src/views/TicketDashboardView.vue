<template>
  <div class="d-flex flex-column gap-4">
    <div class="d-flex align-center justify-space-between">
      <div>
        <div class="text-caption text-medium-emphasis">TICKETS</div>
        <div class="text-h5 font-weight-semibold">Ticket Dashboard</div>
      </div>

      <div class="d-flex align-center gap-2">
        <v-btn variant="tonal" :loading="isLoading" @click="td.refresh()" :disabled="!ready">
          <v-icon start icon="mdi-refresh" />
          Yenile
        </v-btn>

        <v-chip variant="tonal" size="small">
          TicketId: {{ routeTicketId || "—" }}
        </v-chip>
      </div>
    </div>

    <v-alert v-if="errText" type="error" variant="tonal" class="rounded-xl">
      {{ errText }}
    </v-alert>

    <v-skeleton-loader
      v-if="isLoading && !dashboard"
      type="article, article, article"
      class="rounded-xl"
    />

    <template v-if="dashboard">
      <TicketKpiRow :ticket="dashboard.ticket" :device="dashboard.device || null" />
      <v-row dense>
        <v-col cols="12" lg="8" class="d-flex flex-column gap-4">
          <TicketDetailsCard :ticket="dashboard.ticket" />
          <TicketStatusNotesCard
            :actor-user-id="actorId"
            :status="dashboard.ticket.status"
            :notes="dashboard.ticket.internalNotes || ''"
            :busy="actionBusy"
            hint="Status/notes güncellemesi sonrası otomatik yenilenir."
            @save="onSaveStatusNotes"
          />
        </v-col>
        <v-col cols="12" lg="4">
          <DeviceInfoCard
            :device="dashboard.device || null"
            :specs="dashboard.modelSpecs || null"
          />
        </v-col>
      </v-row>
      <PartRequestPanel
        :requests="dashboard.partRequests"
        :busy="actionBusy"
        :search-parts="td.searchParts"
        @create="onCreatePR"
        @addItem="onAddPRItem"
        @issueItem="onIssuePRItem"
        @setStatus="onSetPRStatus"
        @cancel="onCancelPR"
        @close="onClosePR"
      />
    </template>
    <v-snackbar v-model="snack.open" :color="snack.color" timeout="2500">
      {{ snack.text }}
    </v-snackbar>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { useRoute } from "vue-router";

import TicketKpiRow from "../components/tickets/TicketKpiRow.vue";
import TicketDetailsCard from "../components/tickets/TicketDetailsCard.vue";
import DeviceInfoCard from "../components/tickets/DeviceInfoCard.vue";
import TicketStatusNotesCard from "../components/tickets/TicketStatusNotesCard.vue";
import PartRequestPanel from "../components/partRequests/PartRequestPanel.vue";

import { useTicketDashboard } from "../features/ticketDashboard/useTicketDashboard";

const route = useRoute();
const td = useTicketDashboard();

const isLoading = computed(() => td.loading.value);
const errText = computed(() => td.error.value);

const dashboard = computed(() => td.data.value);
const actorId = computed(() => td.actorUserId.value);

const actionBusy = ref(false);

const routeTicketId = computed(() => {
  const raw = route.params.ticketId;
  const n = Number(raw);
  return Number.isFinite(n) ? n : 0;
});

const ready = computed(() => routeTicketId.value > 0);

const snack = ref<{ open: boolean; text: string; color: string }>({
  open: false,
  text: "",
  color: "success",
});

function toast(text: string, color: string = "success") {
  snack.value = { open: true, text, color };
}

async function runAndRefresh(fn: () => Promise<void>, okMsg?: string) {
  actionBusy.value = true;
  try {
    await fn();
    await td.refresh();
    if (okMsg) toast(okMsg, "success");
  } catch (e: any) {
    toast(e?.normalizedMessage ?? e?.message ?? "İşlem başarısız.", "error");
  } finally {
    actionBusy.value = false;
  }
}

onMounted(async () => {
  if (ready.value) await td.load(routeTicketId.value);
});

watch(routeTicketId, async (id) => {
  if (id > 0) await td.load(id);
});

async function onSaveStatusNotes(payload: { status: string; notes: string }) {
  if (!dashboard.value) return;
  const id = dashboard.value.ticket.ticketId;

  await runAndRefresh(async () => {
    if (payload.status && payload.status !== dashboard.value!.ticket.status) {
      await td.updateTicketStatus(id, payload.status);
    }
    const currentNotes = dashboard.value!.ticket.internalNotes ?? "";
    if ((payload.notes ?? "") !== currentNotes) {
      await td.updateTicketNotes(id, payload.notes ?? "");
    }
  }, "Güncellendi");
}

async function onCreatePR() {
  if (!dashboard.value) return;
  const id = dashboard.value.ticket.ticketId;
  await runAndRefresh(async () => td.createPartRequest(id), "PR oluşturuldu");
}


async function onAddPRItem(payload: { requestId: number; partCode: string; qty: number }) {
  await runAndRefresh(
    async () => td.addPartRequestItem(payload.requestId, payload.partCode, payload.qty),
    "Item eklendi"
  );
}


async function onIssuePRItem(payload: { requestId: number; itemId: number; issuedQty: number; note?: string }) {
  await runAndRefresh(
    async () => td.issuePartRequestItem(payload.requestId, payload.itemId, payload.issuedQty, payload.note),
    "Item issue edildi"
  );
}

async function onSetPRStatus(payload: { requestId: number; status: string }) {
  await runAndRefresh(
    async () => td.setPartRequestStatus(payload.requestId, payload.status),
    "PR status güncellendi"
  );
}

async function onCancelPR(payload: { requestId: number; reason?: string }) {
  await runAndRefresh(
    async () => td.cancelPartRequest(payload.requestId, payload.reason),
    "PR iptal edildi"
  );
}

async function onClosePR(payload: { requestId: number }) {
  await runAndRefresh(async () => td.closePartRequest(payload.requestId), "PR kapatıldı");
}
</script>

<style scoped>
.gap-2 {
  gap: 8px;
}
.gap-4 {
  gap: 16px;
}
</style>
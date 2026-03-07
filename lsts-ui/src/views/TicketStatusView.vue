<template>
  <v-container fluid class="page">
    <div class="page-head">
      <div class="head-left">
        <div class="kicker">Tickets</div>
        <div class="title-row">
          <h1 class="page-title">Status & Notlar</h1>

          <template v-if="ticket">
            <v-chip
              class="ml-3"
              size="small"
              variant="tonal"
              :color="statusMeta(ticket.currentStatus).color"
              prepend-icon="mdi-circle-medium"
            >
              {{ statusMeta(ticket.currentStatus).label }}
            </v-chip>
          </template>
        </div>

        <div class="subtitle" v-if="ticket">
          <span class="mono">#{{ ticket.ticketId }}</span>
          <span class="dot">•</span>
          <span class="mono">{{ ticket.serviceNo }}</span>
          <span class="dot">•</span>
          <span class="muted">Cihaz:</span>
          <span class="mono">{{ ticket.deviceId }}</span>
        </div>

        <div class="subtitle muted" v-else>
          Ticket durumunu güncelle ve notları yönet.
        </div>
      </div>

      <div class="head-right">
        <v-btn
          variant="tonal"
          prepend-icon="mdi-arrow-left"
          @click="goDashboard"
          :disabled="!ticketId"
        >
          Dashboard’a dön
        </v-btn>

        <v-btn
          color="primary"
          prepend-icon="mdi-content-save"
          @click="saveAll"
          :loading="saving"
          :disabled="!ticketId"
        >
          Kaydet
        </v-btn>
      </div>
    </div>

    <v-alert
      v-if="err"
      type="error"
      variant="tonal"
      class="mb-4"
      border="start"
      border-color="error"
    >
      {{ err }}
    </v-alert>

    <v-alert
      v-if="ok"
      type="success"
      variant="tonal"
      class="mb-4"
      border="start"
      border-color="success"
    >
      {{ ok }}
    </v-alert>

    <v-skeleton-loader v-if="loading" type="article, heading, list-item-two-line, actions" />

    <template v-else>
      <template v-if="ticket">
        <v-row dense class="mt-1">
          <v-col cols="12" md="6">
            <v-card class="panel" variant="tonal">
              <v-card-title class="panel-title">
                <div class="d-flex align-center ga-2">
                  <v-icon icon="mdi-swap-vertical" />
                  Durum Güncelle
                </div>
                <v-spacer />
                <v-chip size="small" variant="tonal" color="info">
                  mevcut: {{ ticket.currentStatus }}
                </v-chip>
              </v-card-title>
              <v-divider />
              <v-card-text>
                <v-row dense>
                  <v-col cols="12">
                    <v-select
                      v-model="form.toStatus"
                      :items="statusOptions"
                      item-title="label"
                      item-value="value"
                      label="Yeni Durum"
                      variant="outlined"
                      density="comfortable"
                      prepend-inner-icon="mdi-flag"
                      hide-details
                    >
                      <template #selection="{ item }">
                        <div class="d-flex align-center ga-2">
                          <v-chip
                            size="x-small"
                            variant="tonal"
                            :color="statusMeta(item.value).color"
                          >
                            {{ statusMeta(item.value).label }}
                          </v-chip>
                          <span class="mono muted">{{ item.value }}</span>
                        </div>
                      </template>

                      <template #item="{ props, item }">
                        <v-list-item v-bind="props">
                          <template #prepend>
                            <v-avatar size="28" :color="statusMeta(item.value).color" variant="tonal">
                              <v-icon :icon="statusMeta(item.value).icon" size="18" />
                            </v-avatar>
                          </template>
                          <v-list-item-title>{{ statusMeta(item.value).label }}</v-list-item-title>
                          <v-list-item-subtitle class="mono">{{ item.value }}</v-list-item-subtitle>
                        </v-list-item>
                      </template>
                    </v-select>
                  </v-col>

                  <v-col cols="12" md="6">
                    <v-text-field
                      v-model.number="actorUserId"
                      label="changedBy (User ID)"
                      type="number"
                      min="1"
                      variant="outlined"
                      density="comfortable"
                      prepend-inner-icon="mdi-account"
                      hide-details
                    />
                  </v-col>

                  <v-col cols="12" md="6">
                    <v-text-field
                      :model-value="fmtDateTime(ticket.statusUpdatedAt)"
                      label="Son Status Güncelleme"
                      variant="outlined"
                      density="comfortable"
                      prepend-inner-icon="mdi-clock-outline"
                      hide-details
                      readonly
                    />
                  </v-col>
                </v-row>

                <div class="muted small mt-3">
                  Status kaydı: <span class="mono">{ toStatus, changedBy }</span>
                </div>
              </v-card-text>
            </v-card>
          </v-col>
          
          <v-col cols="12" md="6">
            <v-card class="panel" variant="tonal">
              <v-card-title class="panel-title">
                <div class="d-flex align-center ga-2">
                  <v-icon icon="mdi-notebook-edit-outline" />
                  Notlar
                </div>
                <v-spacer />
                <v-chip size="small" variant="tonal" color="secondary">internal + public</v-chip>
              </v-card-title>
              <v-divider />
              <v-card-text>
                <v-row dense>
                  <v-col cols="12">
                    <v-textarea
                      v-model="form.internalNotes"
                      label="Internal Notes"
                      variant="outlined"
                      density="comfortable"
                      rows="4"
                      auto-grow
                      prepend-inner-icon="mdi-shield-lock-outline"
                      hide-details
                    />
                  </v-col>

                  <v-col cols="12">
                    <v-textarea
                      v-model="form.publicNote"
                      label="Public Note"
                      variant="outlined"
                      density="comfortable"
                      rows="4"
                      auto-grow
                      prepend-inner-icon="mdi-bullhorn-outline"
                      hide-details
                    />
                  </v-col>
                </v-row>

                <div class="muted small mt-3">
                  Notes kaydı: <span class="mono">{ internalNotes, publicNote, updatedBy }</span>
                </div>

                <v-divider class="my-4" />

                <div class="d-flex ga-2">
                  <v-btn variant="tonal" prepend-icon="mdi-refresh" @click="reload" :disabled="saving">
                    Yenile
                  </v-btn>
                  <v-spacer />
                  <v-btn color="primary" prepend-icon="mdi-content-save" @click="saveAll" :loading="saving">
                    Kaydet
                  </v-btn>
                </div>
              </v-card-text>
            </v-card>
          </v-col>
        </v-row>
      </template>

      <template v-else>
        <v-card class="panel mt-3" variant="tonal">
          <v-card-text class="empty">
            <v-icon icon="mdi-ticket-confirmation-outline" size="32" class="mb-2" />
            <div class="empty-title">Ticket bulunamadı</div>
            <div class="muted small">
              URL şöyle olmalı: <span class="mono">/tickets/status?ticketId=6</span>
            </div>
          </v-card-text>
        </v-card>
      </template>
    </template>
  </v-container>
</template>

<script setup lang="ts">
import { onMounted, ref, computed, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { http } from "../api/http";

const STATUS_ENDPOINT = (id: number) => `/api/tickets/${id}/status`;
const NOTES_ENDPOINT  = (id: number) => `/api/tickets/${id}/notes`;


type TicketDto = {
  ticketId: number;
  serviceNo: string;
  deviceId: number;
  currentStatus: string;
  statusUpdatedAt: string | null;
  faultDesc: string;
  internalNotes: string | null;
  publicNote: string | null;
  estimatedDeliveryDate: string | null;
};
type TicketDashboardDto = { ticket: TicketDto };


const route = useRoute();
const router = useRouter();


const ticketId = computed(
  () => Number(route.params.ticketId ?? route.query.ticketId ?? 0) || 0
);


const loading = ref(false);
const saving = ref(false);
const err = ref<string | null>(null);
const ok = ref<string | null>(null);
const ticket = ref<TicketDto | null>(null);

const actorUserId = ref<number>(Number(localStorage.getItem("lsts_user_id") ?? "1") || 1);
watch(actorUserId, (v) => localStorage.setItem("lsts_user_id", String(v ?? 1)));

const form = ref({
  toStatus: "" as string,
  internalNotes: "" as string,
  publicNote: "" as string,
});

const statusOptions = [
  { value: "NEW", label: "Yeni" },
  { value: "RECEIVED", label: "Teslim Alındı" },
  { value: "DIAGNOSING", label: "Teşhis" },
  { value: "WAITING_APPROVAL", label: "Onay Bekliyor" },
  { value: "WAITING_PARTS", label: "Parça Bekliyor" },
  { value: "IN_REPAIR", label: "Onarımda" },
  { value: "READY_FOR_PICKUP", label: "Teslime Hazır" },
  { value: "DELIVERED", label: "Teslim Edildi" },
  { value: "CLOSED", label: "Kapandı" },
  { value: "CANCELED", label: "İptal" },
];

function fmtDateTime(v: string | null | undefined) {
  if (!v) return "—";
  const d = new Date(v);
  if (Number.isNaN(d.getTime())) return String(v);
  return new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium", timeStyle: "short" }).format(d);
}

function statusMeta(s: string | null | undefined) {
  const v = (s ?? "").trim().toUpperCase();
  const map: Record<string, { label: string; color: string; icon: string }> = {
    NEW: { label: "Yeni", color: "grey", icon: "mdi-new-box" },
    RECEIVED: { label: "Teslim Alındı", color: "info", icon: "mdi-inbox-arrow-down" },
    DIAGNOSING: { label: "Teşhis", color: "warning", icon: "mdi-stethoscope" },
    WAITING_APPROVAL: { label: "Onay Bekliyor", color: "warning", icon: "mdi-account-clock" },
    WAITING_PARTS: { label: "Parça Bekliyor", color: "warning", icon: "mdi-truck-fast" },
    IN_REPAIR: { label: "Onarımda", color: "primary", icon: "mdi-tools" },
    READY_FOR_PICKUP: { label: "Teslime Hazır", color: "secondary", icon: "mdi-package-variant" },
    DELIVERED: { label: "Teslim Edildi", color: "success", icon: "mdi-check-circle" },
    CLOSED: { label: "Kapandı", color: "success", icon: "mdi-lock-check" },
    CANCELED: { label: "İptal", color: "error", icon: "mdi-cancel" },
  };
  return map[v] ?? { label: v || "—", color: "info", icon: "mdi-information" };
}


async function load() {
  err.value = null;
  ok.value = null;

  if (!ticketId.value || ticketId.value <= 0) {
    ticket.value = null;
    err.value = "Geçerli ticketId yok. Örn: /tickets/status?ticketId=6";
    return;
  }

  loading.value = true;
  try {
    const { data } = await http.get(`/api/tickets/${ticketId.value}/dashboard`);
    const dash = data as TicketDashboardDto;

    ticket.value = dash.ticket;

    
    form.value.toStatus = (ticket.value.currentStatus ?? "").toUpperCase();
    form.value.internalNotes = ticket.value.internalNotes ?? "";
    form.value.publicNote = ticket.value.publicNote ?? "";
  } catch (e: any) {
    err.value = e?.response?.data?.error || e?.response?.data?.title || e?.message || "Beklenmeyen hata.";
  } finally {
    loading.value = false;
  }
}

function reload() {
  load();
}

function goDashboard() {
  if (!ticketId.value) return;
  router.push({ name: "ticket-dashboard", params: { ticketId: ticketId.value } });
}

async function saveAll() {
  err.value = null;
  ok.value = null;

  if (!ticket.value) {
    err.value = "Ticket yüklenmemiş.";
    return;
  }
  if (!actorUserId.value || actorUserId.value <= 0) {
    err.value = "User ID > 0 olmalı.";
    return;
  }

  const id = ticket.value.ticketId;

  const statusChanged =
    (form.value.toStatus || "").toUpperCase() !== (ticket.value.currentStatus || "").toUpperCase();

  const notesChanged =
    (form.value.internalNotes ?? "") !== (ticket.value.internalNotes ?? "") ||
    (form.value.publicNote ?? "") !== (ticket.value.publicNote ?? "");

  if (!statusChanged && !notesChanged) {
    ok.value = "Değişiklik yok.";
    return;
  }

  saving.value = true;
  try {

    if (statusChanged) {
    const v = (form.value.toStatus || "").toUpperCase();
    await http.patch(STATUS_ENDPOINT(id), {
      toStatus: v,
      status: v,
      changedBy: actorUserId.value,
      updatedBy: actorUserId.value,
    });
  }

  if (notesChanged) {
    await http.patch(NOTES_ENDPOINT(id), {
      internalNotes: form.value.internalNotes?.trim() || null,
      publicNote: form.value.publicNote?.trim() || null,
      updatedBy: actorUserId.value,
      changedBy: actorUserId.value, 
    });
  }

    ok.value = "Kaydedildi.";
    await load();
  } catch (e: any) {
    err.value = e?.response?.data?.error || e?.response?.data?.title || e?.message || "Beklenmeyen hata.";
  } finally {
    saving.value = false;
  }
}

onMounted(async () => {

  if (route.query.ticketId && ticketId.value > 0) {
    await router.replace({
      name: "ticket-status",
      params: { ticketId: ticketId.value },
      query: {},
    });
  }

  load();
});
watch(() => ticketId.value, () => load());
</script>

<style scoped>
.page { padding-top: 18px; }

.page-head {
  display: flex;
  gap: 16px;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 12px;
}

.head-left { min-width: 280px; }

.kicker {
  font-size: 12px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  opacity: 0.7;
}

.title-row { display: flex; align-items: center; gap: 8px; }

.page-title {
  font-size: 28px;
  font-weight: 700;
  margin: 2px 0 0;
}

.subtitle {
  margin-top: 6px;
  opacity: 0.9;
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.dot { opacity: 0.5; }
.muted { opacity: 0.72; }
.small { font-size: 12px; }

.mono {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace;
}

.head-right { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }

.panel {
  border: 1px solid rgba(255, 255, 255, 0.06);
  backdrop-filter: blur(8px);
}
.panel-title { font-weight: 750; }

.empty { padding: 16px 8px; text-align: center; }
.empty-title { font-weight: 750; margin-bottom: 4px; }
</style>
<template>
  <div>
    <PageHeader
      title="Dashboard"
      subtitle="Operasyon özeti, hızlı aksiyonlar ve son hareketler"
    >
      <template #actions>
        <v-btn color="primary" prepend-icon="mdi-plus" @click="goCreateTicket">
          Yeni Ticket
        </v-btn>
        <v-btn variant="tonal" prepend-icon="mdi-magnify" @click="goTickets">
          Ticket Ara
        </v-btn>
      </template>
    </PageHeader>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">
      {{ error }}
    </v-alert>

    <v-row>
      <v-col cols="12" md="3">
        <KpiCard
          label="Açık Ticket"
          :value="kpis.openTickets"
          icon="mdi-ticket-confirmation"
          color="primary"
          hint="CLOSED/CANCELLED hariç"
        />
      </v-col>
      <v-col cols="12" md="3">
        <KpiCard
          label="Bekleyen Parça"
          :value="kpis.waitingParts"
          icon="mdi-truck-fast"
          color="warning"
          hint="WAITING_PARTS"
        />
      </v-col>
      <v-col cols="12" md="3">
        <KpiCard
          label="Atölyede"
          :value="kpis.inRepair"
          icon="mdi-tools"
          color="info"
          hint="IN_REPAIR"
        />
      </v-col>
      <v-col cols="12" md="3">
        <KpiCard
          label="Bugün Kapandı"
          :value="kpis.closedToday"
          icon="mdi-lock-check"
          color="success"
          hint="CLOSED (bugün)"
        />
      </v-col>
    </v-row>

    <v-row class="mt-1">
      <v-col cols="12" lg="8">
        <v-card class="panel" variant="flat">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="text-subtitle-1 font-weight-bold">Son Ticket’lar</div>
            <v-btn
              size="small"
              variant="text"
              prepend-icon="mdi-refresh"
              :loading="loading"
              @click="loadDashboard"
            >
              Yenile
            </v-btn>
          </v-card-title>

          <v-divider />
          <v-card-text class="panel-body">
            <v-skeleton-loader
              v-if="loading && recentTickets.length === 0"
              type="table"
            />

            <v-table v-else density="comfortable" class="tickets-table">
              <thead>
                <tr>
                  <th class="text-left">#</th>
                  <th class="text-left">Service No</th>
                  <th class="text-left">Status</th>
                  <th class="text-left">Arıza</th>
                  <th class="text-left">Tarih</th>
                  <th class="text-right">İşlem</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="t in recentTickets" :key="t.ticketId">
                  <td class="font-weight-medium">{{ t.ticketId }}</td>
                  <td>{{ t.serviceNo }}</td>
                  <td><StatusChip :status="t.status" /></td>
                  <td class="text-truncate" style="max-width: 360px">
                    {{ t.fault }}
                  </td>
                  <td class="text-medium-emphasis">{{ t.date }}</td>
                  <td class="text-right">
                    <v-btn
                      size="small"
                      variant="tonal"
                      prepend-icon="mdi-eye"
                      @click="goTicketDashboard(t.ticketId)"
                    >
                      Detay
                    </v-btn>
                  </td>
                </tr>

                <tr v-if="recentTickets.length === 0">
                  <td colspan="6" class="text-center text-medium-emphasis py-6">
                    Henüz ticket yok.
                  </td>
                </tr>
              </tbody>
            </v-table>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" lg="4">
        <v-card class="panel" variant="flat">
          <v-card-title class="text-subtitle-1 font-weight-bold">
            Hızlı Aksiyon
          </v-card-title>
          <v-divider />
          <v-card-text class="d-flex flex-column ga-3">
            <v-text-field
              v-model="quickTicketId"
              label="Ticket ID"
              type="number"
              prepend-inner-icon="mdi-ticket"
              hide-details
            />
            <v-btn
              color="secondary"
              prepend-icon="mdi-monitor-dashboard"
              @click="quickOpenDashboard"
            >
              Ticket Dashboard Aç
            </v-btn>

            <v-divider class="my-1" />

            <v-btn variant="tonal" prepend-icon="mdi-memory" @click="goParts">
              Parça Ara
            </v-btn>
            <v-btn
              variant="tonal"
              prepend-icon="mdi-clipboard-list-outline"
              @click="goPartRequests"
            >
              Part Requests
            </v-btn>
          </v-card-text>
        </v-card>

        <v-card class="panel mt-4" variant="flat">
          <v-card-title class="text-subtitle-1 font-weight-bold">
            Son Hareketler
          </v-card-title>
          <v-divider />

          <v-card-text class="panel-body panel-body--activity d-flex flex-column ga-2">
            <v-skeleton-loader
              v-if="loading && activity.length === 0"
              type="list-item-three-line"
            />

            <div
              v-else
              v-for="a in activity"
              :key="a.id"
              class="d-flex align-start ga-3"
            >
              <v-avatar size="26" color="surface" variant="flat" class="ava">
                <v-icon size="16" :icon="a.icon" />
              </v-avatar>
              <div class="flex-1">
                <div class="text-body-2">{{ a.text }}</div>
                <div class="text-caption text-medium-emphasis">{{ a.time }}</div>
              </div>
            </div>

            <div v-if="!loading && activity.length === 0" class="text-medium-emphasis py-2">
              Aktivite bulunamadı.
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";

import PageHeader from "../components/PageHeader.vue";
import KpiCard from "../components/KpiCard.vue";
import StatusChip from "../components/StatusChip.vue";

import { getDashboardSummary } from "../api/lsts";

const router = useRouter();

const loading = ref(false);
const error = ref<string | null>(null);

const kpis = ref({
  openTickets: 0,
  waitingParts: 0,
  inRepair: 0,
  closedToday: 0,
});

const recentTickets = ref<
  Array<{ ticketId: number; serviceNo: string; status: string; fault: string; date: string }>
>([]);

const activity = ref<
  Array<{ id: number; icon: string; text: string; time: string }>
>([]);

const quickTicketId = ref<number | null>(null);

function fmtShort(v: string | null) {
  if (!v) return "-";
  try {
    const d = new Date(v);
    return d.toLocaleString("tr-TR", {
      day: "2-digit",
      month: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return v;
  }
}

async function loadDashboard() {
  loading.value = true;
  error.value = null;

  try {
    const dto = await getDashboardSummary();

    kpis.value = dto.kpis;

    recentTickets.value = dto.recentTickets.map((t) => ({
      ticketId: t.ticketId,
      serviceNo: t.serviceNo,
      status: t.status,
      fault: t.fault,
      date: fmtShort(t.at),
    }));

    activity.value = dto.activity.map((a) => ({
      id: a.id,
      icon: a.icon,
      text: a.text,
      time: fmtShort(a.at),
    }));

    if (!quickTicketId.value && recentTickets.value.length) {
      quickTicketId.value = recentTickets.value[0].ticketId;
    }
  } catch (e: any) {
    error.value = e?.message ?? "Dashboard verisi alınamadı.";
  } finally {
    loading.value = false;
  }
}

onMounted(loadDashboard);

function goTicketDashboard(id: number) {
  router
    .push({ name: "ticket-dashboard", params: { ticketId: id } })
    .catch(() => router.push({ name: "tickets" }).catch(() => {}));
}

function quickOpenDashboard() {
  if (!quickTicketId.value) return;
  goTicketDashboard(quickTicketId.value);
}

function goCreateTicket() {
  router
    .push({ name: "ticket-create" })
    .catch(() => router.push({ name: "tickets" }).catch(() => {}));
}
function goTickets() {
  router.push({ name: "tickets" }).catch(() => {});
}
function goParts() {
  router.push({ name: "parts" }).catch(() => {});
}
function goPartRequests() {
  router.push({ name: "part-requests" }).catch(() => {});
}
</script>

<style scoped>
.panel {
  border: 1px solid rgba(255, 255, 255, 0.06);
  background: rgba(255, 255, 255, 0.02);
}
.ava {
  border: 1px solid rgba(255, 255, 255, 0.06);
}

.panel-body {
  overflow: hidden; /* ✅ tablo kendi wrapper’ında scroll olacak */
}

/* ✅ Son Ticketlar: scroll + sticky header doğru yerde */
.tickets-table :deep(.v-table__wrapper) {
  max-height: 340px;
  overflow-y: auto;
  overflow-x: hidden;
}

/* ✅ TH sticky */
.tickets-table :deep(.v-table__wrapper thead th) {
  position: sticky;
  top: 0;
  z-index: 5;
  background: rgba(10, 12, 16, 0.96);
  backdrop-filter: blur(10px);
  box-shadow: inset 0 -1px rgba(255, 255, 255, 0.10);
}

/* optional: header text daha net */
.tickets-table :deep(.v-table__wrapper thead th) {
  color: rgba(255, 255, 255, 0.88);
  font-weight: 800;
}

/* premium scrollbar (tablo wrapper) */
.tickets-table :deep(.v-table__wrapper::-webkit-scrollbar) { width: 10px; }
.tickets-table :deep(.v-table__wrapper::-webkit-scrollbar-track) { background: rgba(255,255,255,0.02); }
.tickets-table :deep(.v-table__wrapper::-webkit-scrollbar-thumb) {
  background: rgba(255,255,255,0.12);
  border-radius: 999px;
  border: 2px solid rgba(0,0,0,0);
  background-clip: padding-box;
}
.tickets-table :deep(.v-table__wrapper::-webkit-scrollbar-thumb:hover) { background: rgba(255,255,255,0.18); }

/* Son Hareketler kartı içeride scroll */
.panel-body--activity {
  max-height: 300px;
  overflow-y: auto;
  overflow-x: hidden;
  padding-top: 12px;
}

/* premium scrollbar (activity) */
.panel-body--activity::-webkit-scrollbar { width: 10px; }
.panel-body--activity::-webkit-scrollbar-track { background: rgba(255,255,255,0.02); }
.panel-body--activity::-webkit-scrollbar-thumb{
  background: rgba(255,255,255,0.12);
  border-radius: 999px;
  border: 2px solid rgba(0,0,0,0);
  background-clip: padding-box;
}
.panel-body--activity::-webkit-scrollbar-thumb:hover { background: rgba(255,255,255,0.18); }
</style>
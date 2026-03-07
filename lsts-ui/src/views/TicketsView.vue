<template>
  <v-container fluid class="py-6">
    <div class="d-flex align-center justify-space-between mb-5">
      <div>
        <div class="text-h5 font-weight-bold">Tickets</div>
        <div class="text-body-2 text-medium-emphasis">
          Aktif servis kayıtları (liste)
        </div>
      </div>

      <div class="d-flex ga-2">
        <v-btn
          variant="tonal"
          prepend-icon="mdi-refresh"
          :loading="loading"
          @click="refresh"
        >
          Yenile
        </v-btn>

        <v-btn color="primary" prepend-icon="mdi-plus" to="/tickets/create">
          Yeni Ticket
        </v-btn>
      </div>
    </div>

    <v-row>
      <!-- Filters -->
      <v-col cols="12" md="4" lg="3">
        <v-card class="pa-4" rounded="xl">
          <div class="d-flex align-center justify-space-between mb-3">
            <div class="text-subtitle-1 font-weight-bold">Filtre</div>
            <v-icon icon="mdi-filter-variant" class="text-medium-emphasis" />
          </div>

          <v-text-field
            v-model="q"
            label="Ara (ServiceNo / TicketId / Açıklama)"
            prepend-inner-icon="mdi-magnify"
            clearable
            density="comfortable"
            :disabled="loading"
            class="mb-3"
          />

          <v-select
            v-model="status"
            :items="statusItems"
            label="Durum"
            prepend-inner-icon="mdi-tag"
            clearable
            density="comfortable"
            :disabled="loading"
            class="mb-2"
          />

          <v-switch
            v-model="openOnly"
            label="Sadece açık ticketlar"
            inset
            color="primary"
            :disabled="loading"
            class="mt-1"
          />

          <v-divider class="my-4" />

          <div class="text-caption text-medium-emphasis mb-2">
            Hızlı aç (Ticket ID)
          </div>

          <div class="d-flex ga-2">
            <v-text-field
              v-model.number="ticketId"
              type="number"
              min="1"
              density="comfortable"
              hide-details
              placeholder="Ticket ID"
              prepend-inner-icon="mdi-pound"
              :disabled="loading"
            />
            <v-btn
              color="secondary"
              variant="tonal"
              :disabled="loading"
              @click="openDash"
            >
              Aç
            </v-btn>
          </div>

          <v-alert
            v-if="err"
            type="error"
            variant="tonal"
            class="mt-3"
            density="comfortable"
            border="start"
          >
            {{ err }}
          </v-alert>

          <v-divider class="my-4" />

          <div class="text-caption text-medium-emphasis">
            Satıra tıkla → Ticket Dashboard açılır.
          </div>
        </v-card>
      </v-col>

      <!-- List -->
      <v-col cols="12" md="8" lg="9">
        <v-card rounded="xl">
          <div class="px-4 pt-4 pb-2 d-flex align-center justify-space-between">
            <div>
              <div class="text-subtitle-1 font-weight-bold">Kayıtlar</div>
              <div class="text-caption text-medium-emphasis">
                {{ filtered.length }} kayıt
              </div>
            </div>

            <div class="d-flex ga-2">
              <v-chip v-if="openOnly" variant="tonal" color="primary" size="small">
                Açık
              </v-chip>
              <v-chip v-else variant="tonal" size="small">
                Tümü
              </v-chip>
            </div>
          </div>

          <v-divider />

          <v-card-text class="pa-0">
            <v-skeleton-loader v-if="loading" type="table" class="pa-4" />

            <div v-else>
              <v-data-table
                :headers="headers"
                :items="filtered"
                item-key="ticketId"
                density="comfortable"
                hover
                class="table-click"
                @click:row="onRowClick"
              >
                <template #item.currentStatus="{ item }">
                  <v-chip
                    size="small"
                    :color="statusColor(item.currentStatus)"
                    variant="flat"
                  >
                    {{ item.currentStatus }}
                  </v-chip>
                </template>

                <template #item.faultDesc="{ item }">
                  <div class="text-body-2 text-truncate" style="max-width: 520px">
                    {{ item.faultDesc }}
                  </div>
                </template>

                <template #item.at="{ item }">
                  <span class="text-body-2">{{ formatDate(item.at) }}</span>
                </template>

                <template #item.actions="{ item }">
                  <v-btn
                    size="small"
                    variant="text"
                    icon="mdi-open-in-new"
                    @click.stop="goTicketDashboard(item.ticketId)"
                  />
                </template>

                <template #no-data>
                  <div class="py-10 text-center text-medium-emphasis">
                    <v-icon icon="mdi-ticket-outline" class="mb-2" />
                    <div class="text-subtitle-1 font-weight-bold">Kayıt yok</div>
                    <div class="text-body-2">
                      Filtreleri temizlemeyi deneyebilirsin.
                    </div>
                  </div>
                </template>
              </v-data-table>
            </div>
          </v-card-text>
        </v-card>

        <v-alert
          v-if="error"
          type="error"
          variant="tonal"
          class="mt-3"
          border="start"
        >
          {{ error }}
        </v-alert>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { useRouter } from "vue-router";
import { getTicketsList, type TicketListItemDto } from "../api/lsts";

const router = useRouter();

const loading = ref(false);
const error = ref<string | null>(null);

const rows = ref<TicketListItemDto[]>([]);

const q = ref("");
const status = ref<string | null>(null);
const openOnly = ref(true);

const ticketId = ref<number>(0);
const err = ref<string | null>(null);

const statusItems = [
  "NEW",
  "RECEIVED",
  "DIAGNOSING",
  "WAITING_APPROVAL",
  "WAITING_PARTS",
  "IN_REPAIR",
  "READY_FOR_PICKUP",
  "DELIVERED",
  "CLOSED",
  "CANCELED",
];

const headers = [
  { title: "Ticket", key: "ticketId", sortable: true, width: 90 },
  { title: "Service No", key: "serviceNo", sortable: true, width: 160 },
  { title: "Durum", key: "currentStatus", sortable: true, width: 150 },
  { title: "Arıza", key: "faultDesc", sortable: false },
  { title: "Güncelleme", key: "at", sortable: true, width: 170 },
  { title: "", key: "actions", sortable: false, align: "end", width: 60 },
] as const;

function formatDate(v: any) {
  if (!v) return "-";
  try {
    const d = new Date(v);
    return d.toLocaleString("tr-TR");
  } catch {
    return String(v);
  }
}

function statusColor(s: string) {
  const x = (s || "").toUpperCase();
  if (x.includes("CANCEL")) return "error";
  if (x.includes("CLOSE")) return "success";
  if (x.includes("WAIT")) return "warning";
  if (x.includes("REPAIR") || x.includes("DIAG")) return "info";
  return "primary";
}

function normalize(v: any) {
  return String(v ?? "").trim().toLowerCase();
}

const filtered = computed(() => {
  const text = normalize(q.value);
  const st = (status.value || "").trim().toUpperCase();

  return rows.value.filter((r) => {
    if (st && (r.currentStatus || "").toUpperCase() !== st) return false;

    if (!text) return true;
    return (
      normalize(r.serviceNo).includes(text) ||
      normalize(r.ticketId).includes(text) ||
      normalize(r.faultDesc).includes(text)
    );
  });
});

async function load() {
  loading.value = true;
  error.value = null;
  try {
    rows.value = await getTicketsList({ openOnly: openOnly.value, limit: 300 });
  } catch (e: any) {
    error.value = e?.message ?? "Ticket listesi alınamadı.";
  } finally {
    loading.value = false;
  }
}

function refresh() {
  load();
}

function goTicketDashboard(id: number) {
  router.push(`/tickets/${id}/dashboard`);
}

function onRowClick(_: any, ctx: any) {
  const item = ctx?.item?.raw as TicketListItemDto | undefined;
  if (!item) return;
  goTicketDashboard(item.ticketId);
}

function openDash() {
  err.value = null;
  if (!ticketId.value || ticketId.value <= 0) {
    err.value = "Ticket ID 1 veya daha büyük olmalı.";
    return;
  }
  goTicketDashboard(ticketId.value);
}

watch(openOnly, () => {
  load();
});

onMounted(load);
</script>

<style scoped>
.table-click :deep(tbody tr) {
  cursor: pointer;
}
</style>

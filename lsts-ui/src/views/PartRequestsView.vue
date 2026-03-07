<template>
  <v-container fluid class="py-6">
    <div class="d-flex align-center justify-space-between mb-6">
      <div>
        <div class="text-h5 font-weight-bold">Part Requests</div>
        <div class="text-medium-emphasis">Aktif parça talepleri (ticket seçmeden)</div>
      </div>

      <div class="d-flex ga-2">
        <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="load">
          Yenile
        </v-btn>
        <v-btn variant="text" @click="clearFilters" :disabled="loading">
          Temizle
        </v-btn>
      </div>
    </div>

    <v-row dense>
      <!-- Filters -->
      <v-col cols="12" lg="4">
        <v-card class="pa-5" rounded="xl">
          <div class="d-flex align-center justify-space-between mb-3">
            <div class="text-subtitle-1 font-weight-bold">Filtre</div>
            <v-icon icon="mdi-filter-variant" />
          </div>

          <v-text-field
            v-model="q"
            label="Ara (ServiceNo / TicketId / RequestId)"
            prepend-inner-icon="mdi-magnify"
            clearable
            hide-details
            :disabled="loading"
          />

          <v-select
            v-model="status"
            :items="statusItems"
            label="Status"
            prepend-inner-icon="mdi-tag"
            clearable
            hide-details
            class="mt-3"
            :disabled="loading"
          />

          <v-switch
            v-model="onlyOpenItems"
            color="primary"
            inset
            class="mt-2"
            :disabled="loading"
            hide-details
            label="Sadece issue bekleyenler (açık item)"
          />

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
            İpucu: Satıra tıkla → Ticket Dashboard açılır. Warehouse rolü issue işlemini oradan yapar.
          </div>
        </v-card>
      </v-col>

      <!-- List -->
      <v-col cols="12" lg="8">
        <v-card rounded="xl">
          <div class="d-flex align-center justify-space-between pa-5 pb-2">
            <div>
              <div class="text-subtitle-1 font-weight-bold">Kayıtlar</div>
              <div class="text-medium-emphasis">Aktif talepler</div>
            </div>

            <v-chip v-if="filtered.length" color="primary" variant="tonal">
              {{ filtered.length }} kayıt
            </v-chip>
          </div>

          <v-progress-linear v-if="loading" indeterminate />

          <v-divider />

          <div v-if="!loading && rows.length === 0" class="pa-8 text-center">
            <v-icon icon="mdi-inbox" size="42" class="mb-2" />
            <div class="text-subtitle-1 font-weight-bold">Kayıt yok</div>
            <div class="text-medium-emphasis mt-1">
              Şu an aktif part request görünmüyor.
            </div>
          </div>

          <div v-else-if="!loading && rows.length > 0 && filtered.length === 0" class="pa-8 text-center">
            <v-icon icon="mdi-filter-remove-outline" size="42" class="mb-2" />
            <div class="text-subtitle-1 font-weight-bold">Filtre nedeniyle sonuç yok</div>
            <div class="text-medium-emphasis mt-1">
              “Sadece issue bekleyenler” açık olabilir veya arama/status filtresi daraltıyor.
            </div>
            <v-btn class="mt-4" variant="tonal" @click="clearFilters">
              Filtreleri temizle
            </v-btn>
          </div>

          <v-table v-else class="table-premium">
            <thead>
              <tr>
                <th class="text-left">Request</th>
                <th class="text-left">Ticket</th>
                <th class="text-left">Service No</th>
                <th class="text-left">Status</th>
                <th class="text-left">Items</th>
                <th class="text-left">Tarih</th>
                <th class="text-right"></th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="r in filtered"
                :key="r.requestId"
                class="row-click"
                @click="goDashboard(r.ticketId)"
              >
                <td class="font-weight-medium">#{{ r.requestId }}</td>
                <td class="text-medium-emphasis">#{{ r.ticketId }}</td>
                <td class="font-weight-medium">{{ r.serviceNo || '-' }}</td>
                <td>
                  <v-chip
                    size="small"
                    variant="tonal"
                    :color="statusColor(r.requestStatus)"
                    prepend-icon="mdi-circle-medium"
                  >
                    {{ (r.requestStatus || "-").toString() }}
                  </v-chip>
                </td>
                <td class="text-medium-emphasis">
                  <v-chip size="x-small" variant="tonal">
                    {{ r.openItemCount }} / {{ r.itemCount }}
                  </v-chip>
                </td>
                <td class="text-medium-emphasis">{{ formatDate(r.requestedAt) }}</td>
                <td class="text-right">
                  <v-btn
                    size="small"
                    variant="text"
                    icon="mdi-open-in-new"
                    @click.stop="goDashboard(r.ticketId)"
                  />
                </td>
              </tr>
            </tbody>
          </v-table>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { getActivePartRequests, type ActivePartRequestListItemDto } from "../api/lsts";

const router = useRouter();

const loading = ref(false);
const err = ref<string | null>(null);
const rows = ref<ActivePartRequestListItemDto[]>([]);

const q = ref<string>("");
const status = ref<string | null>(null);
const onlyOpenItems = ref(false);

const statusItems = [
  "REQUESTED",
  "PARTIALLY_ISSUED",
  "ISSUED",
];

function norm(v: any) {
  return String(v ?? "").trim().toLowerCase();
}

const filtered = computed(() => {
  const text = norm(q.value);
  return rows.value.filter((r) => {
    if (status.value) {
      const rs = (r.requestStatus ?? "").toUpperCase();
      if (rs !== status.value.toUpperCase()) return false;
    }
    if (onlyOpenItems.value && (r.openItemCount ?? 0) <= 0) return false;

    if (!text) return true;
    return (
      norm(r.serviceNo).includes(text) ||
      norm(r.ticketId).includes(text) ||
      norm(r.requestId).includes(text)
    );
  });
});

async function load() {
  err.value = null;
  loading.value = true;
  try {
    rows.value = await getActivePartRequests(300);
  } catch (e: any) {
    err.value = e?.message ?? "İstek başarısız";
    rows.value = [];
  } finally {
    loading.value = false;
  }
}

function clearFilters() {
  q.value = "";
  status.value = null;
  onlyOpenItems.value = false;
}

function goDashboard(ticketId: number) {
  router.push({ name: "ticket-dashboard", params: { ticketId } });
}

function formatDate(v: string | null | undefined) {
  if (!v) return "-";
  const d = new Date(v);
  if (Number.isNaN(d.getTime())) return String(v);
  return d.toLocaleString("tr-TR");
}

function statusColor(s: string | null | undefined) {
  const v = (s || "").toUpperCase();
  if (v === "REQUESTED") return "warning";
  if (v === "PARTIALLY_ISSUED") return "info";
  if (v === "ISSUED") return "success";
  if (v === "CANCELLED" || v === "CANCELED") return "error";
  if (v === "CLOSED") return "secondary";
  return "primary";
}

onMounted(load);
</script>

<style scoped>
.table-premium :deep(thead th) {
  font-size: 12px;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  opacity: 0.8;
}
.table-premium :deep(tbody tr:hover) {
  background: rgba(255, 255, 255, 0.04);
}
.row-click {
  cursor: pointer;
}
</style>

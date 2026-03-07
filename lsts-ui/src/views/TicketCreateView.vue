<template>
  <v-row>
    <v-col cols="12" md="8">
      <v-card class="panel" variant="flat">
        <v-card-title class="d-flex align-center justify-space-between">
          <div>
            <div class="text-h6 font-weight-bold">Yeni Ticket</div>
            <div class="text-caption text-medium-emphasis">Servis kaydı oluştur</div>
          </div>
          <v-chip color="secondary" variant="tonal" prepend-icon="mdi-ticket">
            Tickets
          </v-chip>
        </v-card-title>

        <v-divider />

        <v-card-text>
          <v-form @submit.prevent="submit">
            <v-row>
              <v-col cols="12" md="6">
                <v-autocomplete
                  v-model="selectedCustomer"
                  v-model:search="customerSearch"
                  :items="customerItems"
                  item-title="label"
                  return-object
                  clearable
                  label="Müşteri seç"
                  :loading="customerLoading"
                  :no-data-text="customerSearch && customerSearch.length >= 2 ? 'Sonuç yok' : 'Aramak için en az 2 karakter yaz...'"
                  @click:control="onCustomerFocus"
                  @update:menu="(v) => v && onCustomerFocus()"
                  prepend-inner-icon="mdi-account"
                >
                  <template #item="{ props, item }">
                    <v-list-item v-bind="props">
                      <v-list-item-title class="d-flex align-center ga-2">
                        <span class="font-weight-semibold">{{ item.raw.fullName }}</span>
                        <v-chip size="x-small" variant="tonal" color="secondary">
                          #{{ item.raw.customerId }}
                        </v-chip>
                      </v-list-item-title>
                      <v-list-item-subtitle>
                        <span class="mono">{{ maskPhone(item.raw.phoneDigits) }}</span>
                        <span v-if="item.raw.email"> • {{ item.raw.email }}</span>
                      </v-list-item-subtitle>
                    </v-list-item>
                  </template>
                </v-autocomplete>
              </v-col>

              <v-col cols="12" md="6">
                <v-autocomplete
                  v-model="selectedDevice"
                  v-model:search="deviceSearch"
                  :items="deviceItems"
                  item-title="label"
                  return-object
                  clearable
                  label="Cihaz seç"
                  :disabled="!selectedCustomer"
                  :loading="deviceLoading"
                  :no-data-text="deviceNoDataText"
                  @click:control="onDeviceFocus"
                  @update:menu="(v) => v && onDeviceFocus()"
                  prepend-inner-icon="mdi-laptop"
                >
                  <template #item="{ props, item }">
                    <v-list-item v-bind="props">
                      <v-list-item-title class="d-flex align-center ga-2">
                        <span class="mono">{{ item.raw.serialNo }}</span>
                        <v-chip size="x-small" variant="tonal" color="secondary">
                          {{ item.raw.brandCode }}
                        </v-chip>
                      </v-list-item-title>

                      <v-list-item-subtitle>
                        {{ item.raw.modelCode }} • {{ item.raw.modelName }} • {{ item.raw.modelNo }}
                        <span v-if="item.raw.ramSummary"> • RAM: {{ item.raw.ramSummary }}</span>
                        <span v-if="item.raw.screenSummary"> • Screen: {{ item.raw.screenSummary }}</span>
                      </v-list-item-subtitle>
                    </v-list-item>
                  </template>
                </v-autocomplete>

                <div v-if="!selectedCustomer" class="text-caption text-medium-emphasis mt-1">
                  Önce müşteri seç. Sonra cihaz listesi müşteri bazlı filtrelenir.
                </div>
              </v-col>

              <v-col cols="12">
                <v-textarea v-model="form.faultDesc" label="Arıza Açıklaması" rows="3" />
              </v-col>

              <v-col cols="12" md="6">
                <v-text-field v-model="form.internalNotes" label="Internal Notes" />
              </v-col>

              <v-col cols="12" md="6">
                <v-text-field v-model="form.publicNote" label="Public Note" />
              </v-col>

              <v-col cols="12" class="d-flex ga-2">
                <v-btn color="primary" type="submit" :loading="loading" prepend-icon="mdi-content-save">
                  Oluştur
                </v-btn>
                <v-btn variant="text" @click="reset" prepend-icon="mdi-refresh">
                  Temizle
                </v-btn>
              </v-col>
            </v-row>
          </v-form>
        </v-card-text>
      </v-card>
    </v-col>

    <v-col cols="12" md="4">
      <v-card class="panel" variant="flat">
        <v-card-title class="text-subtitle-1 font-weight-bold">İpuçları</v-card-title>
        <v-divider />
        <v-card-text class="text-body-2 text-medium-emphasis">
          Akış: <b>Müşteri → Cihaz → Ticket</b>
        </v-card-text>
      </v-card>
    </v-col>
  </v-row>

  <v-snackbar v-model="snack.open" :color="snack.color">
    {{ snack.text }}
  </v-snackbar>
</template>

<script setup lang="ts">
import { reactive, ref, watch, computed } from "vue";
import { useRoute } from "vue-router";
import { http } from "../api/http";
import { useActor } from "../features/actor/useActor";

const route = useRoute();

const { actorUserId } = useActor();

const loading = ref(false);
const snack = reactive({ open: false, text: "", color: "success" as "success" | "error" });

const form = reactive({
  deviceId: 0,
  faultDesc: "",
  internalNotes: "",
  publicNote: "",
  createdBy: 1,
});

type CustomerPicklistItem = {
  customerId: number;
  fullName: string;
  phoneDigits: string;
  phoneLast4?: string | null;
  email?: string | null;
  label: string;
};

const selectedCustomer = ref<CustomerPicklistItem | null>(null);
const customerItems = ref<CustomerPicklistItem[]>([]);
const customerLoading = ref(false);
const customerSearch = ref("");

let customerTimer: any = null;

function maskPhone(phoneDigits: string) {
  const digits = (phoneDigits || "").replace(/\D/g, "");
  if (digits.length < 4) return digits || "-";
  return `*** *** ${digits.slice(-4)}`;
}

async function loadCustomerPicklist(q?: string) {
  customerLoading.value = true;
  try {
    const { data } = await http.get<any[]>("/api/customers/picklist", {
      params: { q: q || null, limit: 30 },
    });

    customerItems.value = (data ?? []).map((c: any) => ({
      ...c,
      label: `${c.fullName} • ${maskPhone(c.phoneDigits)} • #${c.customerId}`,
    }));
  } catch {
    customerItems.value = [];
  } finally {
    customerLoading.value = false;
  }
}

function onCustomerFocus() {
  clearTimeout(customerTimer);

  const q = (customerSearch.value || "").trim();
  if (q.length >= 2) {
    loadCustomerPicklist(q);
    return;
  }

  if (customerItems.value.length > 0) return;
  loadCustomerPicklist("");
}

watch(customerSearch, (v) => {
  clearTimeout(customerTimer);
  customerTimer = setTimeout(() => {
    const q = v?.trim() || "";
    if (q.length === 0) return loadCustomerPicklist("");
    if (q.length < 2) return;
    loadCustomerPicklist(q);
  }, 250);
});

type DevicePicklistItem = {
  deviceId: number;
  serialNo: string;
  customerId: number;
  deviceType: string;
  modelId: number;
  modelCode: string;
  modelName: string;
  modelNo: string;
  cpu?: string | null;
  gpu?: string | null;
  ramSummary?: string | null;
  screenSummary?: string | null;
  brandId: number;
  brandCode: string;
  label: string;
};

const selectedDevice = ref<DevicePicklistItem | null>(null);
const deviceItems = ref<DevicePicklistItem[]>([]);
const deviceLoading = ref(false);
const deviceSearch = ref("");

const deviceNoDataText = computed(() => {
  if (!selectedCustomer.value) return "Önce müşteri seç.";
  const q = deviceSearch.value?.trim() || "";
  if (q.length >= 2) return "Sonuç yok";
  return "Aramak için en az 2 karakter yaz veya menüyü aç (son cihazlar).";
});

async function loadDevicePicklist(q?: string) {
  if (!selectedCustomer.value) return;

  deviceLoading.value = true;
  try {
    const { data } = await http.get<DevicePicklistItem[]>("/api/devices/picklist", {
      params: {
        customerId: selectedCustomer.value.customerId,
        q: q || null,
        limit: 50,
      },
    });

    deviceItems.value = (data ?? []).map((d: any) => ({
      ...d,
      label: `${d.serialNo} • ${d.brandCode} ${d.modelCode} • ${d.modelNo}`,
    }));
  } catch {
    deviceItems.value = [];
  } finally {
    deviceLoading.value = false;
  }
}

function onDeviceFocus() {
  if (!selectedCustomer.value) {
    snack.color = "error";
    snack.text = "Önce müşteri seçmelisin.";
    snack.open = true;
    return;
  }

  clearTimeout(deviceTimer);

  const q = (deviceSearch.value || "").trim();
  if (q.length >= 2) {
    loadDevicePicklist(q);
    return;
  }

  if (deviceItems.value.length > 0) return;
  loadDevicePicklist("");
}

let deviceTimer: any = null;
watch(deviceSearch, (v) => {
  clearTimeout(deviceTimer);
  deviceTimer = setTimeout(() => {
    if (!selectedCustomer.value) return;

    const q = v?.trim() || "";
    if (q.length === 0) return loadDevicePicklist("");
    if (q.length < 2) return;
    loadDevicePicklist(q);
  }, 250);
});

watch(selectedCustomer, () => {
  selectedDevice.value = null;
  deviceSearch.value = "";
  deviceItems.value = [];
  form.deviceId = 0;
});

watch(selectedDevice, (d) => {
  form.deviceId = d?.deviceId ?? 0;
});

const queryCustomerId = computed(() => {
  const v = route.query.customerId;
  const n = typeof v === "string" ? Number(v) : NaN;
  return Number.isFinite(n) ? n : null;
});
const queryDeviceId = computed(() => {
  const v = route.query.deviceId;
  const n = typeof v === "string" ? Number(v) : NaN;
  return Number.isFinite(n) ? n : null;
});

watch(
  queryCustomerId,
  async (cid) => {
    if (!cid) return;
    await loadCustomerPicklist(String(cid));
    const found = customerItems.value.find((x) => x.customerId === cid);
    if (found) selectedCustomer.value = found;
  },
  { immediate: true }
);

watch(
  queryDeviceId,
  async (did) => {
    if (!did) return;
    try {
      const { data } = await http.get<DevicePicklistItem>(`/api/devices/${did}`);
      if (data?.customerId && !selectedCustomer.value) {
        await loadCustomerPicklist(String(data.customerId));
        const found = customerItems.value.find((x) => x.customerId === data.customerId);
        if (found) selectedCustomer.value = found;
      }
      selectedDevice.value = {
        ...data,
        label: `${data.serialNo} • ${data.brandCode} ${data.modelCode} • ${data.modelNo}`,
      };
    } catch {
    }
  },
  { immediate: true }
);

function reset() {
  selectedCustomer.value = null;
  customerSearch.value = "";
  customerItems.value = [];

  selectedDevice.value = null;
  deviceSearch.value = "";
  deviceItems.value = [];

  form.deviceId = 0;
  form.faultDesc = "";
  form.internalNotes = "";
  form.publicNote = "";
  form.createdBy = 1;
}

async function submit() {
  if (!selectedCustomer.value) {
    snack.color = "error";
    snack.text = "Önce bir müşteri seçmelisin.";
    snack.open = true;
    return;
  }

  if (!form.deviceId || form.deviceId <= 0) {
    snack.color = "error";
    snack.text = "Önce bir cihaz seçmelisin.";
    snack.open = true;
    return;
  }

  if (!form.faultDesc?.trim()) {
    snack.color = "error";
    snack.text = "Arıza açıklaması zorunlu.";
    snack.open = true;
    return;
  }

  loading.value = true;
  try {
    const res = await http.post("/api/tickets", {
      deviceId: form.deviceId,
      faultDesc: form.faultDesc.trim(),
      internalNotes: form.internalNotes?.trim() || null,
      publicNote: form.publicNote?.trim() || null,
      createdBy: actorUserId.value,
    });

    snack.color = "success";
    snack.text = `Ticket oluşturuldu: #${res.data.ticketId}`;
    snack.open = true;

    reset();
  } catch (e: any) {
    snack.color = "error";
    snack.text = e?.response?.data?.error ?? "Beklenmeyen hata";
    snack.open = true;
  } finally {
    loading.value = false;
  }
}
</script>

<style scoped>
.panel {
  border: 1px solid rgba(255, 255, 255, 0.06);
  background: rgba(255, 255, 255, 0.02);
}
.mono {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace;
}
.ga-2 {
  gap: 8px;
}
</style>
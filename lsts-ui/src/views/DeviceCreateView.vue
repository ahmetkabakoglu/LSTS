<template>
  <div>
    <div class="d-flex align-center justify-space-between mb-4">
      <div>
        <div class="text-h6 font-weight-bold">Device Register</div>
        <div class="text-caption text-medium-emphasis">
          Müşteriye cihaz ekle (serial + model + deviceType)
        </div>
      </div>

      <v-btn variant="tonal" prepend-icon="mdi-account-multiple" @click="goCustomers">
        Customers
      </v-btn>
    </div>

    <v-row>
      <v-col cols="12" lg="8">
        <v-card class="panel" variant="flat">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="text-subtitle-1 font-weight-bold">Yeni Cihaz</div>
            <v-chip color="secondary" variant="tonal" prepend-icon="mdi-laptop">
              LSTS_DEVICES
            </v-chip>
          </v-card-title>

          <v-divider />

          <v-card-text>
            <v-form @submit.prevent="submit">
              <v-row>
                <!-- Customer + Model aynı satır -->
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
                    v-model="selectedModel"
                    v-model:search="modelSearch"
                    :items="modelItems"
                    item-title="label"
                    return-object
                    clearable
                    label="Model seç"
                    :loading="modelLoading"
                    :no-data-text="modelNoDataText"
                    @click:control="onModelFocus"
                    @update:menu="(v) => v && onModelFocus()"
                    prepend-inner-icon="mdi-chip"
                  >
                    <template #item="{ props, item }">
                      <v-list-item v-bind="props">
                        <v-list-item-title class="d-flex align-center ga-2">
                          <span class="font-weight-semibold">{{ item.raw.modelCode }}</span>
                          <v-chip size="x-small" variant="tonal" color="secondary">
                            {{ item.raw.brandCode }}
                          </v-chip>
                          <v-chip size="x-small" variant="tonal" color="info">
                            #{{ item.raw.modelId }}
                          </v-chip>
                        </v-list-item-title>
                        <v-list-item-subtitle>
                          {{ item.raw.modelName }} • {{ item.raw.modelNo }}
                          <span v-if="item.raw.ramSummary"> • RAM: {{ item.raw.ramSummary }}</span>
                          <span v-if="item.raw.screenSummary"> • Screen: {{ item.raw.screenSummary }}</span>
                        </v-list-item-subtitle>
                      </v-list-item>
                    </template>
                  </v-autocomplete>
                </v-col>

                <v-col cols="12" md="6">
                  <v-text-field
                    v-model="form.serialNo"
                    label="Serial No"
                    prepend-inner-icon="mdi-barcode"
                    hint="Aktif cihazlarda unique olmalı"
                    persistent-hint
                    @blur="normalizeSerial"
                  />
                </v-col>

                <v-col cols="12" md="6">
                  <v-select
                    v-model="form.deviceType"
                    :items="deviceTypeItems"
                    label="Device Type"
                    prepend-inner-icon="mdi-shape-outline"
                    hint="Seçim zorunlu"
                    persistent-hint
                  />
                </v-col>

                <v-col cols="12">
                  <v-text-field
                    v-model="form.notes"
                    label="Notes (opsiyonel)"
                    prepend-inner-icon="mdi-note-text-outline"
                    hide-details
                  />
                </v-col>

                <v-col cols="12" class="d-flex ga-2">
                  <v-btn
                    color="primary"
                    type="submit"
                    :loading="saving"
                    prepend-icon="mdi-content-save"
                  >
                    Kaydet
                  </v-btn>

                  <v-btn variant="text" prepend-icon="mdi-refresh" @click="reset">
                    Temizle
                  </v-btn>

                  <v-spacer />

                  <v-btn
                    variant="tonal"
                    prepend-icon="mdi-ticket"
                    :disabled="!lastCreatedDeviceId"
                    @click="goTicketCreate"
                  >
                    Ticket Aç
                  </v-btn>
                </v-col>

                <v-col cols="12" v-if="lastCreatedDeviceId">
                  <v-alert type="success" variant="tonal" border="start">
                    Cihaz oluşturuldu:
                    <b class="mono">#{{ lastCreatedDeviceId }}</b>
                    <span class="ml-2 text-medium-emphasis">
                      ({{ lastCreatedSerial }} • {{ lastCreatedModelLabel }})
                    </span>
                  </v-alert>
                </v-col>
              </v-row>
            </v-form>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" lg="4">
        <v-card class="panel" variant="flat">
          <v-card-title class="text-subtitle-1 font-weight-bold">İpuçları</v-card-title>
          <v-divider />
          <v-card-text class="text-body-2 text-medium-emphasis">
            <ul class="pl-5">
              <li>Önce müşteriyi seç.</li>
              <li>Serial No’yu yapıştır; otomatik büyük harfe çevrilir.</li>
              <li>Model seçimi artık picklist’ten geliyor.</li>
            </ul>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-snackbar v-model="snack.open" :color="snack.color">
      {{ snack.text }}
    </v-snackbar>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { http } from "../api/http";
import { useActor } from "../features/actor/useActor";

type CustomerPick = {
  customerId: number;
  fullName: string;
  phoneDigits: string;
  phoneLast4?: string | null;
  email?: string | null;
  label: string;
};

type ModelPick = {
  modelId: number;
  brandId: number;
  brandCode: string;
  brandName: string;
  modelCode: string;
  modelName: string;
  modelNo: string;
  cpu?: string | null;
  gpu?: string | null;
  ramSummary?: string | null;
  screenSummary?: string | null;
  label: string;
};

const route = useRoute();
const router = useRouter();
const { actorUserId } = useActor();
const snack = reactive({
  open: false,
  text: "",
  color: "success" as "success" | "error" | "info" | "warning",
});

const saving = ref(false);
const lastCreatedDeviceId = ref<number | null>(null);
const lastCreatedSerial = ref<string>("");
const lastCreatedModelLabel = ref<string>("");

const deviceTypeItems = [{ title: "LAPTOP", value: "LAPTOP" }];

const selectedCustomer = ref<CustomerPick | null>(null);
const customerItems = ref<CustomerPick[]>([]);
const customerLoading = ref(false);
const customerSearch = ref("");

let customerTimer: any = null;

function maskPhone(phoneDigits: string) {
  const digits = (phoneDigits || "").replace(/\D/g, "");
  if (!digits) return "-";
  const last4 = digits.slice(-4);
  return `*** *** ${last4}`;
}

async function loadCustomerPicklist(q?: string) {
  customerLoading.value = true;
  try {
    const { data } = await http.get<any[]>("/api/customers/picklist", {
      params: { q: q || null, limit: 50 },
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
  if (q.length >= 2) return loadCustomerPicklist(q);
  if (customerItems.value.length > 0) return;
  return loadCustomerPicklist("");
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

const preCustomerId = computed(() => {
  const v = route.query.customerId;
  const n = typeof v === "string" ? Number(v) : NaN;
  return Number.isFinite(n) ? n : null;
});

watch(
  preCustomerId,
  async (id) => {
    if (!id) return;
    await loadCustomerPicklist(String(id));
    const found = customerItems.value.find((x) => x.customerId === id);
    if (found) selectedCustomer.value = found;
  },
  { immediate: true }
);

const selectedModel = ref<ModelPick | null>(null);
const modelItems = ref<ModelPick[]>([]);
const modelLoading = ref(false);
const modelSearch = ref("");
let modelTimer: any = null;

const modelNoDataText = computed(() => {
  const q = (modelSearch.value || "").trim();
  if (q.length >= 2) return "Sonuç yok";
  return "Aramak için en az 2 karakter yaz veya menüyü aç (son modeller).";
});

async function loadModels(q?: string) {
  modelLoading.value = true;
  try {
    const params: any = { q: q || null, limit: 50 };
    const { data } = await http.get<any[]>("/api/models/picklist", { params });

    modelItems.value = (data ?? []).map((m: any) => ({
      ...m,
      label: `${m.brandCode} • ${m.modelCode} • ${m.modelNo} • #${m.modelId}`,
    }));
  } catch {
    modelItems.value = [];
  } finally {
    modelLoading.value = false;
  }
}

function onModelFocus() {
  clearTimeout(modelTimer);

  const q = (modelSearch.value || "").trim();
  if (q.length >= 2) {
    loadModels(q);
    return;
  }

  if (modelItems.value.length > 0) return;
  loadModels("");
}

watch(modelSearch, (v) => {
  clearTimeout(modelTimer);
  modelTimer = setTimeout(() => {
    const q = v?.trim() || "";
    if (q.length === 0) return loadModels("");
    if (q.length < 2) return;
    loadModels(q);
  }, 250);
});

const form = reactive({
  customerId: 0,
  serialNo: "",
  deviceType: "LAPTOP",
  modelId: 0,
  notes: "",
});

watch(selectedCustomer, (c) => {
  form.customerId = c?.customerId ?? 0;
});

watch(selectedModel, (m) => {
  form.modelId = m?.modelId ?? 0;
});

function normalizeSerial() {
  form.serialNo = (form.serialNo || "").trim().toUpperCase();
}

function reset() {
  form.serialNo = "";
  form.deviceType = "LAPTOP";
  form.notes = "";
  form.modelId = 0;
  selectedModel.value = null;
  modelSearch.value = "";
  modelItems.value = [];
  lastCreatedDeviceId.value = null;
  lastCreatedSerial.value = "";
  lastCreatedModelLabel.value = "";
}

async function submit() {
  if (form.customerId <= 0) {
    snack.color = "error";
    snack.text = "Önce müşteri seçmelisin.";
    snack.open = true;
    return;
  }

  normalizeSerial();
  if (!form.serialNo) {
    snack.color = "error";
    snack.text = "Serial No zorunlu.";
    snack.open = true;
    return;
  }

  if (!form.deviceType?.trim()) {
    snack.color = "error";
    snack.text = "Device Type zorunlu.";
    snack.open = true;
    return;
  }

  if (!form.modelId || form.modelId <= 0) {
    snack.color = "error";
    snack.text = "Model seçmelisin.";
    snack.open = true;
    return;
  }

  saving.value = true;
  try {
    const payload = {
      customerId: form.customerId,
      serialNo: form.serialNo,
      deviceType: form.deviceType.trim(),
      modelId: form.modelId,
      notes: form.notes?.trim() || null,
      createdBy: actorUserId.value,
    };

    const { data } = await http.post("/api/devices", payload);

    const deviceId = data?.deviceId ?? data?.DeviceId ?? null;
    lastCreatedDeviceId.value = typeof deviceId === "number" ? deviceId : null;

    lastCreatedSerial.value = form.serialNo;
    lastCreatedModelLabel.value = selectedModel.value
      ? `${selectedModel.value.brandCode} ${selectedModel.value.modelCode} • ${selectedModel.value.modelNo}`
      : `modelId=${form.modelId}`;

    snack.color = "success";
    snack.text = lastCreatedDeviceId.value
      ? `Cihaz oluşturuldu (#${lastCreatedDeviceId.value})`
      : "Cihaz oluşturuldu";
    snack.open = true;

    form.serialNo = "";
    form.notes = "";
  } catch (e: any) {
    const apiErr = e?.response?.data?.error;

    snack.color = "error";
    snack.text = apiErr ?? "Cihaz oluşturulamadı";
    snack.open = true;
  } finally {
    saving.value = false;
  }
}

function goTicketCreate() {
  const cid = form.customerId;
  const did = lastCreatedDeviceId.value;
  router.push({
    name: "ticket-create",
    query: {
      customerId: String(cid),
      deviceId: did ? String(did) : "",
    },
  });
}

function goCustomers() {
  router.push({ name: "customers" });
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
</style>
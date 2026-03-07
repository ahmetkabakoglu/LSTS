<template>
  <div>
    <div class="d-flex align-center justify-space-between mb-4">
      <div>
        <div class="text-h6 font-weight-bold">Customers</div>
        <div class="text-caption text-medium-emphasis">
          Müşteri oluştur, ara ve ticket/device akışını başlat
        </div>
      </div>

      <v-btn color="primary" prepend-icon="mdi-account-plus" @click="openCreate()">
        Yeni Müşteri
      </v-btn>
    </div>

    <v-row>
      <v-col cols="12" lg="7">
        <v-card class="panel" variant="flat">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="text-subtitle-1 font-weight-bold">Müşteri Listesi</div>
            <v-btn size="small" variant="text" prepend-icon="mdi-refresh" @click="reload()">
              Yenile
            </v-btn>
          </v-card-title>
          <v-divider />

          <v-card-text>
            <v-row>
              <v-col cols="12" md="8">
                <v-text-field
                  v-model="q"
                  label="Ara (isim / telefon / email / last4 / id)"
                  prepend-inner-icon="mdi-magnify"
                  hide-details
                  clearable
                  @keyup.enter="reload()"
                />
              </v-col>
              <v-col cols="12" md="4" class="d-flex align-center justify-end">
                <v-btn variant="tonal" prepend-icon="mdi-magnify" @click="reload()">
                  Ara
                </v-btn>
              </v-col>
            </v-row>

            <div class="mt-2">
              <v-skeleton-loader v-if="loading" type="table" />
              <v-alert
                v-else-if="!items.length"
                type="info"
                variant="tonal"
                border="start"
                class="mt-2"
              >
                Sonuç yok. Yeni müşteri ekleyebilirsin.
              </v-alert>

              <v-table v-else density="comfortable" class="mt-2">
                <thead>
                  <tr>
                    <th class="text-left">#</th>
                    <th class="text-left">Ad Soyad</th>
                    <th class="text-left">Telefon</th>
                    <th class="text-left">Email</th>
                    <th class="text-right">İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="c in items" :key="c.customerId">
                    <td class="mono">{{ c.customerId }}</td>
                    <td class="font-weight-medium">{{ c.fullName }}</td>
                    <td class="mono">{{ maskPhone(c.phoneDigits) }}</td>
                    <td class="text-medium-emphasis">{{ c.email ?? "-" }}</td>
                    <td class="text-right">
                      <v-btn
                        size="small"
                        variant="tonal"
                        prepend-icon="mdi-laptop-plus"
                        @click="goCreateDevice(c.customerId)"
                      >
                        Cihaz Ekle
                      </v-btn>
                    </td>
                  </tr>
                </tbody>
              </v-table>
            </div>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" lg="5">
        <v-card class="panel" variant="flat">
          <v-card-title class="text-subtitle-1 font-weight-bold">Akış</v-card-title>
          <v-divider />
          <v-card-text class="text-body-2 text-medium-emphasis">
            <ol class="pl-5">
              <li>Müşteri oluştur</li>
              <li>Müşteriye cihaz ekle (serial + modelId)</li>
              <li>Ticket oluştur (cihaz seçimi ile)</li>
              <li>Public sorgu: serviceNo + telefon son 4</li>
            </ol>

            <v-divider class="my-4" />

            <v-alert type="warning" variant="tonal" border="start">
              Model picklist endpoint olmadığı için <b>modelId</b> şimdilik manuel girilecek.
              Demo için yeterli; sonra picklist eklenir.
            </v-alert>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-dialog v-model="createDialog" max-width="720" scrim="rgba(0,0,0,0.65)">
      <v-card class="panel panel-solid rounded-xl" elevation="12">
        <v-card-title class="d-flex align-center justify-space-between">
          <div>
            <div class="text-subtitle-1 font-weight-bold">Yeni Müşteri</div>
            <div class="text-caption text-medium-emphasis">
              İsim + telefon zorunlu
            </div>
          </div>
          <v-btn icon variant="text" @click="createDialog = false">
            <v-icon icon="mdi-close" />
          </v-btn>
        </v-card-title>

        <v-divider />

        <v-card-text>
          <v-row>
            <v-col cols="12">
              <v-text-field
                v-model="create.fullName"
                label="Ad Soyad"
                prepend-inner-icon="mdi-account"
                hide-details
              />
            </v-col>

            <v-col cols="12" md="6">
              <v-text-field
                v-model="create.phoneDigits"
                label="Telefon"
                hint="Sadece rakamlar alınır (10-15 hane)"
                persistent-hint
                prepend-inner-icon="mdi-phone"
              />
            </v-col>

            <v-col cols="12" md="6">
              <v-text-field
                v-model="create.email"
                label="Email (opsiyonel)"
                prepend-inner-icon="mdi-email-outline"
                hide-details
              />
            </v-col>

            <v-col cols="12">
              <v-text-field
                v-model="create.addressText"
                label="Adres (opsiyonel)"
                prepend-inner-icon="mdi-map-marker-outline"
                hide-details
              />
            </v-col>

            <v-col cols="12">
              <v-textarea
                v-model="create.notes"
                label="Not (opsiyonel)"
                rows="2"
                auto-grow
              />
            </v-col>
          </v-row>
        </v-card-text>

        <v-divider />

        <v-card-actions class="pa-4">
          <v-spacer />
          <v-btn variant="text" @click="createDialog = false">Vazgeç</v-btn>
          <v-btn color="primary" :loading="saving" prepend-icon="mdi-content-save" @click="submitCreate">
            Kaydet
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-snackbar v-model="snack.open" :color="snack.color">
      {{ snack.text }}
    </v-snackbar>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from "vue";
import { useRouter } from "vue-router";
import { http } from "../api/http";
import { useActor } from "../features/actor/useActor";

type CustomerItem = {
  customerId: number;
  fullName: string;
  phoneDigits: string;
  phoneLast4?: string | null;
  email?: string | null;
};

const router = useRouter();
const { actorUserId } = useActor();
const q = ref("");
const loading = ref(false);
const items = ref<CustomerItem[]>([]);

const createDialog = ref(false);
const saving = ref(false);

const snack = reactive({
  open: false,
  text: "",
  color: "success" as "success" | "error" | "info" | "warning",
});

const create = reactive({
  fullName: "",
  phoneDigits: "",
  email: "",
  addressText: "",
  notes: "",
});

function maskPhone(phoneDigits: string) {
  const digits = (phoneDigits || "").replace(/\D/g, "");
  if (!digits) return "-";
  const last4 = digits.slice(-4);
  return `*** *** ${last4}`;
}

function openCreate() {
  create.fullName = "";
  create.phoneDigits = "";
  create.email = "";
  create.addressText = "";
  create.notes = "";
  createDialog.value = true;
}

async function reload() {
  loading.value = true;
  try {
    const { data } = await http.get<CustomerItem[]>("/api/customers/picklist", {
      params: { q: q.value?.trim() || null, limit: 50 },
    });
    items.value = data ?? [];
  } catch (e: any) {
    items.value = [];
    snack.color = "error";
    snack.text = e?.response?.data?.error ?? "Müşteri listesi alınamadı";
    snack.open = true;
  } finally {
    loading.value = false;
  }
}

async function submitCreate() {
  if (!create.fullName.trim()) {
    snack.color = "error";
    snack.text = "Ad Soyad zorunlu";
    snack.open = true;
    return;
  }
  if (!create.phoneDigits.trim()) {
    snack.color = "error";
    snack.text = "Telefon zorunlu";
    snack.open = true;
    return;
  }

  saving.value = true;
  try {
  const payload = {
  fullName: create.fullName.trim(),
  phoneDigits: create.phoneDigits.trim(),
  email: create.email?.trim() || null,
  addressText: create.addressText?.trim() || null,
  notes: create.notes?.trim() || null,
  createdBy: actorUserId.value,
};

    const { data } = await http.post("/api/customers", payload);

    snack.color = "success";
    snack.text = `Müşteri oluşturuldu (#${data.customerId})`;
    snack.open = true;

    createDialog.value = false;
    await reload();
  } catch (e: any) {
    snack.color = "error";
    snack.text = e?.response?.data?.error ?? "Müşteri oluşturulamadı";
    snack.open = true;
  } finally {
    saving.value = false;
  }
}

function goCreateDevice(customerId: number) {
  router.push({ name: "device-create", query: { customerId: String(customerId) } });
}

reload();
</script>

<style scoped>
.panel {
  border: 1px solid rgba(255, 255, 255, 0.06);
  background: rgba(255, 255, 255, 0.02);
}
.mono {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace;
}
.panel-solid {
  background: rgb(var(--v-theme-surface)) !important;
}
</style>
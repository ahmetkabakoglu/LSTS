<template>
  <div class="auth-page">
    <v-container class="fill-height" fluid>
      <v-row align="center" justify="center" class="py-10">
        <v-col cols="12" sm="10" md="7" lg="5" xl="4">
          <v-card class="auth-card rounded-xl" elevation="10">
            <v-card-title class="px-6 pt-6 pb-2">
              <div class="d-flex align-center justify-space-between w-100">
                <div>
                  <div class="text-h6 font-weight-semibold">Ticket Sorgulama</div>
                  <div class="text-caption text-medium-emphasis">
                    Servis / Seri No + Telefon son 4
                  </div>
                </div>

                <v-btn
                  variant="text"
                  density="comfortable"
                  icon="mdi-arrow-left"
                  @click="goLogin"
                />
              </div>
            </v-card-title>

            <v-card-text class="px-6 pt-4">
              <v-tabs v-model="mode" density="compact" class="mb-4">
                <v-tab value="service">Servis No</v-tab>
                <v-tab value="serial">Seri No</v-tab>
              </v-tabs>

              <v-alert
                v-if="error"
                type="error"
                variant="tonal"
                border="start"
                density="comfortable"
                class="mb-4"
              >
                {{ error }}
              </v-alert>

              <v-text-field
                v-if="mode === 'service'"
                v-model="serviceNo"
                label="Servis No"
                variant="outlined"
                density="comfortable"
                prepend-inner-icon="mdi-ticket-outline"
                class="mb-3"
                :disabled="loading"
              />
              <v-text-field
                v-else
                v-model="serialNo"
                label="Seri No"
                variant="outlined"
                density="comfortable"
                prepend-inner-icon="mdi-barcode-scan"
                class="mb-3"
                :disabled="loading"
              />

              <v-text-field
                v-model="last4"
                label="Telefon son 4"
                maxlength="4"
                variant="outlined"
                density="comfortable"
                prepend-inner-icon="mdi-phone"
                class="mb-4"
                :disabled="loading"
              />

              <div class="d-flex ga-3">
                <v-btn
                  color="primary"
                  size="large"
                  class="rounded-lg"
                  :loading="loading"
                  :disabled="!canSearch || loading"
                  @click="onSearch"
                >
                  Sorgula
                </v-btn>

                <v-btn
                  size="large"
                  variant="tonal"
                  class="rounded-lg"
                  :disabled="loading"
                  @click="onReset"
                >
                  Temizle
                </v-btn>
              </div>

              <v-divider class="my-5" />

              <div>
                <v-skeleton-loader v-if="loading" type="article" />

                <v-card v-else-if="result" variant="tonal" class="rounded-xl pa-4">
                  <div class="d-flex align-center justify-space-between">
                    <div class="text-subtitle-1 font-weight-bold">
                      {{ result.serviceNo }}
                    </div>

                    <v-chip :color="statusColor(result.currentStatus)" variant="flat">
                      {{ result.currentStatus }}
                    </v-chip>
                  </div>

                  <div class="text-body-2 mt-3 text-medium-emphasis">
                    Son güncelleme: {{ formatDate(result.statusUpdatedAt) }}
                  </div>

                  <div
                    v-if="result.estimatedDeliveryDate"
                    class="text-body-2 mt-1 text-medium-emphasis"
                  >
                    Tahmini teslim: {{ formatDate(result.estimatedDeliveryDate) }}
                  </div>

                  <v-alert
                    v-if="result.publicNote"
                    type="info"
                    variant="tonal"
                    density="comfortable"
                    class="mt-4"
                  >
                    {{ result.publicNote }}
                  </v-alert>
                </v-card>

                <div v-else class="empty">
                  <v-icon icon="mdi-information-outline" class="mr-2" />
                  <span class="text-body-2 text-medium-emphasis">
                    Sonuç burada görünecek.
                  </span>
                </div>
              </div>
            </v-card-text>

            <v-divider />

            <v-card-actions class="px-6 py-4">
              <div class="d-flex align-center justify-space-between w-100 text-caption text-medium-emphasis">
                <span>Public View</span>
                <RouterLink to="/login" class="link">Giriş sayfası</RouterLink>
              </div>
            </v-card-actions>
          </v-card>

          <div class="text-caption text-medium-emphasis text-center mt-4">
            Bu sayfa login gerektirmez.
          </div>
        </v-col>
      </v-row>
    </v-container>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useRouter } from "vue-router";
import { publicApi, type PublicTicketStatusDto } from "../api/lsts";

const router = useRouter();

const mode = ref<"service" | "serial">("service");
const serviceNo = ref("");
const serialNo = ref("");
const last4 = ref("");

const loading = ref(false);
const error = ref<string | null>(null);
const result = ref<PublicTicketStatusDto | null>(null);

const canSearch = computed(() => {
  const l4 = last4.value.trim();
  if (l4.length !== 4) return false;
  if (mode.value === "service") return serviceNo.value.trim().length > 0;
  return serialNo.value.trim().length > 0;
});

function goLogin() {
  router.push("/login");
}

function formatDate(v: string) {
  try {
    return new Date(v).toLocaleString("tr-TR");
  } catch {
    return v;
  }
}

function statusColor(status: string) {
  const s = (status || "").toUpperCase();
  if (s.includes("CLOSED") || s.includes("DELIVER")) return "success";
  if (s.includes("WAIT") || s.includes("HOLD")) return "warning";
  if (s.includes("CANCEL")) return "error";
  return "primary";
}

function onReset() {
  error.value = null;
  result.value = null;
  serviceNo.value = "";
  serialNo.value = "";
  last4.value = "";
}

async function onSearch() {
  error.value = null;
  result.value = null;

  const l4 = last4.value.trim();
  if (l4.length !== 4) {
    error.value = "Telefon son 4 hane 4 karakter olmalı.";
    return;
  }

  loading.value = true;
  try {
    if (mode.value === "service") {
      result.value = await publicApi.byService(serviceNo.value.trim(), l4);
    } else {
      result.value = await publicApi.bySerial(serialNo.value.trim(), l4);
    }
  } catch (e: any) {
    error.value = e?.message ?? "Sorgu başarısız.";
  } finally {
    loading.value = false;
  }
}
</script>

<style scoped>
.auth-page {
  min-height: 100vh;
  background:
    radial-gradient(900px 520px at 18% 0%, rgba(34, 197, 94, 0.10), rgba(0,0,0,0) 55%),
    radial-gradient(900px 520px at 88% 18%, rgba(56, 189, 248, 0.08), rgba(0,0,0,0) 55%),
    linear-gradient(180deg, #0a0c10 0%, #07080b 60%, #06070a 100%);
}

.auth-card {
  border: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(10, 12, 16, 0.82);
  backdrop-filter: blur(10px);
}

.empty {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 18px 0;
}

.link {
  color: rgba(255, 255, 255, 0.75);
  text-decoration: none;
}
.link:hover {
  color: rgba(255, 255, 255, 0.95);
}
</style>
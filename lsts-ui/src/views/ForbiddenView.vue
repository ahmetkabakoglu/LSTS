<template>
  <v-container class="forbidden">
    <v-row justify="center">
      <v-col cols="12" md="9" lg="7">
        <v-card class="card" elevation="0">
          <div class="header">
            <div class="icon">
              <v-icon icon="mdi-shield-alert" />
            </div>

            <div class="title-wrap">
              <div class="title">Yetkiniz yok</div>
              <div class="subtitle">
                Bu sayfaya erişmek için gerekli rolünüz bulunmuyor.
              </div>
            </div>

            <v-spacer />

            <v-chip color="error" variant="tonal" class="code">403</v-chip>
          </div>

          <v-divider class="my-4" />

          <v-row class="info" align="start">
            <v-col cols="12" md="6">
              <div class="k">Kullanıcı</div>
              <div class="v">{{ auth.username || "—" }}</div>

              <div class="k mt">Rol(ler)</div>
              <div class="chips">
                <v-chip
                  v-for="r in (auth.roles ?? [])"
                  :key="r"
                  variant="tonal"
                  class="chip"
                >
                  {{ r }}
                </v-chip>
                <span v-if="!(auth.roles?.length)" class="muted">—</span>
              </div>
            </v-col>

            <v-col cols="12" md="6" v-if="requiredRoles.length">
              <div class="k">Gerekli rol(ler)</div>
              <div class="chips">
                <v-chip
                  v-for="r in requiredRoles"
                  :key="r"
                  color="warning"
                  variant="tonal"
                  class="chip"
                >
                  {{ r }}
                </v-chip>
              </div>

              <div class="k mt">Hedef</div>
              <div class="mono">{{ fromPath }}</div>
            </v-col>
          </v-row>

          <v-divider class="my-4" />

          <div class="actions">
            <v-btn variant="tonal" prepend-icon="mdi-arrow-left" @click="router.back()">
              Geri dön
            </v-btn>

            <v-btn
              color="success"
              prepend-icon="mdi-account-switch"
              @click="switchAccount"
            >
              Hesap değiştir
            </v-btn>

            <v-spacer />

            <v-btn variant="text" prepend-icon="mdi-logout" @click="logout">
              Çıkış
            </v-btn>
          </div>
        </v-card>

        <div class="hint">
          Eğer bu sayfanın açılması bekleniyorsa, route meta roles veya API policy tarafını kontrol edelim.
        </div>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useAuthStore } from "../stores/auth";
import type { Role } from "../api/lsts";

const router = useRouter();
const route = useRoute();
const auth = useAuthStore();

const requiredRoles = computed(() => (route.meta.roles as Role[] | undefined) ?? []);
const fromPath = computed(() => String(route.query.from ?? route.fullPath ?? "/"));

function logout() {
  auth.logout();
  router.replace({ name: "login" });
}

function switchAccount() {
  
  auth.logout();
  router.replace({ name: "login", query: { redirect: fromPath.value } });
}
</script>

<style scoped>
.forbidden {
  padding-top: 24px;
}

.card {
  border-radius: 22px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(255, 255, 255, 0.03);
  backdrop-filter: blur(10px);
  box-shadow: 0 18px 60px rgba(0, 0, 0, 0.35);
  padding: 18px;
}

.header {
  display: flex;
  align-items: center;
  gap: 14px;
}

.icon {
  width: 44px;
  height: 44px;
  border-radius: 16px;
  display: grid;
  place-items: center;
  border: 1px solid rgba(248, 113, 113, 0.25);
  background: rgba(248, 113, 113, 0.08);
}

.title {
  font-size: 20px;
  font-weight: 900;
  letter-spacing: 0.2px;
}

.subtitle {
  font-size: 13px;
  opacity: 0.75;
  margin-top: 2px;
}

.code {
  font-weight: 900;
}

.info .k {
  font-size: 12px;
  opacity: 0.7;
  margin-bottom: 6px;
}

.info .v {
  font-weight: 800;
  margin-bottom: 10px;
}

.mt {
  margin-top: 12px;
}

.chips {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.chip {
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.10);
}

.mono {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
  font-size: 12px;
  opacity: 0.9;
  word-break: break-all;
}

.actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.hint {
  margin-top: 12px;
  text-align: center;
  font-size: 12px;
  opacity: 0.65;
}

.muted {
  opacity: 0.7;
}
</style>
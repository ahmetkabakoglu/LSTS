<template>
  <div class="auth-page">
    <v-container class="fill-height" fluid>
      <v-row align="center" justify="center" class="py-10">
        <v-col cols="12" sm="10" md="6" lg="4" xl="3">
          <v-card class="auth-card rounded-xl" elevation="10">
            <v-card-title class="px-6 pt-6 pb-2">
              <div class="d-flex align-center justify-space-between w-100">
                <div>
                  <div class="text-h6 font-weight-semibold">LSTS</div>
                  <div class="text-caption text-medium-emphasis">
                    Laptop Servis Takip Sistemi
                  </div>
                </div>

                <v-avatar size="36" class="bg-surface-variant">
                  <v-icon icon="mdi-laptop" />
                </v-avatar>
              </div>
            </v-card-title>

            <v-card-text class="px-6 pt-4">
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

              <v-form @submit.prevent="onLogin">
                <v-text-field
                  v-model="username"
                  label="Kullanıcı adı"
                  autocomplete="username"
                  variant="outlined"
                  density="comfortable"
                  prepend-inner-icon="mdi-account"
                  class="mb-3"
                  :disabled="loading"
                />

                <v-text-field
                  v-model="password"
                  label="Şifre"
                  :type="showPassword ? 'text' : 'password'"
                  autocomplete="current-password"
                  variant="outlined"
                  density="comfortable"
                  prepend-inner-icon="mdi-lock"
                  :append-inner-icon="showPassword ? 'mdi-eye-off' : 'mdi-eye'"
                  @click:append-inner="showPassword = !showPassword"
                  class="mb-2"
                  :disabled="loading"
                />

                <div class="d-flex align-center justify-space-between mt-2 mb-4">
                  <v-checkbox
                    v-model="remember"
                    density="compact"
                    hide-details
                    label="Beni hatırla"
                    :disabled="loading"
                  />

                  <RouterLink to="/track" class="link">
                    Ticket sorgula
                  </RouterLink>
                </div>

                <v-btn
                  color="primary"
                  size="large"
                  block
                  class="rounded-lg"
                  type="submit"
                  :loading="loading"
                  :disabled="!canSubmit || loading"
                >
                  Giriş yap
                </v-btn>
              </v-form>
            </v-card-text>

            <v-divider />

            <v-card-actions class="px-6 py-4">
              <div class="d-flex align-center justify-space-between w-100 text-caption text-medium-emphasis">
                <span>Local Dev • JWT • RBAC</span>
                <RouterLink to="/track" class="link">Public sorgu</RouterLink>
              </div>
            </v-card-actions>
          </v-card>

          <div class="text-caption text-medium-emphasis text-center mt-4">
            
          </div>
        </v-col>
      </v-row>
    </v-container>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useAuthStore } from "../stores/auth";

const auth = useAuthStore();
const router = useRouter();
const route = useRoute();

const username = ref("");
const password = ref("");
const remember = ref(true);

const loading = ref(false);
const error = ref<string | null>(null);

const showPassword = ref(false);

const canSubmit = computed(
  () => username.value.trim().length > 0 && password.value.trim().length > 0
);

async function onLogin() {
  error.value = null;
  loading.value = true;
  try {
    await auth.login(username.value.trim(), password.value, remember.value);
    const redirect = (route.query.redirect as string) || "/";
    await router.replace(redirect);
  } catch (e: any) {
    error.value =
      e?.response?.status === 401 ? "Kullanıcı adı/şifre hatalı." : "Giriş başarısız.";
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

.link {
  color: rgba(255, 255, 255, 0.75);
  text-decoration: none;
}
.link:hover {
  color: rgba(255, 255, 255, 0.95);
}
</style>
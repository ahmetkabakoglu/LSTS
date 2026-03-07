<template>
  <v-app>
    <template v-if="!showShell">
      <v-main class="main">
        <v-container fluid class="py-6 px-6 page">
          <router-view />
        </v-container>
      </v-main>
    </template>


    <v-layout v-else class="app-shell">
      <v-navigation-drawer
        v-model="drawer"
        :rail="rail"
        :rail-width="84"
        :width="288"
        permanent
        class="nav"
        :class="{ rail }"
      >
        <div class="brand-wrap">
          <v-avatar size="38" color="primary" variant="flat" class="brand-avatar">
            <v-icon icon="mdi-laptop" />
          </v-avatar>

          <div class="brand-text" v-show="!rail">
            <div class="text-subtitle-1 font-weight-semibold">LSTS</div>
            <div class="text-caption text-medium-emphasis">Laptop Servis Sistemi</div>
          </div>

          <v-btn
            class="rail-toggle"
            icon
            variant="text"
            size="small"
            @click="rail = !rail"
            :title="rail ? 'Genişlet' : 'Daralt'"
          >
            <v-icon :icon="rail ? 'mdi-chevron-right' : 'mdi-chevron-left'" />
          </v-btn>
        </div>

        <v-divider class="mx-3 my-2" />

        <div v-if="rail" class="rail-links">
          <RouterLink
            v-for="item in visibleNavItems"
            :key="item.to"
            :to="item.to"
            custom
            v-slot="{ navigate, isActive }"
          >
            <v-tooltip location="right" :text="item.title">
              <template #activator="{ props }">
                <v-btn
                  v-bind="props"
                  class="rail-link"
                  :class="{ active: isActive }"
                  icon
                  variant="text"
                  @click="navigate"
                >
                  <v-icon :icon="item.icon" />
                </v-btn>
              </template>
            </v-tooltip>
          </RouterLink>
        </div>

        <v-list v-else nav density="compact" class="nav-list">
          <v-list-item
            v-for="item in visibleNavItems"
            :key="item.to"
            class="nav-item"
            :to="item.to"
            exact
          >
            <template #prepend>
              <v-icon :icon="item.icon" class="nav-icon" />
            </template>

            <v-list-item-title class="nav-title">{{ item.title }}</v-list-item-title>
          </v-list-item>
        </v-list>

        <template #append>
          <div class="pa-3">
            <v-card variant="tonal" class="env-card">
              <v-card-text class="d-flex align-center justify-space-between">
                <div v-show="!rail">
                  <div class="text-caption text-medium-emphasis">Ortam</div>
                  <div class="text-subtitle-2 font-weight-medium">Local Dev</div>
                </div>
                <v-chip color="success" variant="flat" size="small">OK</v-chip>
              </v-card-text>
            </v-card>
          </div>
        </template>
      </v-navigation-drawer>
      
      <v-main class="main">
        <v-app-bar flat class="topbar">
          <v-toolbar-title class="font-weight-semibold">
            Laptop Servis Takip Sistemi
          </v-toolbar-title>

          <v-spacer />

          <v-btn variant="text" prepend-icon="mdi-bell-outline">Bildirimler</v-btn>

          <v-menu v-model="userMenu">
            <template #activator="{ props }">
              <v-btn v-bind="props" variant="text" prepend-icon="mdi-account">
                {{ userLabel }}
              </v-btn>
            </template>

            <v-list density="compact">
              <v-list-item prepend-icon="mdi-account-circle" title="Profil" />
              <v-list-item prepend-icon="mdi-cog" title="Ayarlar" />
              <v-divider />
              <v-list-item prepend-icon="mdi-logout" title="Çıkış" @click="onLogout" />
            </v-list>
          </v-menu>
        </v-app-bar>

        <v-container fluid class="py-6 px-6 page">
          <router-view />
        </v-container>
      </v-main>
    </v-layout>
  </v-app>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useAuthStore } from "./stores/auth";
import type { Role } from "./api/lsts";

const drawer = ref(true);
const rail = ref(false);
const userMenu = ref(false);

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();


const showShell = computed(() => auth.isAuthenticated && !route.meta.hideShell);


const userLabel = computed(() => {
  const roles = auth.roles ?? [];
  if (roles.includes("Admin")) return "Admin";
  return roles[0] ?? "Hesap";
});

type NavItem = { to: string; title: string; icon: string; roles?: Role[] };

const navItems: NavItem[] = [
  { to: "/", title: "Dashboard", icon: "mdi-view-dashboard" },

  { to: "/customers", title: "Customers", icon: "mdi-account-multiple", roles: ["Admin", "Intake"] },
  { to: "/devices/create", title: "Device Register", icon: "mdi-laptop-account", roles: ["Admin", "Intake"] },

  { to: "/tickets", title: "Tickets", icon: "mdi-ticket", roles: ["Admin", "Supervisor", "Intake", "Technician", "Warehouse"] },
  { to: "/parts", title: "Parts", icon: "mdi-cog", roles: ["Admin", "Warehouse", "Supervisor", "Technician"] },
  { to: "/part-requests", title: "Part Requests", icon: "mdi-clipboard-list", roles: ["Admin", "Supervisor", "Technician", "Warehouse"] },
];

const visibleNavItems = computed(() =>
  navItems.filter((x) => !x.roles || auth.hasAnyRole(x.roles))
);

function onLogout() {
  userMenu.value = false;
  auth.logout();
  router.replace({ name: "login" });
}
</script>

<style scoped>
.app-shell { min-height: 100vh; }

.main {
  background:
    radial-gradient(900px 420px at 12% 0%, rgba(124, 58, 237, 0.10), rgba(0, 0, 0, 0) 55%),
    radial-gradient(760px 360px at 88% 12%, rgba(56, 189, 248, 0.06), rgba(0, 0, 0, 0) 55%),
    linear-gradient(180deg, #0a0c10 0%, #07080b 60%, #06070a 100%);
}
.page { background: transparent; }

.nav {
  border-right: 1px solid rgba(255, 255, 255, 0.06);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.04), rgba(255, 255, 255, 0.02));
}

.topbar {
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  backdrop-filter: blur(10px);
  background: rgba(10, 12, 16, 0.65);
}

.brand-wrap {
  margin: 14px 12px 8px;
  padding: 12px;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.06);
  background: rgba(255, 255, 255, 0.03);
  display: flex;
  align-items: center;
  gap: 12px;
}
.rail-toggle { margin-left: auto; }

.nav.rail .brand-wrap { flex-direction: column; align-items: center; gap: 10px; padding: 10px 8px; }
.nav.rail .rail-toggle { margin-left: 0; }

.rail-links { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 6px 0 10px; }

.rail-link {
  width: 52px;
  height: 52px;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(255, 255, 255, 0.02);
  transition: transform 120ms ease, background 120ms ease, border-color 120ms ease, box-shadow 120ms ease;
}
.rail-link:hover { background: rgba(255, 255, 255, 0.04); transform: translateY(-1px); }
.rail-link.active {
  background: rgba(124, 58, 237, 0.16);
  border-color: rgba(124, 58, 237, 0.45);
  box-shadow: 0 0 0 5px rgba(124, 58, 237, 0.1), 0 12px 24px rgba(0, 0, 0, 0.28);
}

:deep(.nav-list) { padding: 0; }
:deep(.nav-item) {
  border-radius: 16px;
  margin: 8px 10px;
  min-height: 46px;
  border: 1px solid rgba(255, 255, 255, 0.06);
  background: rgba(255, 255, 255, 0.02);
}
:deep(.nav-item:hover) { background: rgba(255, 255, 255, 0.04); }
.nav-icon { opacity: 0.92; }
:deep(.nav-item.v-list-item--active) { background: rgba(124, 58, 237, 0.1); border-color: rgba(124, 58, 237, 0.2); }
:deep(.nav-item.v-list-item--active .nav-icon) { color: rgb(167, 139, 250); opacity: 1; }
.nav-title { font-weight: 600; }

.env-card { border: 1px solid rgba(255, 255, 255, 0.06); }
</style>
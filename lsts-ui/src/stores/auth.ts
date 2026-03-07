import { defineStore } from "pinia";
import { ref, computed } from "vue";
import { authApi, type Role } from "../api/lsts";

const LS_TOKEN = "lsts.auth.token";
const LS_USER = "lsts.auth.user";
const LS_ROLES = "lsts.auth.roles";

function getStored(key: string): string | null {
  return localStorage.getItem(key) ?? sessionStorage.getItem(key);
}

function clearStored(key: string) {
  localStorage.removeItem(key);
  sessionStorage.removeItem(key);
}

export const useAuthStore = defineStore("auth", () => {
  const token = ref<string | null>(getStored(LS_TOKEN));
  const username = ref<string | null>(getStored(LS_USER));
  const roles = ref<Role[]>(
    (() => {
      try {
        return JSON.parse(getStored(LS_ROLES) ?? "[]");
      } catch {
        return [];
      }
    })()
  );

  const initialized = ref(false);

  const isAuthenticated = computed(() => !!token.value);

  function persist(remember: boolean) {
    clearStored(LS_TOKEN);
    clearStored(LS_USER);
    clearStored(LS_ROLES);

    const target = remember ? localStorage : sessionStorage;

    if (token.value) target.setItem(LS_TOKEN, token.value);
    if (username.value) target.setItem(LS_USER, username.value);
    target.setItem(LS_ROLES, JSON.stringify(roles.value ?? []));
  }

  function setSession(
    next: { token: string; username: string; roles: Role[] },
    remember: boolean
  ) {
    token.value = next.token;
    username.value = next.username;
    roles.value = next.roles ?? [];
    persist(remember);
  }

  function logout() {
    token.value = null;
    username.value = null;
    roles.value = [];
    clearStored(LS_TOKEN);
    clearStored(LS_USER);
    clearStored(LS_ROLES);
  }

  function hasAnyRole(required?: Role[]) {
    if (!required || required.length === 0) return true;
    if (roles.value.includes("Admin")) return true;
    return required.some((r) => roles.value.includes(r));
  }

  async function login(u: string, p: string, remember = true) {
    const { data } = await authApi.login(u, p);
    setSession({ token: data.token, username: data.username, roles: data.roles }, remember);
  }

  async function initOnce() {
    if (initialized.value) return;
    initialized.value = true;

    if (!token.value) return;

    try {
      const { data } = await authApi.me();
      username.value = data.username ?? username.value;
      roles.value = data.roles ?? roles.value;
      const remembered = !!localStorage.getItem(LS_TOKEN);
      persist(remembered);
    } catch {
      logout();
    }
  }

  return {
    token,
    username,
    roles,
    isAuthenticated,
    hasAnyRole,
    login,
    logout,
    initOnce,
  };
});
import { createRouter, createWebHistory } from "vue-router";
import { useAuthStore } from "../stores/auth";
import type { Role } from "../api/lsts";
import { pinia } from "../shared/pinia";

declare module "vue-router" {
  interface RouteMeta {
    public?: boolean;
    roles?: Role[];
    hideShell?: boolean;
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: "/",
      name: "home",
      component: () => import("../views/HomeView.vue"),
      meta: { roles: ["Admin", "Supervisor", "Intake", "Technician", "Warehouse"] },
    },
    { path: "/login", name: "login", component: () => import("../views/LoginView.vue"), meta: { public: true, hideShell: true } },

    { path: "/track", name: "track", component: () => import("../views/PublicLookupView.vue"), meta: { public: true, hideShell: true } },

    { path: "/forbidden", name: "forbidden", component: () => import("../views/ForbiddenView.vue"), meta: { public: true } },

    {
      path: "/tickets",
      name: "tickets",
      component: () => import("../views/TicketsView.vue"),
      meta: { roles: ["Admin", "Supervisor", "Intake", "Technician", "Warehouse"] },
    },
    {
      path: "/tickets/create",
      name: "ticket-create",
      component: () => import("../views/TicketCreateView.vue"),
      meta: { roles: ["Admin", "Supervisor","Intake"] },
    },
    {
      path: "/tickets/:ticketId/status",
      name: "ticket-status",
      component: () => import("../views/TicketStatusView.vue"),
      meta: { roles: ["Admin", "Supervisor", "Technician"] },
    },
    {
      path: "/tickets/:ticketId/dashboard",
      name: "ticket-dashboard",
      component: () => import("../views/TicketDashboardView.vue"),
      props: true,
      meta: { roles: ["Admin", "Supervisor", "Intake", "Technician", "Warehouse"] },
    },
    {
      path: "/parts",
      name: "parts",
      component: () => import("../views/PartsView.vue"),
      meta: { roles: ["Admin", "Warehouse", "Supervisor", "Technician"] },
    },
    {
      path: "/part-requests",
      name: "part-requests",
      component: () => import("../views/PartRequestsView.vue"),
      meta: { roles: ["Admin", "Supervisor", "Technician", "Warehouse"] },
    },
    {
      path: "/customers",
      name: "customers",
      component: () => import("../views/CustomersView.vue"),
      meta: { roles: ["Admin", "Intake"] },
    },
    {
      path: "/devices/create",
      name: "device-create",
      component: () => import("../views/DeviceCreateView.vue"),
      meta: { roles: ["Admin", "Intake"] },
    },

    { path: "/:pathMatch(.*)*", redirect: "/" },
  ],
});

router.beforeEach(async (to) => {
  const auth = useAuthStore(pinia);
  await auth.initOnce();

  if (to.meta.public) return true;

  if (!auth.isAuthenticated) {
    return { name: "login", query: { redirect: to.fullPath } };
  }

  if (!auth.hasAnyRole(to.meta.roles)) {
    return { name: "forbidden" };
  }

  return true;
});

export default router;
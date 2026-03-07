import { computed, ref } from "vue";
import { http } from "../../api/http";
import { useActor } from "../actor/useActor";
import { lsts } from "../../api/lsts";

export type PartRequestStatus = string;

export interface DashboardTicket {
  ticketId: number;
  serviceNo?: string | null;
  deviceId?: number | null;
  status: string;
  statusUpdatedAt?: string | null;
  faultDesc?: string | null;
  internalNotes?: string | null;
  publicNote?: string | null;
  estimatedDeliveryDate?: string | null;
}

export interface DashboardDevice {
  deviceId?: number | null;
  modelCode?: string | null;
  modelName?: string | null;
  brandCode?: string | null;
  serialNo?: string | null;
  deviceType?: string | null;
  customerMasked?: string | null;
}

export interface ModelSpecs {
  ram?: string | null;
  display?: string | null;
  cpu?: string | null;
  gpu?: string | null;
  storage?: string | null;
}

export interface PartRequestItem {
  itemId: number;
  partId: number;
  partCode?: string | null;
  partName?: string | null;
  qty: number;
  issuedQty?: number | null;
  status?: string | null;
  note?: string | null;
}

export interface PartRequest {
  requestId: number;
  status: PartRequestStatus;
  note?: string | null;
  requestedBy?: number | null;
  requestedAt?: string | null;
  createdBy?: number | null;
  createdAt?: string | null;
  items: PartRequestItem[];
}

export interface TicketDashboard {
  ticket: DashboardTicket;
  device?: DashboardDevice | null;
  modelSpecs?: ModelSpecs | null;
  partRequests: PartRequest[];
}

export interface PartSearchItem {
  partId: number;
  code: string;
  name: string;
}

function safeArray<T>(v: any): T[] {
  return Array.isArray(v) ? (v as T[]) : [];
}

function mapTicketDashboard(api: any): TicketDashboard {
  const t = api?.ticket ?? {};

  const ticket: DashboardTicket = {
    ticketId: Number(t?.ticketId ?? 0),
    serviceNo: t?.serviceNo ?? null,
    deviceId: t?.deviceId ?? null,
    status: String(t?.currentStatus ?? t?.status ?? ""),
    statusUpdatedAt: t?.statusUpdatedAt ?? null,
    faultDesc: t?.faultDesc ?? null,
    internalNotes: t?.internalNotes ?? null,
    publicNote: t?.publicNote ?? null,
    estimatedDeliveryDate: t?.estimatedDeliveryDate ?? null,
  };

  const prsRaw = safeArray<any>(api?.partRequests);
  const partRequests: PartRequest[] = prsRaw.map((r) => ({
    requestId: Number(r?.requestId ?? 0),
    status: String(r?.requestStatus ?? r?.status ?? ""),
    note: r?.note ?? null,
    requestedBy: r?.requestedBy ?? null,
    requestedAt: r?.requestedAt ?? null,
    createdBy: r?.createdBy ?? null,
    createdAt: r?.createdAt ?? null,
    items: safeArray<any>(r?.items).map((it) => ({
      itemId: Number(it?.itemId ?? it?.id ?? 0),
      partId: Number(it?.partId ?? 0),
      partCode: it?.partCode ?? it?.code ?? null,
      partName: it?.partName ?? it?.name ?? null,
      qty: Number(it?.qtyRequested ?? it?.qty ?? it?.quantity ?? 1),
      issuedQty: it?.qtyIssued ?? it?.issuedQty ?? it?.issued_quantity ?? null,
      status: it?.status ?? null,
      note: it?.note ?? null,
    })),
  }));

  return { ticket, partRequests, device: null, modelSpecs: null };
}

function mapDeviceSummary(ds: any): { device: DashboardDevice | null; modelSpecs: ModelSpecs | null } {
  if (!ds) return { device: null, modelSpecs: null };

  const device: DashboardDevice = {
    deviceId: ds?.deviceId ?? null,
    serialNo: ds?.serialNo ?? null,
    modelCode: ds?.modelNo ?? ds?.modelCode ?? null,
    modelName: ds?.modelName ?? null,
    brandCode: ds?.brandCode ?? null,
    deviceType: ds?.deviceType ?? null,
    customerMasked: ds?.customerId ? `CUST-${ds.customerId}` : null,
  };

  const modelSpecs: ModelSpecs = {
    cpu: ds?.cpu ?? null,
    gpu: ds?.gpu ?? null,
    ram: ds?.ramSummary ?? null,
    display: ds?.screenSummary ?? null,
    storage: ds?.storageSummary ?? ds?.diskSummary ?? null,
  };

  const empty =
    !modelSpecs.ram && !modelSpecs.display && !modelSpecs.cpu && !modelSpecs.gpu && !modelSpecs.storage;

  return { device, modelSpecs: empty ? null : modelSpecs };
}

export function useTicketDashboard() {
  const { actorUserId } = useActor();

  const loading = ref(false);
  const error = ref<string | null>(null);
  const data = ref<TicketDashboard | null>(null);

  const ticketId = computed(() => data.value?.ticket.ticketId ?? 0);

  async function load(id: number) {
    loading.value = true;
    error.value = null;

    try {
      const res = await http.get(`/api/tickets/${id}/dashboard`);
      const base = mapTicketDashboard(res.data);

      const deviceId = base.ticket.deviceId;
      if (deviceId && Number(deviceId) > 0) {
        try {
          const dsRes = await http.get(`/api/devices/${deviceId}/summary`);
          const mapped = mapDeviceSummary(dsRes.data);
          base.device = mapped.device;
          base.modelSpecs = mapped.modelSpecs;
        } catch {
          base.device = null;
          base.modelSpecs = null;
        }
      }

      data.value = base;
    } catch (e: any) {
      error.value =
        e?.normalizedMessage ?? e?.response?.data?.title ?? e?.message ?? "Dashboard yüklenemedi.";
      data.value = null;
    } finally {
      loading.value = false;
    }
  }

  async function refresh() {
    if (ticketId.value > 0) await load(ticketId.value);
  }

  async function searchParts(q: string, limit = 15): Promise<PartSearchItem[]> {
    const query = (q ?? "").trim();
    if (!query) return [];
    const res = await http.get(`/api/parts`, { params: { query, limit } });
    const arr = safeArray<any>(res.data);
    return arr.map((p) => ({
      partId: Number(p?.partId ?? p?.id ?? 0),
      code: String(p?.code ?? p?.partCode ?? ""),
      name: String(p?.name ?? p?.partName ?? ""),
    }));
  }


async function updateTicketStatus(id: number, status: string) {
  await lsts.updateTicketStatus(id, {
    toStatus: status,
    changedBy: actorUserId.value,
  });
}

  
  async function updateTicketNotes(id: number, notes: string) {
    const payload = {
      internalNotes: (notes ?? "").trim() || null,
      publicNote: null,
      updatedBy: actorUserId.value,
    };
    await http.patch(`/api/tickets/${id}/notes`, payload);
  }

  async function createPartRequest(id: number) {
    const payload = { ticketId: id, requestedBy: actorUserId.value };
    await http.post(`/api/part-requests`, payload);
  }

  async function addPartRequestItem(requestId: number, partCode: string, qty: number) {
  await http.post(`/api/part-requests/${requestId}/items`, {
    partCode,
    qty,
    requestedBy: actorUserId.value,
  });
}
 
  async function issuePartRequestItem(requestId: number, itemId: number, issuedQty: number, note?: string) {
    const payload = { issuedQty, issuedBy: actorUserId.value, note: note ?? null };
    await http.patch(`/api/part-requests/${requestId}/items/${itemId}/issue`, payload);
  }

  async function setPartRequestStatus(requestId: number, status: PartRequestStatus) {
    const payload = { toStatus: status, changedBy: actorUserId.value, note: null };
    await http.patch(`/api/part-requests/${requestId}/status`, payload);
  }

  async function cancelPartRequest(requestId: number, reason?: string) {
    const payload = { cancelledBy: actorUserId.value, note: reason?.trim() || null };
    await http.patch(`/api/part-requests/${requestId}/cancel`, payload);
  }

  async function closePartRequest(requestId: number) {
    const payload = { closedBy: actorUserId.value, note: null };
    await http.patch(`/api/part-requests/${requestId}/close`, payload);
  }

  return {
    actorUserId,
    loading,
    error,
    data,
    load,
    refresh,

    searchParts,

    updateTicketStatus,
    updateTicketNotes,

    createPartRequest,
    addPartRequestItem,
    issuePartRequestItem,
    setPartRequestStatus,
    cancelPartRequest,
    closePartRequest,
  };
}
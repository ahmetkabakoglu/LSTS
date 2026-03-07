import type { AxiosError } from "axios";
import { http } from "./http";

export type TicketDto = {
  ticketId: number;
  serviceNo: string;
  deviceId: number;
  currentStatus: string;
  statusUpdatedAt?: string | null;
  faultDesc: string;
  internalNotes?: string | null;
  publicNote?: string | null;
  estimatedDeliveryDate?: string | null;
};

export type PartRequestItemDto = {
  itemId: number;
  requestId: number;
  partId: number;
  partCode: string;
  qtyRequested: number;
  qtyIssued: number;
  note?: string | null;
  requestedBy: number;
  requestedAt?: string | null;
  createdBy: number;
  createdAt?: string | null;
};

export type PartRequestDto = {
  requestId: number;
  requestStatus: string;
  note?: string | null;
  requestedBy: number;
  requestedAt?: string | null;
  createdBy: number;
  createdAt?: string | null;
  items?: PartRequestItemDto[];
};

export type UsageSummaryDto = {
  partId: number;
  partCode: string;
  totalQty: number;
  totalAmount: number;
};

export type TicketDashboardDto = {
  ticket: TicketDto;
  partRequests: PartRequestDto[];
  usageSummary: UsageSummaryDto[];
};

export type PartListItem = {
  partId: number;
  partCode: string;
  onHandQty: number;
  unitPrice?: number;
};

function getErrMsg(err: unknown): string {
  const e = err as AxiosError<any>;
  const data = e?.response?.data;
  if (typeof data === "string") return data;
  if (data?.error) return String(data.error);
  if (data?.title) return String(data.title);
  if (e?.message) return e.message;
  return "Bir hata oluştu";
}

export const lsts = {
  async getTicketDashboard(ticketId: number): Promise<TicketDashboardDto> {
    try {
      const { data } = await http.get(`/api/tickets/${ticketId}/dashboard`);
      return data;
    } catch (err) {
      throw new Error(getErrMsg(err));
    }
  },

  async createTicket(req: {
    deviceId: number;
    faultDesc: string;
    internalNotes?: string | null;
    publicNote?: string | null;
    estimatedDeliveryDate?: string | null;
    createdBy: number;
  }): Promise<{ ticketId: number; serviceNo: string; currentStatus: string }> {
    try {
      const { data } = await http.post(`/api/tickets`, req);
      return data;
    } catch (err) {
      throw new Error(getErrMsg(err));
    }
  },

  async updateTicketStatus(
    ticketId: number,
    req: { toStatus: string; changedBy: number }
  ): Promise<{ ticketId: number; serviceNo: string; currentStatus: string }> {
    try {
      const { data } = await http.patch(`/api/tickets/${ticketId}/status`, req);
      return data;
    } catch (err) {
      throw new Error(getErrMsg(err));
    }
  },

  async searchParts(query: string, limit = 20): Promise<PartListItem[]> {

    const q = query?.trim();
    try {
      const { data } = await http.get(`/api/parts`, {
        params: { query: q || null, limit },
      });
      return data;
    } catch {
      const { data } = await http.get(`/api/parts`, {
        params: { prefix: q || null },
      });
      return data;
    }
  },

  async createPartRequest(req: {
    ticketId: number;
    requestedBy: number;
    note?: string | null;
  }): Promise<{ requestId: number; requestStatus: string }> {
    try {
      const { data } = await http.post(`/api/part-requests`, req);
      return data;
    } catch (err) {
      throw new Error(getErrMsg(err));
    }
  },

  async getPartRequest(requestId: number): Promise<{ header: any; items: any[] }> {
    try {
      const { data } = await http.get(`/api/part-requests/${requestId}`);
      return data;
    } catch (err) {
      throw new Error(getErrMsg(err));
    }
  },

  async addPartRequestItem(
    requestId: number,
    req: { partCode: string; qty: number; requestedBy: number }
  ): Promise<any> {
    try {
      const { data } = await http.post(`/api/part-requests/${requestId}/items`, req);
      return data;
    } catch (err) {
      throw new Error(getErrMsg(err));
    }
  },

  async issuePartRequestItem(
    requestId: number,
    itemId: number,
    req: { issuedQty: number; issuedBy: number }
  ): Promise<any> {
    try {
      const { data } = await http.patch(
        `/api/part-requests/${requestId}/items/${itemId}/issue`,
        req
      );
      return data;
    } catch (err) {
      throw new Error(getErrMsg(err));
    }
  },

  async updatePartRequestStatus(
    requestId: number,
    req: { toStatus: string; changedBy: number; note?: string | null }
  ): Promise<any> {
    try {
      const { data } = await http.patch(`/api/part-requests/${requestId}/status`, req);
      return data;
    } catch (err) {
      throw new Error(getErrMsg(err));
    }
  },

  async listPartRequestsByTicket(ticketId: number): Promise<any[]> {
    try {
      const { data } = await http.get(`/api/tickets/${ticketId}/part-requests`);
      return data;
    } catch (err) {
      throw new Error(getErrMsg(err));
    }
  },
};
export type PartRequestListItemDto = {
  requestId: number;
  ticketId: number;
  requestStatus: string | null;
  note: string | null;
  requestedBy: number;
  requestedAt: string | null;
  createdBy: number;
};

export async function getPartRequestsByTicket(ticketId: number) {
  const { data } = await http.get<PartRequestListItemDto[]>(
    `/api/tickets/${ticketId}/part-requests`
  );
  return data;
}


export type ActivePartRequestListItemDto = PartRequestListItemDto & {
  serviceNo: string | null;
  itemCount: number;
  openItemCount: number;
};

export async function getActivePartRequests(limit = 200) {
  const { data } = await http.get<ActivePartRequestListItemDto[]>(
    `/api/part-requests/active`,
    { params: { limit } }
  );
  return data;
}

export type Role = "Admin" | "Supervisor" | "Intake" | "Technician" | "Warehouse";

export const authApi = {
  login: (username: string, password: string) =>
    http.post<{ token: string; username: string; roles: Role[] }>("/api/auth/login", {
      username,
      password,
    }),

  me: () =>
    http.get<{ username: string | null; roles: Role[] }>("/api/auth/me"),
};
export type PublicTicketStatusDto = {
  serviceNo: string;
  currentStatus: string;
  statusUpdatedAt: string;
  estimatedDeliveryDate?: string | null;
  publicNote?: string | null;
};

export const publicApi = {
  async byService(serviceNo: string, last4: string): Promise<PublicTicketStatusDto> {
    try {
      const { data } = await http.get("/api/public/tickets/by-service", {
        params: { serviceNo, last4 },
      });
      return data;
    } catch (err) {
      throw new Error(getErrMsg(err));
    }
  },

  async bySerial(serialNo: string, last4: string): Promise<PublicTicketStatusDto> {
    try {
      const { data } = await http.get("/api/public/tickets/by-serial", {
        params: { serialNo, last4 },
      });
      return data;
    } catch (err) {
      throw new Error(getErrMsg(err));
    }
  },
};
export type DashboardSummaryDto = {
  kpis: {
    openTickets: number;
    waitingParts: number;
    inRepair: number;
    closedToday: number;
  };
  recentTickets: Array<{
    ticketId: number;
    serviceNo: string;
    status: string;
    fault: string;
    at: string | null;
  }>;
  activity: Array<{
    id: number;
    icon: string;
    text: string;
    at: string | null;
  }>;
};

export async function getDashboardSummary(): Promise<DashboardSummaryDto> {
  const { data } = await http.get<DashboardSummaryDto>("/api/dashboard/summary");
  return data;
}
export type TicketListItemDto = {
  ticketId: number;
  serviceNo: string;
  currentStatus: string;
  faultDesc: string;
  at: string | null;
};

export async function getTicketsList(params?: {
  q?: string | null;
  status?: string | null;
  openOnly?: boolean | null;
  limit?: number | null;
}): Promise<TicketListItemDto[]> {
  const { data } = await http.get<TicketListItemDto[]>("/api/tickets", {
    params: {
      q: params?.q ?? null,
      status: params?.status ?? null,
      openOnly: params?.openOnly ?? true,
      limit: params?.limit ?? 200,
    },
  });
  return data;
}
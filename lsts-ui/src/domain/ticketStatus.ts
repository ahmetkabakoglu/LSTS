export type TicketStatus =
  | "NEW"
  | "RECEIVED"
  | "DIAGNOSING"
  | "WAITING_APPROVAL"
  | "WAITING_PARTS"
  | "IN_REPAIR"
  | "READY_FOR_PICKUP"
  | "DELIVERED"
  | "CLOSED"
  | "CANCELED"
  | string;

export const TICKET_STATUS_META: Record<
  string,
  { label: string; color: string; icon: string }
> = {
  NEW: { label: "Yeni", color: "blue-grey", icon: "mdi-new-box" },
  RECEIVED: { label: "Teslim Alındı", color: "indigo", icon: "mdi-inbox-arrow-down" },
  DIAGNOSING: { label: "Teşhis", color: "deep-purple", icon: "mdi-stethoscope" },
  WAITING_APPROVAL: { label: "Onay Bekliyor", color: "amber", icon: "mdi-account-clock" },
  WAITING_PARTS: { label: "Parça Bekliyor", color: "amber", icon: "mdi-truck-fast" },
  IN_REPAIR: { label: "Onarımda", color: "cyan", icon: "mdi-tools" },
  READY_FOR_PICKUP: { label: "Teslime Hazır", color: "green", icon: "mdi-package-variant" },
  DELIVERED: { label: "Teslim Edildi", color: "green-darken-2", icon: "mdi-check-circle" },
  CLOSED: { label: "Kapandı", color: "grey", icon: "mdi-lock" },
  CANCELED: { label: "İptal", color: "red", icon: "mdi-cancel" },
};

export function getTicketStatusMeta(status: TicketStatus) {
  const s = (status ?? "").toString().toUpperCase();
  return (
    TICKET_STATUS_META[s] ?? {
      label: status?.toString?.() ?? "Bilinmiyor",
      color: "grey",
      icon: "mdi-help-circle",
    }
  );
}

export const TICKET_STATUS_OPTIONS: { value: string; title: string }[] = [
  { value: "NEW", title: "Yeni" },
  { value: "RECEIVED", title: "Teslim Alındı" },
  { value: "DIAGNOSING", title: "Teşhis" },
  { value: "WAITING_APPROVAL", title: "Onay Bekliyor" },
  { value: "WAITING_PARTS", title: "Parça Bekliyor" },
  { value: "IN_REPAIR", title: "Onarımda" },
  { value: "READY_FOR_PICKUP", title: "Teslime Hazır" },
  { value: "DELIVERED", title: "Teslim Edildi" },
  { value: "CLOSED", title: "Kapandı" },
  { value: "CANCELED", title: "İptal" },
];
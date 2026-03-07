<template>
  <div class="page">
    <div class="d-flex align-center justify-space-between mb-4">
      <div>
        <div class="text-h5 font-weight-semibold">Parts</div>
        <div class="text-body-2 text-medium-emphasis">Stok arama ve hızlı görünüm</div>
      </div>

      <div class="d-flex align-center gap-2">
        <v-btn variant="tonal" :loading="loading" @click="refresh">
          <v-icon start icon="mdi-refresh" />
          Yenile
        </v-btn>
      </div>
    </div>

    <v-alert v-if="error" type="error" variant="tonal" class="rounded-xl mb-4">
      {{ error }}
    </v-alert>

    <v-card class="surface-card" variant="flat" rounded="xl">
      <v-card-text class="pb-0">
        <div class="d-flex flex-wrap align-center gap-3">
          <v-text-field
            v-model="q"
            class="search"
            density="comfortable"
            variant="outlined"
            prepend-inner-icon="mdi-magnify"
            label="Parça ara (partCode)"
            hide-details
            @keyup.enter="applySearch"
          />

          <v-btn color="primary" class="btn-primary" :loading="loading" @click="applySearch">
            <v-icon start icon="mdi-magnify" />
            Ara
          </v-btn>

          <div class="d-flex align-center gap-2 ml-auto">
            <div class="text-caption text-medium-emphasis">Sayfa boyutu</div>
            <v-select
              v-model="pageSize"
              class="page-size"
              :items="pageSizeOptions"
              density="comfortable"
              variant="outlined"
              hide-details
              style="width: 140px"
            />
          </div>
        </div>

        <div class="d-flex align-center justify-space-between mt-3 mb-2">
          <div class="text-caption text-medium-emphasis">
            Toplam: <span class="text-body-2 font-weight-semibold">{{ total }}</span>
            <span v-if="filteredNote" class="ml-2">• {{ filteredNote }}</span>
          </div>

          <div class="d-flex align-center gap-2">
            <v-chip size="small" variant="tonal" class="mono meta-chip">
              Sayfa {{ page }}/{{ pageCount }}
            </v-chip>
          </div>
        </div>
      </v-card-text>

      <v-divider />
      <div class="table-wrap">
        <v-skeleton-loader v-if="loading && items.length === 0" type="table" class="pa-4" />

        <v-table v-else class="parts-table">
          <thead>
  <tr>
    <th style="width: 220px">
      <button class="th-btn" type="button" @click="toggleSort('partCode')">
        Part Code
        <v-icon
          size="16"
          class="ml-1"
          :class="sortBy === 'partCode' ? 'sort-active' : 'sort-idle'"
          :icon="sortBy === 'partCode' ? (sortDesc ? 'mdi-arrow-down' : 'mdi-arrow-up') : 'mdi-swap-vertical'"
        />
      </button>
    </th>

    <th>
      <button class="th-btn" type="button" @click="toggleSort('partName')">
        Part Name
        <v-icon
          size="16"
          class="ml-1"
          :class="sortBy === 'partName' ? 'sort-active' : 'sort-idle'"
          :icon="sortBy === 'partName' ? (sortDesc ? 'mdi-arrow-down' : 'mdi-arrow-up') : 'mdi-swap-vertical'"
        />
      </button>
    </th>

    <th class="text-center" style="width: 180px">
      <button class="th-btn center" type="button" @click="toggleSort('stock')">
        Stock
        <v-icon
          size="16"
          class="ml-1"
          :class="sortBy === 'stock' ? 'sort-active' : 'sort-idle'"
          :icon="sortBy === 'stock' ? (sortDesc ? 'mdi-arrow-down' : 'mdi-arrow-up') : 'mdi-swap-vertical'"
        />
      </button>
    </th>

    <th class="text-right" style="width: 220px">
      <button class="th-btn right" type="button" @click="toggleSort('unitPrice')">
        Unit Price
        <v-icon
          size="16"
          class="ml-1"
          :class="sortBy === 'unitPrice' ? 'sort-active' : 'sort-idle'"
          :icon="sortBy === 'unitPrice' ? (sortDesc ? 'mdi-arrow-down' : 'mdi-arrow-up') : 'mdi-swap-vertical'"
        />
      </button>
    </th>
  </tr>
</thead>

          <tbody>
            <tr v-for="p in pageItems" :key="p.partId ?? p.partCode">
  <td>
    <v-chip
      size="small"
      class="mono pill pill-code clickable-code"
      variant="flat"
      @click="copyCode(p.partCode)"
    >
      <v-icon start size="16" icon="mdi-tag-outline" />
      {{ p.partCode }}
    </v-chip>
  </td>
  <td>
    <div class="partNameCol ellipsis">
      {{ p.partName || "—" }}
    </div>
  </td>
  <td class="text-center">
    <v-chip
      size="small"
      class="mono pill pill-stock"
      :class="`stock-${stockTone(p.stock)}`"
      variant="flat"
    >
      <v-icon start size="16" :icon="stockIcon(p.stock)" />
      <span class="stock-num">{{ stockText(p.stock) }}</span>
      <span class="stock-suffix">adet</span>
    </v-chip>
  </td>
  <td class="text-right">
    <v-chip size="small" class="mono pill pill-price" variant="flat">
      <v-icon start size="16" icon="mdi-currency-try" />
      <span class="price-num">{{ formatTRYNumberOnly(p.unitPrice) }}</span>
    </v-chip>
  </td>
</tr>

            <tr v-if="!loading && pageItems.length === 0">
              <td colspan="4" class="text-center py-8 text-medium-emphasis">
                Kayıt bulunamadı.
              </td>
            </tr>
          </tbody>
        </v-table>
      </div>

      <v-divider />
      <v-card-actions class="d-flex align-center justify-space-between">
        <div class="text-caption text-medium-emphasis">
          Gösterilen: <span class="mono">{{ rangeText }}</span>
        </div>

        <v-pagination
          v-model="page"
          :length="pageCount"
          density="comfortable"
          rounded="lg"
          :total-visible="7"
        />
      </v-card-actions>
    </v-card>

    <v-snackbar v-model="snack.open" :color="snack.color" timeout="2200">
      {{ snack.text }}
    </v-snackbar>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { http } from "../api/http";

type PartListItem = {
  partId?: number;
  partCode: string;
  partName?: string | null;
  stock?: number | null;
  unitPrice?: number | null;
};

type SortKey = "partCode" | "partName" | "stock" | "unitPrice";

const q = ref("");
const loading = ref(false);
const error = ref<string | null>(null);

const items = ref<PartListItem[]>([]);

const page = ref(1);


const pageSizeOptions = [20, 25, 50, 100];
const pageSize = ref<number>(20);

const sortBy = ref<SortKey>("partCode");
const sortDesc = ref(false);

const snack = ref<{ open: boolean; text: string; color: string }>({
  open: false,
  text: "",
  color: "success",
});

function toast(text: string, color: string = "success") {
  snack.value = { open: true, text, color };
}


let t: any = null;
watch(
  () => q.value,
  () => {
    page.value = 1;
    if (t) clearTimeout(t);
    t = setTimeout(() => applySearch(), 350);
  }
);

watch(
  () => pageSize.value,
  () => {
    page.value = 1;
  }
);

const total = computed(() => items.value.length);

const filteredNote = computed(() => {
  const s = q.value.trim();
  return s ? `Filtre: "${s}"` : "";
});

const sortedItems = computed(() => {
  const arr = [...items.value];
  const key = sortBy.value;
  const desc = sortDesc.value ? -1 : 1;

  arr.sort((a, b) => {
    const av: any = (a as any)[key];
    const bv: any = (b as any)[key];

    const aNil = av === null || av === undefined || av === "";
    const bNil = bv === null || bv === undefined || bv === "";
    if (aNil && bNil) return 0;
    if (aNil) return 1;
    if (bNil) return -1;

    if (typeof av === "string" || typeof bv === "string") {
      return String(av).localeCompare(String(bv), "tr", { sensitivity: "base" }) * desc;
    }
    return (Number(av) - Number(bv)) * desc;
  });

  return arr;
});

const pageCount = computed(() => Math.max(1, Math.ceil(sortedItems.value.length / pageSize.value)));

const pageItems = computed(() => {
  const p = Math.min(page.value, pageCount.value);
  const start = (p - 1) * pageSize.value;
  const end = start + pageSize.value;
  return sortedItems.value.slice(start, end);
});

const rangeText = computed(() => {
  if (total.value === 0) return "0-0 / 0";
  const p = Math.min(page.value, pageCount.value);
  const start = (p - 1) * pageSize.value + 1;
  const end = Math.min(p * pageSize.value, total.value);
  return `${start}-${end} / ${total.value}`;
});

function toggleSort(key: SortKey) {
  if (sortBy.value === key) {
    sortDesc.value = !sortDesc.value;
  } else {
    sortBy.value = key;
    sortDesc.value = false;
  }
}

function formatTRYNumberOnly(v?: number | null) {
  if (v === null || v === undefined || Number.isNaN(Number(v))) return "—";
  return new Intl.NumberFormat("tr-TR", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(Number(v));
}

function stockText(v?: number | null) {
  if (v === null || v === undefined) return "—";
  return String(v);
}

function stockTone(v?: number | null) {
  if (v === null || v === undefined) return "na";
  if (v <= 0) return "zero";
  if (v <= 5) return "low";
  return "ok";
}

function stockIcon(v?: number | null) {
  if (v === null || v === undefined) return "mdi-help-circle-outline";
  if (v <= 0) return "mdi-close-circle";
  if (v <= 5) return "mdi-alert-circle";
  return "mdi-check-circle";
}

async function copyCode(code: string) {
  try {
    await navigator.clipboard.writeText(code);
    toast("Kopyalandı");
  } catch {
    toast("Kopyalanamadı", "error");
  }
}

function toNumberOrNull(v: any): number | null {
  if (v === null || v === undefined) return null;
  if (typeof v === "number" && Number.isFinite(v)) return v;
  if (typeof v === "string") {
    const cleaned = v.replace(/[^\d.-]/g, "");
    const n = Number(cleaned);
    return Number.isFinite(n) ? n : null;
  }
  return null;
}

function pickStock(x: any): number | null {
  const raw = x.onHandQty ?? x.stock ?? x.on_hand_qty ?? null;
  return toNumberOrNull(raw);
}

async function fetchParts() {
  loading.value = true;
  error.value = null;

  try {
    const params: any = {
      query: q.value.trim() || undefined,
      limit: 5000,
    };

    const { data } = await http.get("/api/parts", { params });

    const arr = Array.isArray(data) ? data : Array.isArray(data?.items) ? data.items : [];

    items.value = (arr as any[]).map((x) => ({
      partId: x.partId ?? x.part_id ?? x.id,
      partCode: x.partCode ?? x.part_code ?? x.code,
      partName: x.partName ?? x.part_name ?? x.name ?? null,
      stock: pickStock(x),
      unitPrice: toNumberOrNull(x.unitPrice ?? x.unit_price ?? x.price ?? null),
    }));
  } catch (e: any) {
    error.value = e?.normalizedMessage ?? e?.message ?? "Parçalar yüklenemedi.";
  } finally {
    loading.value = false;
  }
}

async function refresh() {
  await fetchParts();
  if (!error.value) toast("Güncellendi");
}

async function applySearch() {
  page.value = 1;
  await fetchParts();
}

onMounted(async () => {
  await fetchParts();
});
</script>

<style scoped>
.page {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.gap-2 {
  gap: 8px;
}
.gap-3 {
  gap: 12px;
}

/* ✅ Daha siyaha yakın surface (maviye kaymasın) */
.surface-card {
  border: 1px solid rgba(255, 255, 255, 0.07);
  background: rgba(0, 0, 0, 0.40);
  box-shadow: 0 30px 70px rgba(0, 0, 0, 0.60);
}

/* ✅ Yazıları beyaz (tablo + chip) */
.parts-table {
  color: rgba(255, 255, 255, 0.92);
}
.parts-table :deep(td),
.parts-table :deep(th) {
  color: rgba(255, 255, 255, 0.92);
}

:deep(.v-chip),
:deep(.v-chip__content),
:deep(.v-chip .v-icon) {
  color: rgba(255, 255, 255, 0.92) !important;
}

:deep(.search .v-field),
:deep(.page-size .v-field) {
  border-radius: 16px;
}

.btn-primary {
  border-radius: 16px;
}

.meta-chip {
  border: 1px solid rgba(255, 255, 255, 0.10);
}

/* Tablo scroll */
.table-wrap {
  max-height: 560px;
  overflow: auto;
}

/* Header sticky */
.parts-table thead th {
  position: sticky;
  top: 0;
  z-index: 1;
  background: rgba(0, 0, 0, 0.72);
  backdrop-filter: blur(10px);
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}

/* Sort header */
.th-btn {
  width: 100%;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 10px 6px;
  color: rgba(255, 255, 255, 0.92);
  font-weight: 700;
  letter-spacing: 0.2px;
  text-align: left;
  background: transparent;
  border: 0;
  cursor: pointer;
}
.th-btn.center {
  justify-content: center;
}
.th-btn.right {
  justify-content: flex-end;
}

.sort-active {
  opacity: 0.95;
}
.sort-idle {
  opacity: 0.35;
}

/* ✅ Satırların rengi (daha nötr, maviye kaymasın) */
.parts-table tbody td {
  background: rgba(0, 0, 0, 0.18);
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  padding-top: 14px;
  padding-bottom: 14px;
}
.parts-table tbody tr:nth-child(even) td {
  background: rgba(255, 255, 255, 0.02);
}
.parts-table tbody tr:hover td {
  background: rgba(255, 255, 255, 0.04);
}

/* Chips / pills */
.mono {
  font-variant-numeric: tabular-nums;
  font-feature-settings: "tnum" 1;
}

.pill {
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.04);
  box-shadow: 0 10px 24px rgba(0, 0, 0, 0.28);
  color: rgba(255, 255, 255, 0.92);
}

.pill-code {
  background: rgba(124, 58, 237, 0.18);
  border-color: rgba(124, 58, 237, 0.34);
}

.pill-price {
  background: rgba(255, 255, 255, 0.05);
  border-color: rgba(255, 255, 255, 0.14);
}

.pill-stock {
  min-width: 116px;
  justify-content: center;
}

.price-num {
  font-variant-numeric: tabular-nums;
}

/* Stock tones */
.stock-na {
  background: rgba(148, 163, 184, 0.10);
  border-color: rgba(148, 163, 184, 0.26);
}
.stock-zero {
  background: rgba(239, 68, 68, 0.14);
  border-color: rgba(239, 68, 68, 0.32);
}
.stock-low {
  background: rgba(245, 158, 11, 0.14);
  border-color: rgba(245, 158, 11, 0.32);
}
.stock-ok {
  background: rgba(34, 197, 94, 0.14);
  border-color: rgba(34, 197, 94, 0.32);
}

.stock-num {
  font-weight: 800;
  letter-spacing: 0.2px;
}
.stock-suffix {
  opacity: 0.75;
  margin-left: 6px;
  font-size: 12px;
}

/* Copy icon */
.copy-btn {
  opacity: 0.82;
}
.copy-btn:hover {
  opacity: 1;
}

/* Part code cell */
.codeCell {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.partName {
  opacity: 0.78;
  font-size: 12px;
}

/* Ellipsis */
.ellipsis {
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
  max-width: 460px;
}
.clickable-code {
  cursor: pointer;
  user-select: none;
  transition: filter 0.15s ease, transform 0.15s ease;
}
.clickable-code:hover {
  filter: brightness(1.12);
  transform: translateY(-1px);
}

.partNameCol {
  color: rgba(255, 255, 255, 0.82);
  font-size: 13px;
}
</style>
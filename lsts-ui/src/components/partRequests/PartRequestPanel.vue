<template>
  <v-card class="rounded-xl">
    <v-card-title class="text-subtitle-1 font-weight-semibold d-flex align-center justify-space-between">
      <div class="d-flex align-center">
        <v-icon icon="mdi-clipboard-list" class="mr-2" />
        Part Requests
      </div>

      <v-btn color="primary" variant="flat" @click="emitCreate" :disabled="busy">
        <v-icon start icon="mdi-plus" />
        Yeni PR
      </v-btn>
    </v-card-title>

    <v-divider />

    <v-card-text>
      <v-row dense>
        <v-col cols="12" md="4">
          <v-card variant="tonal" class="rounded-xl">
            <v-card-text>
              <div class="text-caption text-medium-emphasis mb-2">Liste</div>

              <v-list density="compact" class="rounded-lg">
                <v-list-item
                  v-for="r in requests"
                  :key="r.requestId"
                  :active="r.requestId === selectedId"
                  @click="selectedId = r.requestId"
                  class="rounded-lg"
                >
                  <template #prepend>
                    <v-icon icon="mdi-clipboard-text" />
                  </template>

                  <v-list-item-title class="font-weight-medium">
                    PR #{{ r.requestId }}
                  </v-list-item-title>

                  <v-list-item-subtitle class="text-medium-emphasis">
                    {{ r.status }}
                    <span v-if="r.items?.length"> · {{ r.items.length }} item</span>
                  </v-list-item-subtitle>

                  <template #append>
                    <v-chip size="x-small" variant="tonal">{{ r.status }}</v-chip>
                  </template>
                </v-list-item>

                <div v-if="!requests.length" class="text-caption text-medium-emphasis pa-3">
                  Henüz Part Request yok.
                </div>
              </v-list>
            </v-card-text>
          </v-card>
        </v-col>

        <v-col cols="12" md="8">
          <v-card variant="tonal" class="rounded-xl">
            <v-card-text>
              <div class="d-flex align-center justify-space-between mb-3">
                <div>
                  <div class="text-caption text-medium-emphasis">Seçili</div>
                  <div class="text-subtitle-1 font-weight-semibold">
                    <span v-if="selected">PR #{{ selected.requestId }}</span>
                    <span v-else>—</span>
                  </div>
                </div>

                <div class="d-flex gap-2 flex-wrap">
                  <v-btn
                    variant="flat"
                    color="secondary"
                    :disabled="!selected || busy"
                    @click="openAddItem"
                  >
                    <v-icon start icon="mdi-plus-box" />
                    Item Ekle
                  </v-btn>

                  <v-menu v-if="selected">
                    <template #activator="{ props: p }">
                      <v-btn v-bind="p" variant="flat" :disabled="busy">
                        <v-icon start icon="mdi-swap-horizontal" />
                        Status
                      </v-btn>
                    </template>
                    <v-list density="compact">
                      <v-list-item
                        v-for="s in prStatusOptions"
                        :key="s"
                        :title="s"
                        @click="emitStatus(selected!.requestId, s)"
                      />
                    </v-list>
                  </v-menu>

                  <v-btn
                    variant="text"
                    color="error"
                    :disabled="!selected || busy"
                    @click="openCancel"
                  >
                    İptal
                  </v-btn>

                  <v-btn
                    variant="text"
                    color="success"
                    :disabled="!selected || busy"
                    @click="openClose"
                  >
                    Kapat
                  </v-btn>
                </div>
              </div>

              <v-divider class="mb-3" />

              <div v-if="!selected" class="text-caption text-medium-emphasis">
                Sol listeden bir PR seç.
              </div>

              <div v-else>
                <v-table density="compact" class="rounded-lg">
                  <thead>
                    <tr>
                      <th>Part</th>
                      <th class="text-right">Qty</th>
                      <th class="text-right">Issued</th>
                      <th>Status</th>
                      <th class="text-right">İşlem</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="it in selected.items" :key="it.itemId">
                      <td>
                        <div class="font-weight-medium">
                          {{ it.partCode || "—" }}
                        </div>
                        <div class="text-caption text-medium-emphasis">
                          {{ it.partName || "—" }}
                        </div>
                      </td>
                      <td class="text-right">{{ it.qty }}</td>
                      <td class="text-right">{{ it.issuedQty ?? 0 }}</td>
                      <td>
                        <v-chip size="x-small" variant="tonal">
                          {{ it.status || "—" }}
                        </v-chip>
                      </td>
                      <td class="text-right">
                        <v-btn
                          size="small"
                          variant="flat"
                          color="primary"
                          :disabled="busy"
                          @click="openIssue(it.itemId)"
                        >
                          Issue
                        </v-btn>
                      </td>
                    </tr>

                    <tr v-if="!selected.items?.length">
                      <td colspan="5" class="text-caption text-medium-emphasis">
                        Bu PR’da item yok.
                      </td>
                    </tr>
                  </tbody>
                </v-table>
              </div>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>
    </v-card-text>
    
    <v-dialog v-model="dlgAddItem" max-width="720">
      <v-card class="rounded-xl">
        <v-card-title class="d-flex align-center justify-space-between">
          <div class="text-subtitle-1 font-weight-semibold">Item Ekle</div>
          <v-btn icon variant="text" @click="dlgAddItem = false">
            <v-icon icon="mdi-close" />
          </v-btn>
        </v-card-title>

        <v-card-text>
          <v-autocomplete
            v-model="selectedPart"
            :items="partOptions"
            :loading="partsLoading"
            label="Parça ara"
            variant="outlined"
            hide-no-data
            clearable
            item-title="title"
            return-object
            @update:search="onSearchParts"
          />

          <v-text-field
            v-model.number="qty"
            type="number"
            min="1"
            label="Qty"
            variant="outlined"
            class="mt-3"
          />
        </v-card-text>

        <v-card-actions class="px-4 pb-4">
          <v-spacer />
          <v-btn variant="text" @click="dlgAddItem = false">Vazgeç</v-btn>
          <v-btn color="primary" variant="flat" :loading="busy" @click="confirmAddItem">
            Ekle
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="dlgIssue" max-width="620">
      <v-card class="rounded-xl">
        <v-card-title class="d-flex align-center justify-space-between">
          <div class="text-subtitle-1 font-weight-semibold">Issue Item</div>
          <v-btn icon variant="text" @click="dlgIssue = false">
            <v-icon icon="mdi-close" />
          </v-btn>
        </v-card-title>

        <v-card-text>
          <div class="text-caption text-medium-emphasis mb-2">
            Item ID: {{ issueItemId ?? "—" }}
          </div>

          <v-text-field
            v-model.number="issueQty"
            type="number"
            min="1"
            label="Issued Qty"
            variant="outlined"
          />

          <v-textarea
            v-model="issueNote"
            label="Note (opsiyonel)"
            variant="outlined"
            rows="2"
            auto-grow
            class="mt-3"
          />
        </v-card-text>

        <v-card-actions class="px-4 pb-4">
          <v-spacer />
          <v-btn variant="text" @click="dlgIssue = false">Vazgeç</v-btn>
          <v-btn color="primary" variant="flat" :loading="busy" @click="confirmIssue">
            Issue
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <ConfirmDialog
      v-model="dlgCancel"
      title="Part Request iptal edilsin mi?"
      message="Bu işlem geri alınamaz. İstersen açıklama ekleyebilirsin."
      confirm-text="İptal Et"
      confirm-color="error"
      :loading="busy"
      :show-reason="true"
      @confirm="confirmCancel"
    />

    <ConfirmDialog
      v-model="dlgClose"
      title="Part Request kapatılsın mı?"
      message="PR kapanınca üzerinde değişiklik yapmamak daha doğru olur."
      confirm-text="Kapat"
      confirm-color="success"
      :loading="busy"
      @confirm="confirmClose"
    />
  </v-card>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import ConfirmDialog from "../../components/common/ConfirmDialog.vue";

type PR = {
  requestId: number;
  status: string;
  items: Array<{
    itemId: number;
    partId: number;
    partCode?: string | null;
    partName?: string | null;
    qty: number;
    issuedQty?: number | null;
    status?: string | null;
  }>;
};

type PartOpt = { title: string; partId: number; partCode: string };

const props = defineProps<{
  requests: PR[];
  busy?: boolean;
  searchParts: (q: string) => Promise<Array<{ partId: number; code: string; name: string }>>;
}>();

const emit = defineEmits<{
  (e: "create"): void;
  (e: "addItem", payload: { requestId: number; partCode: string; qty: number }): void;
  (e: "issueItem", payload: { requestId: number; itemId: number; issuedQty: number; note?: string }): void;
  (e: "setStatus", payload: { requestId: number; status: string }): void;
  (e: "cancel", payload: { requestId: number; reason?: string }): void;
  (e: "close", payload: { requestId: number }): void;
}>();

const busy = computed(() => props.busy ?? false);

const selectedId = ref<number | null>(null);
const selected = computed(() => props.requests.find((x) => x.requestId === selectedId.value) ?? null);

const dlgAddItem = ref(false);
const dlgIssue = ref(false);
const dlgCancel = ref(false);
const dlgClose = ref(false);

const issueItemId = ref<number | null>(null);
const issueNote = ref("");
const issueQty = ref(1);

const qty = ref(1);
const selectedPart = ref<PartOpt | null>(null);

const partsLoading = ref(false);
const partOptions = ref<PartOpt[]>([]);

const prStatusOptions = ["REQUESTED", "PARTIALLY_ISSUED", "ISSUED", "CANCELLED", "CLOSED"];

function emitCreate() {
  emit("create");
}

function openAddItem() {
  if (!selected.value) return;
  qty.value = 1;
  selectedPart.value = null;
  partOptions.value = [];
  dlgAddItem.value = true;
}

async function onSearchParts(q: string) {
  const query = (q ?? "").trim();
  if (!query) {
    partOptions.value = [];
    return;
  }
  partsLoading.value = true;
  try {
    const res = await props.searchParts(query);
    partOptions.value = res.map((p) => ({
      title: `${p.code} — ${p.name}`,
      partId: p.partId,
      partCode: p.code,
    }));
  } finally {
    partsLoading.value = false;
  }
}

function confirmAddItem() {
  if (!selected.value) return;
  if (!selectedPart.value) return;

  const q = Math.max(1, Number(qty.value ?? 1));

  emit("addItem", {
    requestId: selected.value.requestId,
    partCode: selectedPart.value.partCode,
    qty: q,
  });

  dlgAddItem.value = false;
}

function openIssue(itemId: number) {
  if (!selected.value) return;
  issueItemId.value = itemId;
  issueNote.value = "";
  issueQty.value = 1;
  dlgIssue.value = true;
}

function confirmIssue() {
  if (!selected.value || !issueItemId.value) return;

  const q = Math.max(1, Number(issueQty.value ?? 1));

  emit("issueItem", {
    requestId: selected.value.requestId,
    itemId: issueItemId.value,
    issuedQty: q,
    note: issueNote.value.trim() || undefined,
  });

  dlgIssue.value = false;
}

function emitStatus(requestId: number, status: string) {
  emit("setStatus", { requestId, status });
}

function openCancel() {
  if (!selected.value) return;
  dlgCancel.value = true;
}

function confirmCancel(payload: { reason?: string }) {
  if (!selected.value) return;
  emit("cancel", { requestId: selected.value.requestId, reason: payload.reason });
  dlgCancel.value = false;
}

function openClose() {
  if (!selected.value) return;
  dlgClose.value = true;
}

function confirmClose() {
  if (!selected.value) return;
  emit("close", { requestId: selected.value.requestId });
  dlgClose.value = false;
}
</script>

<style scoped>
.gap-2 {
  gap: 8px;
}
</style>
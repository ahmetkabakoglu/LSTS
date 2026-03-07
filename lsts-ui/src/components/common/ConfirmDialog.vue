<template>
  <v-dialog v-model="model" max-width="520">
    <v-card class="rounded-xl">
      <v-card-title class="d-flex align-center justify-space-between">
        <div class="text-subtitle-1 font-weight-semibold">
          {{ title }}
        </div>
        <v-btn icon variant="text" @click="model = false">
          <v-icon icon="mdi-close" />
        </v-btn>
      </v-card-title>

      <v-card-text class="text-body-2 text-medium-emphasis">
        <slot>
          {{ message }}
        </slot>

        <div v-if="showReason" class="mt-4">
          <v-textarea
            v-model="reasonLocal"
            label="Açıklama (opsiyonel)"
            variant="outlined"
            rows="2"
            auto-grow
          />
        </div>
      </v-card-text>

      <v-card-actions class="px-4 pb-4">
        <v-spacer />
        <v-btn variant="text" @click="model = false">Vazgeç</v-btn>
        <v-btn
          :color="confirmColor"
          variant="flat"
          :loading="loading"
          @click="onConfirmClick"
        >
          {{ confirmText }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";

const props = defineProps<{
  modelValue: boolean;
  title: string;
  message?: string;
  confirmText?: string;
  confirmColor?: string;
  loading?: boolean;
  showReason?: boolean;
}>();

const emit = defineEmits<{
  (e: "update:modelValue", v: boolean): void;
  (e: "confirm", payload: { reason?: string }): void;
}>();

const model = computed({
  get: () => props.modelValue,
  set: (v: boolean) => emit("update:modelValue", v),
});

const reasonLocal = ref("");

watch(
  () => props.modelValue,
  (v) => {
    if (v) reasonLocal.value = "";
  }
);

const confirmText = computed(() => props.confirmText ?? "Onayla");
const confirmColor = computed(() => props.confirmColor ?? "primary");
const showReason = computed(() => props.showReason ?? false);
const loading = computed(() => props.loading ?? false);

function onConfirmClick() {
  emit("confirm", { reason: reasonLocal.value.trim() || undefined });
}
</script>
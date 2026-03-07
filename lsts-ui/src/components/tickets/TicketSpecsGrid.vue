<template>
  <v-card class="rounded-xl">
    <v-card-title class="text-subtitle-1 font-weight-semibold d-flex align-center">
      <v-icon icon="mdi-memory" class="mr-2" />
      Model Specs
    </v-card-title>

    <v-divider />

    <v-card-text>
      <v-row dense>
        <v-col v-for="it in items" :key="it.key" cols="12" md="6" lg="3">
          <v-card variant="tonal" class="rounded-xl">
            <v-card-text class="d-flex align-center">
              <v-icon :icon="it.icon" class="mr-2" />
              <div>
                <div class="text-caption text-medium-emphasis">{{ it.label }}</div>
                <div class="text-body-2 font-weight-medium">
                  {{ it.value && it.value.length ? it.value : "—" }}
                </div>
              </div>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>

      <div v-if="isEmpty" class="text-caption text-medium-emphasis mt-2">
        Model spec verisi yok.
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { computed } from "vue";

const props = defineProps<{
  specs?: {
    ram?: string | null;
    cpu?: string | null;
    gpu?: string | null;
    display?: string | null;
    storage?: string | null;
  } | null;
}>();

const items = computed(() => [
  { key: "ram", label: "RAM", icon: "mdi-memory", value: props.specs?.ram ?? "" },
  { key: "cpu", label: "CPU", icon: "mdi-cpu-64-bit", value: props.specs?.cpu ?? "" },
  { key: "gpu", label: "GPU", icon: "mdi-expansion-card", value: props.specs?.gpu ?? "" },
  { key: "display", label: "Display", icon: "mdi-monitor", value: props.specs?.display ?? "" },
  { key: "storage", label: "Storage", icon: "mdi-harddisk", value: props.specs?.storage ?? "" },
]);

const isEmpty = computed(() => items.value.every((x) => !x.value));
</script>
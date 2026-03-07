import { ref, watch } from "vue";

const KEY = "lsts.actorUserId";

function normalizeActor(v: unknown) {
  const n = Number(v);
  if (Number.isFinite(n) && n > 0) return n;
  return 1;
}

export function useActor() {
  const actorUserId = ref<number>(1);

  try {
    actorUserId.value = normalizeActor(localStorage.getItem(KEY));
  } catch {
    actorUserId.value = 1;
  }

  watch(actorUserId, (v) => {
    try {
      localStorage.setItem(KEY, String(normalizeActor(v)));
    } catch {
      
    }
  });

  return { actorUserId };
}
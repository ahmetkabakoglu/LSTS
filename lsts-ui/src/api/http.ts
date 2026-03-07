import axios from "axios";

export const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5240",
  timeout: 15000,
});

http.interceptors.request.use((config) => {
  const token =
    localStorage.getItem("lsts.auth.token") ??
    sessionStorage.getItem("lsts.auth.token");

  if (token) {
    if (config.headers && typeof (config.headers as any).set === "function") {
      (config.headers as any).set("Authorization", `Bearer ${token}`);
    } else {
      config.headers = {
        ...(config.headers as any),
        Authorization: `Bearer ${token}`,
      } as any;
    }
  }
  return config;
});
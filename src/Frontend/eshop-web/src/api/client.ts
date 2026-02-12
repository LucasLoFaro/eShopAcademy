import axios from "axios";
import { msalInstance, loginRequest } from "../auth/msalConfig";

const api = axios.create({
  baseURL: import.meta.env.VITE_GATEWAY_URL ?? "http://localhost:5200",
});

api.interceptors.request.use(async (config) => {
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length > 0) {
    try {
      const response = await msalInstance.acquireTokenSilent({
        ...loginRequest,
        account: accounts[0],
      });
      config.headers.Authorization = `Bearer ${response.accessToken}`;
    } catch {
      // Token acquisition failed silently — request proceeds without auth
    }
  }
  return config;
});

export default api;

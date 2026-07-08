import axios from "axios";

let accessToken: string | null = null;

function isValidJwt(token: string): boolean {
  return token.split(".").length === 3;
}

export function setAccessToken(token: string): boolean {
  if (isValidJwt(token)) {
    accessToken = token;
    return true;
  }
  console.warn("[Sellers] Received invalid token (not a JWT), ignoring.");
  return false;
}

export function getAccessToken(): string | null {
  return accessToken;
}

const api = axios.create({
  baseURL: import.meta.env.VITE_GATEWAY_URL || "http://localhost:5200",
});

api.interceptors.request.use(async (config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

export default api;

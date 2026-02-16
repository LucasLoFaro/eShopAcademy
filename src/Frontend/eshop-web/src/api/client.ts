import axios from "axios";
import { msalInstance, loginRequest } from "../auth/msalConfig";

const api = axios.create({
  baseURL: import.meta.env.VITE_GATEWAY_URL ?? "http://localhost:5200",
});

let isRedirecting = false;

api.interceptors.request.use(async (config) => {
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length > 0) {
    try {
      const response = await msalInstance.acquireTokenSilent({
        ...loginRequest,
        account: accounts[0],
      });
      config.headers.Authorization = `Bearer ${response.accessToken}`;
      console.log("[Auth] Token acquired for request:", config.url);
    } catch (error: any) {
      console.error("[Auth] Token acquisition failed:", error);
      
      // If interaction is required (token expired, consent needed, etc.), trigger login
      if (error.errorCode === "interaction_required" || 
          error.errorCode === "consent_required" ||
          error.errorCode === "login_required") {
        
        if (!isRedirecting) {
          isRedirecting = true;
          console.warn("[Auth] Token expired or interaction required - redirecting to login...");
          
          // Redirect immediately
          await msalInstance.loginRedirect(loginRequest);
        }
        
        // Cancel the request
        return Promise.reject(new Error("Authentication required - redirecting to login"));
      }
    }
  } else {
    console.warn("[Auth] No accounts found — request sent without auth");
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      console.error("[Auth] 401 Unauthorized:", error.config?.url);
      
      const accounts = msalInstance.getAllAccounts();
      
      // If we got 401, the token is invalid - redirect to login
      if (!isRedirecting) {
        isRedirecting = true;
        
        if (accounts.length > 0) {
          console.warn("[Auth] Token rejected by server - redirecting to login...");
        } else {
          console.warn("[Auth] No account found - redirecting to login...");
        }
        
        // Use a small timeout to let the error propagate to the UI
        setTimeout(() => {
          msalInstance.loginRedirect(loginRequest);
        }, 500);
      }
    }
    
    return Promise.reject(error);
  }
);

export default api;

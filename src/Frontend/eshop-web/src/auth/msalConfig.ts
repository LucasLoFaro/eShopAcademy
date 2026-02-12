import {
  PublicClientApplication,
  type Configuration,
  type AuthenticationResult,
  LogLevel,
  EventType,
} from "@azure/msal-browser";

const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID ?? "",
    authority: `https://login.microsoftonline.com/${import.meta.env.VITE_ENTRA_TENANT_ID ?? "common"}`,
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: "localStorage",
  },
  system: {
    loggerOptions: {
      logLevel: LogLevel.Info,
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return;
        if (level === LogLevel.Error) console.error("[MSAL]", message);
        else if (level === LogLevel.Warning) console.warn("[MSAL]", message);
      },
    },
  },
};

export const msalInstance = new PublicClientApplication(msalConfig);

export const loginRequest = {
  scopes: [`api://${import.meta.env.VITE_ENTRA_API_CLIENT_ID ?? ""}/access_as_user`],
};

export const graphScopes = {
  scopes: ["User.Read"],
};

export async function initializeMsal(): Promise<void> {
  await msalInstance.initialize();

  // Set active account on every login success (register before handleRedirectPromise)
  msalInstance.addEventCallback((event) => {
    if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
      const result = event.payload as AuthenticationResult;
      msalInstance.setActiveAccount(result.account);
    }
  });

  // Process auth code from redirect flow — must catch errors so the app still renders
  try {
    const response = await msalInstance.handleRedirectPromise();
    if (response) {
      msalInstance.setActiveAccount(response.account);
    }
  } catch (error) {
    console.error("[MSAL] handleRedirectPromise failed:", error);
  }

  // Restore active account from cache on page reload
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length > 0 && !msalInstance.getActiveAccount()) {
    msalInstance.setActiveAccount(accounts[0]);
  }
}

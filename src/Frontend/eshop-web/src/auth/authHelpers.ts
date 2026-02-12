import { type IPublicClientApplication } from "@azure/msal-browser";
import { loginRequest } from "./msalConfig";

export async function handleLogin(instance: IPublicClientApplication): Promise<void> {
  await instance.loginRedirect(loginRequest);
}

export async function handleLogout(instance: IPublicClientApplication): Promise<void> {
  await instance.logoutRedirect();
}

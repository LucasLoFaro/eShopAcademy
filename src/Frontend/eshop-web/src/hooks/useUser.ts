import { useState, useEffect } from "react";
import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { graphScopes } from "../auth/msalConfig";

interface UserProfile {
  name: string;
  email: string;
  photoUrl: string | null;
}

export function useUser(): UserProfile | null {
  const { instance } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const [photoUrl, setPhotoUrl] = useState<string | null>(null);

  const account = instance.getActiveAccount();

  useEffect(() => {
    if (!isAuthenticated || !account) {
      setPhotoUrl(null);
      return;
    }

    let revoked = false;

    (async () => {
      try {
        const tokenResponse = await instance.acquireTokenSilent({
          ...graphScopes,
          account,
        });

        const response = await fetch("https://graph.microsoft.com/v1.0/me/photo/$value", {
          headers: { Authorization: `Bearer ${tokenResponse.accessToken}` },
        });

        if (response.ok) {
          const blob = await response.blob();
          if (!revoked) {
            setPhotoUrl(URL.createObjectURL(blob));
          }
        }
      } catch {
        // Photo not available — that's fine
      }
    })();

    return () => {
      revoked = true;
      setPhotoUrl((prev) => {
        if (prev) URL.revokeObjectURL(prev);
        return null;
      });
    };
  }, [isAuthenticated, account?.homeAccountId]);

  if (!isAuthenticated || !account) return null;

  return {
    name: account.name ?? account.username ?? "User",
    email: account.username ?? "",
    photoUrl,
  };
}

import { useMsal } from "@azure/msal-react";
import { useSyncExternalStore, useCallback, useMemo } from "react";

const PREFIX = "wishlist:";

function getKey(userId: string) {
  return PREFIX + userId;
}

function getIds(userId: string): string[] {
  try {
    return JSON.parse(localStorage.getItem(getKey(userId)) || "[]");
  } catch {
    return [];
  }
}

function setIds(userId: string, ids: string[]) {
  localStorage.setItem(getKey(userId), JSON.stringify(ids));
  window.dispatchEvent(new Event("wishlist-change"));
}

let listeners: (() => void)[] = [];
function subscribe(cb: () => void) {
  listeners.push(cb);
  const handler = () => cb();
  window.addEventListener("wishlist-change", handler);
  return () => {
    listeners = listeners.filter((l) => l !== cb);
    window.removeEventListener("wishlist-change", handler);
  };
}

export function useWishlist() {
  const { instance } = useMsal();
  const userId = instance.getActiveAccount()?.localAccountId ?? "";

  const ids = useSyncExternalStore(
    subscribe,
    () => localStorage.getItem(getKey(userId)) || "[]"
  );

  const wishlistIds: string[] = useMemo(() => {
    try { return JSON.parse(ids); } catch { return []; }
  }, [ids]);

  const toggle = useCallback(
    (productId: string) => {
      if (!userId) return;
      const current = getIds(userId);
      if (current.includes(productId)) {
        setIds(userId, current.filter((id) => id !== productId));
      } else {
        setIds(userId, [...current, productId]);
      }
    },
    [userId]
  );

  const isFavorite = useCallback(
    (productId: string) => wishlistIds.includes(productId),
    [wishlistIds]
  );

  const count = wishlistIds.length;

  return { wishlistIds, toggle, isFavorite, count };
}

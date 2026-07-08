import { useQuery } from "@tanstack/react-query";
import api from "../api/client";
import type { Seller } from "../types";

export function useSeller() {
  return useQuery<Seller | null>({
    queryKey: ["seller-me"],
    queryFn: async () => {
      try {
        const { data } = await api.get("/api/sellers/me");
        return data;
      } catch (e: unknown) {
        const err = e as { response?: { status?: number } };
        if (err?.response?.status === 404) return null;
        throw e;
      }
    },
  });
}

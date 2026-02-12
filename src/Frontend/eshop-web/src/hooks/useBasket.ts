import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import api from "../api/client";
import type { BasketWithDetails, BasketItem } from "../types";
import { useClientId } from "./useClientId";

export function useBasket() {
  const clientId = useClientId();
  return useQuery<BasketWithDetails>({
    queryKey: ["basket", clientId],
    queryFn: async () => {
      const { data } = await api.get("/api/basket/clientId", {
        params: { clientID: clientId },
      });
      return data;
    },
    enabled: !!clientId,
  });
}

export function useAddToBasket() {
  const clientId = useClientId();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (item: BasketItem) => {
      await api.post(`/api/basket/clientId/add?clientID=${clientId}`, item);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["basket", clientId] });
    },
  });
}

export function useRemoveFromBasket() {
  const clientId = useClientId();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (item: BasketItem) => {
      await api.post(`/api/basket/clientId/remove?clientID=${clientId}`, item);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["basket", clientId] });
    },
  });
}

import { useQuery, useMutation } from "@tanstack/react-query";
import api from "../api/client";
import type { Order, OrderRequest, PlaceOrderResponse } from "../types";
import { useCustomer } from "./useCustomer";

export function useOrders() {
  const { data: customer } = useCustomer();
  return useQuery<Order[]>({
    queryKey: ["orders", customer?.id],
    queryFn: async () => {
      const { data } = await api.get("/api/orders");
      // Filter client-side by customer ID
      if (customer?.id && Array.isArray(data)) {
        return data.filter((o: Order) => o.customerId === customer.id);
      }
      return data;
    },
    enabled: !!customer?.id,
  });
}

export function useOrder(id: string) {
  return useQuery<Order>({
    queryKey: ["orders", id],
    queryFn: async () => {
      const { data } = await api.get(`/api/orders/${id}`);
      return data;
    },
    enabled: !!id,
  });
}

export function usePlaceOrder() {
  return useMutation<PlaceOrderResponse, Error, OrderRequest>({
    mutationFn: async (request) => {
      const { data } = await api.post("/api/orders", request);
      return data;
    },
  });
}

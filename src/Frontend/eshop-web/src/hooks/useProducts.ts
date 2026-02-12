import { useQuery } from "@tanstack/react-query";
import api from "../api/client";
import type { Product } from "../types";

export interface ProductFilters {
  sort?: string;
  cat?: string;
  deals?: boolean;
}

export function useProducts(filters?: ProductFilters) {
  return useQuery<Product[]>({
    queryKey: ["products", filters],
    queryFn: async () => {
      const params: Record<string, string> = {};
      if (filters?.sort) params.sort = filters.sort;
      if (filters?.cat) params.cat = filters.cat;
      if (filters?.deals) params.deals = "true";
      const { data } = await api.get("/api/products", { params });
      return Array.isArray(data) ? data : data.products ?? [];
    },
  });
}

export function useProduct(id: string) {
  return useQuery<Product>({
    queryKey: ["products", id],
    queryFn: async () => {
      const { data } = await api.get(`/api/products/${id}`);
      return data;
    },
    enabled: !!id,
  });
}

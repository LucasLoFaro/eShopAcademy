import { useQuery } from "@tanstack/react-query";
import api from "../api/client";
import type { Product, ProductSearchFilter, PagedResult } from "../types";

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

export function useProductSearch(filter: ProductSearchFilter) {
  return useQuery<PagedResult<Product>>({
    queryKey: ["productSearch", filter],
    queryFn: async () => {
      const params: Record<string, string> = {};
      if (filter.searchText) params.searchText = filter.searchText;
      if (filter.category) params.category = filter.category;
      if (filter.minPrice != null) params.minPrice = String(filter.minPrice);
      if (filter.maxPrice != null) params.maxPrice = String(filter.maxPrice);
      if (filter.deals) params.deals = "true";
      if (filter.inStock) params.inStock = "true";
      if (filter.minRating != null) params.minRating = String(filter.minRating);
      if (filter.sort) params.sort = filter.sort;
      if (filter.page != null) params.page = String(filter.page);
      if (filter.pageSize != null) params.pageSize = String(filter.pageSize);
      const { data } = await api.get("/api/products/search", { params });
      return data;
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

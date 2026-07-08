import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import api from "../api/client";

export interface PublishProductPayload {
  name: string;
  price: number;
  description: string;
  imageUrl: string;
  categoryId: string;
  additionalImages: string[];
  aboutHtml: string;
  specs: { label: string; value: string }[];
  faqs: { question: string; answer: string }[];
}

export interface Category {
  id: string;
  name: string;
}

export interface SellerProduct {
  id: string;
  name: string;
  price: number;
  description: string;
  imageUrl: string;
  category: { id: string; name: string } | null;
  categoryId: string;
  sellerId: string;
  additionalImages: string[];
  aboutHtml: string;
  specs: { label: string; value: string }[];
  faqs: { question: string; answer: string }[];
  createdAt: string;
}

export function useSellerProducts(sellerId: string | undefined, publishedProductIds: string[] | undefined) {
  return useQuery<SellerProduct[]>({
    queryKey: ["seller-products", sellerId, publishedProductIds],
    queryFn: async () => {
      const { data } = await api.get(`/api/products?sellerId=${sellerId}`);
      if (!publishedProductIds || publishedProductIds.length === 0) return [];
      const idSet = new Set(publishedProductIds);
      return (data as SellerProduct[]).filter((p) => idSet.has(p.id));
    },
    enabled: !!sellerId && !!publishedProductIds,
  });
}

export function useUploadProductImage() {
  return useMutation<string, Error, File>({
    mutationFn: async (file: File) => {
      const formData = new FormData();
      formData.append("file", file);
      const { data } = await api.post("/api/sellers/products/upload-image", formData, {
        headers: { "Content-Type": "multipart/form-data" },
      });
      return data.url;
    },
  });
}

export function usePublishProduct() {
  return useMutation<unknown, Error, PublishProductPayload>({
    mutationFn: async (payload) => {
      const { data } = await api.post("/api/sellers/products", payload);
      return data;
    },
  });
}

export function useDeleteProduct() {
  const queryClient = useQueryClient();
  return useMutation<unknown, Error, string>({
    mutationFn: async (productId: string) => {
      await api.delete(`/api/products/${productId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["seller-products"] });
    },
  });
}

export interface UpdateProductPayload {
  id: string;
  name: string;
  price: number;
  description: string;
  imageUrl: string;
  categoryId: string;
  additionalImages: string[];
  aboutHtml: string;
  specs: { label: string; value: string }[];
  faqs: { question: string; answer: string }[];
  sellerId: string;
}

export function useUpdateProduct() {
  const queryClient = useQueryClient();
  return useMutation<unknown, Error, UpdateProductPayload>({
    mutationFn: async (payload) => {
      const { data } = await api.put("/api/products", payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["seller-products"] });
    },
  });
}

export function useCategories() {
  return useQuery<Category[]>({
    queryKey: ["categories"],
    queryFn: async () => {
      const { data } = await api.get("/api/products");
      // Extract unique categories from products
      const seen = new Map<string, string>();
      for (const product of data) {
        if (product.category?.id && !seen.has(product.category.id)) {
          seen.set(product.category.id, product.category.name);
        }
      }
      return Array.from(seen.entries()).map(([id, name]) => ({ id, name }));
    },
  });
}

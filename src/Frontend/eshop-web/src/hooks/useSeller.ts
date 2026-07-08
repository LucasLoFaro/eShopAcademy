import { useQuery, useMutation } from "@tanstack/react-query";
import { useIsAuthenticated } from "@azure/msal-react";
import api from "../api/client";
import type { Seller, DocumentAnalysisResult, RegisterSellerRequest } from "../types";

export function useSeller() {
  const isAuthenticated = useIsAuthenticated();

  return useQuery<Seller | null>({
    queryKey: ["seller-me"],
    queryFn: async () => {
      try {
        const { data } = await api.get("/api/sellers/me");
        return data;
      } catch (e: any) {
        if (e?.response?.status === 404) return null;
        throw e;
      }
    },
    enabled: isAuthenticated,
  });
}

export function useAnalyzeDocument() {
  return useMutation<DocumentAnalysisResult, Error, File>({
    mutationFn: async (file: File) => {
      const formData = new FormData();
      formData.append("document", file);
      const { data } = await api.post("/api/sellers/analyze-document", formData, {
        headers: { "Content-Type": "multipart/form-data" },
      });
      return data;
    },
  });
}

export function useRegisterSeller() {
  return useMutation<Seller, Error, RegisterSellerRequest>({
    mutationFn: async (request: RegisterSellerRequest) => {
      const { data } = await api.post("/api/sellers/register", request);
      return data;
    },
  });
}

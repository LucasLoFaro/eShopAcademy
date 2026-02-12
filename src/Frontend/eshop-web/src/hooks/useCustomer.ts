import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import api from "../api/client";

interface CustomerData {
  id: string;
  name: string;
  mail: string;
  phone: string;
  status: string;
}

export function useCustomer() {
  const { instance } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const account = instance.getActiveAccount();

  return useQuery<CustomerData | null>({
    queryKey: ["customer", account?.homeAccountId],
    queryFn: async () => {
      if (!account) return null;
      try {
        const { data } = await api.get(`/api/me/customer/${account.localAccountId}`);
        return data;
      } catch (e: any) {
        if (e?.response?.status === 404) return null;
        throw e;
      }
    },
    enabled: isAuthenticated && !!account,
  });
}

export function useEnsureCustomer() {
  const { instance } = useMsal();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async () => {
      const account = instance.getActiveAccount();
      if (!account) throw new Error("Not authenticated");

      // Try to get existing customer
      try {
        const { data } = await api.get(`/api/me/customer/${account.localAccountId}`);
        return data as CustomerData;
      } catch (e: any) {
        if (e?.response?.status !== 404) throw e;
      }

      // Create new customer
      const { data } = await api.post("/api/me/customer", {
        id: account.localAccountId,
        name: account.name ?? account.username ?? "User",
        mail: account.username ?? "",
        phone: "",
        address: { street: "", city: "", state: "", country: "", zipCode: "" },
        status: 0,
      });
      return data as CustomerData;
    },
    onSuccess: () => {
      const account = instance.getActiveAccount();
      queryClient.invalidateQueries({ queryKey: ["customer", account?.homeAccountId] });
    },
  });
}

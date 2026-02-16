import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import api from "../api/client";
import type { Customer } from "../types";

export function useCustomer() {
  const { instance } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const account = instance.getActiveAccount();

  return useQuery<Customer | null>({
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
        return data as Customer;
      } catch (e: any) {
        if (e?.response?.status !== 404) throw e;
      }

      // Create new customer
      const { data } = await api.post("/api/me/customer", {
        id: account.localAccountId,
        name: account.name ?? account.username ?? "User",
        mail: account.username ?? "",
        phone: "",
        address: { street: "", city: "", state: "", country: "", zipCode: "", number: "", additionalInformation: "" },
        savedAddresses: [],
        status: 0,
      });
      return data as Customer;
    },
    onSuccess: () => {
      const account = instance.getActiveAccount();
      queryClient.invalidateQueries({ queryKey: ["customer", account?.homeAccountId] });
    },
  });
}

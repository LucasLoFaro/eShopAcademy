import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useMsal } from "@azure/msal-react";
import api from "../api/client";
import type { SavedAddress, Address } from "../types";

export function useAddCustomerAddress() {
  const { instance } = useMsal();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: { description: string; address: Address; isDefault?: boolean }) => {
      const account = instance.getActiveAccount();
      if (!account) throw new Error("Not authenticated");

      const { data: created } = await api.post<SavedAddress>(
        `/api/me/customer/${account.localAccountId}/addresses`,
        {
          description: data.description,
          address: data.address,
          isDefault: data.isDefault ?? false,
        }
      );
      return created;
    },
    onSuccess: () => {
      const account = instance.getActiveAccount();
      queryClient.invalidateQueries({ queryKey: ["customer", account?.homeAccountId] });
    },
  });
}

export function useUpdateCustomerAddress() {
  const { instance } = useMsal();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: { addressId: string; description: string; address: Address; isDefault?: boolean }) => {
      const account = instance.getActiveAccount();
      if (!account) throw new Error("Not authenticated");

      const { data: updated } = await api.put<SavedAddress>(
        `/api/me/customer/${account.localAccountId}/addresses/${data.addressId}`,
        {
          description: data.description,
          address: data.address,
          isDefault: data.isDefault ?? false,
        }
      );
      return updated;
    },
    onSuccess: () => {
      const account = instance.getActiveAccount();
      queryClient.invalidateQueries({ queryKey: ["customer", account?.homeAccountId] });
    },
  });
}

export function useDeleteCustomerAddress() {
  const { instance } = useMsal();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (addressId: string) => {
      const account = instance.getActiveAccount();
      if (!account) throw new Error("Not authenticated");

      await api.delete(`/api/me/customer/${account.localAccountId}/addresses/${addressId}`);
    },
    onSuccess: () => {
      const account = instance.getActiveAccount();
      queryClient.invalidateQueries({ queryKey: ["customer", account?.homeAccountId] });
    },
  });
}

export function useSetDefaultAddress() {
  const { instance } = useMsal();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (addressId: string) => {
      const account = instance.getActiveAccount();
      if (!account) throw new Error("Not authenticated");

      await api.post(`/api/me/customer/${account.localAccountId}/addresses/${addressId}/set-default`);
    },
    onSuccess: () => {
      const account = instance.getActiveAccount();
      queryClient.invalidateQueries({ queryKey: ["customer", account?.homeAccountId] });
    },
  });
}

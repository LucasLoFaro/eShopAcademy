import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import api from "../api/client";

export interface PackageItem {
  productId: string;
  productName: string;
  quantity: number;
}

export interface PendingPackage {
  orderId: string;
  reservationId: string | null;
  customerName: string;
  customerEmail: string;
  status: string;
  preparedAt: string | null;
  readyAt: string | null;
  issueType: string;
  issueDetails: string;
  updatedAt: string;
  items: PackageItem[];
}

export function useSellerPendingPackages(sellerId: string | undefined) {
  return useQuery<PendingPackage[]>({
    queryKey: ["seller-pending-packages", sellerId],
    queryFn: async () => {
      const { data } = await api.get(`/api/operations/seller/pending-packages`, {
        headers: { "X-Seller-Id": sellerId! },
      });
      return data;
    },
    enabled: !!sellerId,
  });
}

export function useSellerPackages(sellerId: string | undefined) {
  return useQuery<PendingPackage[]>({
    queryKey: ["seller-packages", sellerId],
    queryFn: async () => {
      const { data } = await api.get(`/api/operations/seller/packages?limit=20`, {
        headers: { "X-Seller-Id": sellerId! },
      });
      return data;
    },
    enabled: !!sellerId,
  });
}

export function useMarkReadyForPickup(sellerId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation<void, Error, string>({
    mutationFn: async (orderId: string) => {
      await api.post(
        `/api/operations/seller/orders/${orderId}/ready-for-pickup`,
        {},
        { headers: { "X-Seller-Id": sellerId! } }
      );
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["seller-pending-packages", sellerId] });
      queryClient.invalidateQueries({ queryKey: ["seller-packages", sellerId] });
      queryClient.invalidateQueries({ queryKey: ["seller-me"] });
    },
  });
}

export function useStartProcessing(sellerId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation<void, Error, string>({
    mutationFn: async (orderId: string) => {
      await api.post(
        `/api/operations/seller/orders/${orderId}/start-processing`,
        {},
        { headers: { "X-Seller-Id": sellerId! } }
      );
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["seller-pending-packages", sellerId] });
      queryClient.invalidateQueries({ queryKey: ["seller-packages", sellerId] });
      queryClient.invalidateQueries({ queryKey: ["seller-me"] });
    },
  });
}

export function useReportIssue(sellerId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation<void, Error, { orderId: string; issueType: string; details: string }>({
    mutationFn: async ({ orderId, issueType, details }) => {
      await api.post(
        `/api/operations/seller/orders/${orderId}/report-issue`,
        { issueType, details },
        { headers: { "X-Seller-Id": sellerId! } }
      );
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["seller-pending-packages", sellerId] });
      queryClient.invalidateQueries({ queryKey: ["seller-packages", sellerId] });
      queryClient.invalidateQueries({ queryKey: ["seller-me"] });
    },
  });
}

export interface ShippingStatusEntry {
  shipmentId: string;
  orderId: string;
  status: string;
  trackingNumber: string;
  carrier: string;
  occurredAt: string;
}

export function useShippingHistory(orderId: string | undefined) {
  return useQuery<ShippingStatusEntry[]>({
    queryKey: ["shipping-history", orderId],
    queryFn: async () => {
      const { data } = await api.get(`/api/shipping/${orderId}/history`);
      return data;
    },
    enabled: !!orderId,
  });
}

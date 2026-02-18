import { useEffect, useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { msalInstance, loginRequest } from "../auth/msalConfig";
import { useClientId } from "./useClientId";

interface OrderStatusEvent {
  orderId: string;
  status: string;
  occurredAt: string;
  trackingNumber?: string;
  carrier?: string;
}

export function useOrderStatusStream(orderId: string | undefined) {
const queryClient = useQueryClient();
const clientId = useClientId();

  const invalidateOrder = useCallback(
    () => {
      if (orderId) {
        queryClient.invalidateQueries({ queryKey: ["orders", orderId] });
      }
    },
    [orderId, queryClient]
  );

  useEffect(() => {
    if (!orderId) return;

    let eventSource: EventSource | null = null;
    let retryTimeout: ReturnType<typeof setTimeout>;

    const connect = async () => {
      // Get auth token for the SSE request
      const accounts = msalInstance.getAllAccounts();
      let token = "";
      if (accounts.length > 0) {
        try {
          const response = await msalInstance.acquireTokenSilent({
            ...loginRequest,
            account: accounts[0],
          });
          token = response.accessToken;
        } catch {
          console.error("[SSE] Failed to acquire token");
          return;
        }
      }

      const baseUrl = import.meta.env.VITE_GATEWAY_URL ?? "http://localhost:5200";

      // EventSource doesn't support custom headers, so pass the token as a query param
      // The gateway/API should accept it via query string
      const url = `${baseUrl}/api/orders/${orderId}/stream?access_token=${encodeURIComponent(token)}`;

      eventSource = new EventSource(url);

      eventSource.addEventListener("connected", () => {
        console.log("[SSE] Connected to order status stream:", orderId);
      });

      eventSource.onmessage = (event) => {
        try {
          const data: OrderStatusEvent = JSON.parse(event.data);
          console.log("[SSE] Order status update:", data.status);

          invalidateOrder();

          if (data.status === "Paid") {
            queryClient.invalidateQueries({ queryKey: ["basket", clientId] });
          }
        } catch (err) {
          console.error("[SSE] Failed to parse event:", err);
        }
      };

      eventSource.onerror = () => {
        console.warn("[SSE] Connection lost, reconnecting in 5s...");
        eventSource?.close();
        retryTimeout = setTimeout(connect, 5000);
      };
    };

    connect();

    return () => {
      eventSource?.close();
      clearTimeout(retryTimeout);
    };
  }, [orderId, invalidateOrder, clientId, queryClient]);
}

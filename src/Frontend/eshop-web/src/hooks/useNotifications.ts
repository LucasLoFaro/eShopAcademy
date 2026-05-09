import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import api from "../api/client";
import type { NotificationMessage } from "../types";
import { useUser } from "./useUser";

export function useNotifications() {
  const user = useUser();
  const email = user?.email;

  return useQuery<NotificationMessage[]>({
    queryKey: ["notifications", email],
    queryFn: async () => {
      const { data } = await api.get("/api/notifications", {
        params: { email },
      });
      return data;
    },
    enabled: !!email,
  });
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient();
  const user = useUser();

  return useMutation({
    mutationFn: async (id: string) => {
      await api.patch(`/api/notifications/${id}/read`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notifications", user?.email] });
    },
  });
}

export function useMarkAllNotificationsRead() {
  const queryClient = useQueryClient();
  const user = useUser();

  return useMutation({
    mutationFn: async () => {
      await api.post("/api/notifications/read-all", null, {
        params: { email: user?.email },
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notifications", user?.email] });
    },
  });
}

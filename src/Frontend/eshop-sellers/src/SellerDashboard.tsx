import { useEffect, useState } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { setAccessToken } from "./api/client";
import App from "./App";
import "./index.css";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, retry: 1 },
  },
});

interface SellerDashboardProps {
  token: string | null;
}

export default function SellerDashboard({ token }: SellerDashboardProps) {
  const [ready, setReady] = useState(false);

  useEffect(() => {
    if (token) {
      const accepted = setAccessToken(token);
      if (accepted) {
        queryClient.invalidateQueries();
        setReady(true);
      }
    }
  }, [token]);

  if (!ready) {
    return (
      <div className="p-8 text-center">
        <div className="animate-spin h-8 w-8 border-4 border-amber-400 border-t-transparent rounded-full mx-auto" />
        <p className="mt-4 text-gray-500">Loading seller module...</p>
      </div>
    );
  }

  return (
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  );
}

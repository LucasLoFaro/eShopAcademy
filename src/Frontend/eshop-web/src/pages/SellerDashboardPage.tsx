import { lazy, Suspense, useCallback, useEffect, useState } from "react";
import { useSeller } from "../hooks/useSeller";
import { useMsal } from "@azure/msal-react";
import { loginRequest } from "../auth/msalConfig";
import { Link } from "react-router";

const RemoteSellerDashboard = lazy(() => import("eshopSellers/SellerDashboard"));

export default function SellerDashboardPage() {
  const { data: seller, isLoading, error } = useSeller();
  const { instance } = useMsal();
  const [token, setToken] = useState<string | null>(null);

  const acquireToken = useCallback(async () => {
    const account = instance.getActiveAccount();
    if (!account) return;
    try {
      const response = await instance.acquireTokenSilent({
        ...loginRequest,
        account,
      });
      const t = response.accessToken;
      if (t && t.split(".").length === 3) {
        setToken(t);
      }
    } catch {
      // Token acquisition failed silently
    }
  }, [instance]);

  useEffect(() => {
    acquireToken();
  }, [acquireToken]);

  if (isLoading) {
    return (
      <div className="max-w-4xl mx-auto py-12 px-4 text-center">
        <div className="animate-spin h-8 w-8 border-4 border-amber-400 border-t-transparent rounded-full mx-auto" />
        <p className="mt-4 text-gray-500">Loading seller dashboard...</p>
      </div>
    );
  }

  if (error || !seller) {
    return (
      <div className="max-w-4xl mx-auto py-12 px-4 text-center">
        <h2 className="text-xl font-semibold text-gray-700">Not a seller yet</h2>
        <p className="mt-2 text-gray-500">You need to register as a seller to access the dashboard.</p>
        <Link to="/sell/register" className="mt-4 inline-block rounded-lg bg-indigo-600 px-6 py-3 font-semibold text-white hover:bg-indigo-700 transition">
          Register as Seller
        </Link>
      </div>
    );
  }

  if (seller.status !== "Active") {
    return (
      <div className="max-w-4xl mx-auto py-12 px-4">
        <div className="bg-amber-50 border border-amber-200 rounded-xl p-8 text-center">
          <div className="text-5xl mb-4">⏳</div>
          <h2 className="text-2xl font-bold text-amber-800 mb-2">Verification Pending</h2>
          <p className="text-amber-700">
            Your seller account is being verified. You'll receive a notification once approved.
          </p>
        </div>
      </div>
    );
  }

  return (
    <Suspense
      fallback={
        <div className="p-8 text-center">
          <div className="animate-spin h-8 w-8 border-4 border-amber-400 border-t-transparent rounded-full mx-auto" />
          <p className="mt-4 text-gray-500">Loading seller module...</p>
        </div>
      }
    >
      <RemoteSellerDashboard token={token} />
    </Suspense>
  );
}

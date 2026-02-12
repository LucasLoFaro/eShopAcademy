import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { handleLogin } from "../auth/authHelpers";
import type { ReactNode } from "react";

export default function RequireAuth({ children }: { children: ReactNode }) {
  const isAuthenticated = useIsAuthenticated();
  const { instance } = useMsal();

  if (isAuthenticated) {
    return <>{children}</>;
  }

  return (
    <div className="py-16 text-center">
      <h2 className="text-xl font-semibold text-gray-700">Sign in required</h2>
      <p className="mt-2 text-gray-500">
        You need to sign in to access this page.
      </p>
      <button
        onClick={() => handleLogin(instance)}
        className="mt-4 rounded-lg bg-indigo-600 px-6 py-3 font-semibold text-white transition hover:bg-indigo-700"
      >
        Sign in with Microsoft
      </button>
    </div>
  );
}

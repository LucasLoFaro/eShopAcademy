import { importShared } from './__federation_fn_import-BcBLlA-1.js';
import { Q as QueryClient, s as setAccessToken, j as jsxRuntimeExports, a as QueryClientProvider, A as App } from './App-J9Oz6k16.js';

const {useEffect,useState} = await importShared('react');
const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 3e4, retry: 1 }
  }
});
function SellerDashboard({ token }) {
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
    return /* @__PURE__ */ jsxRuntimeExports.jsxs("div", { className: "p-8 text-center", children: [
      /* @__PURE__ */ jsxRuntimeExports.jsx("div", { className: "animate-spin h-8 w-8 border-4 border-amber-400 border-t-transparent rounded-full mx-auto" }),
      /* @__PURE__ */ jsxRuntimeExports.jsx("p", { className: "mt-4 text-gray-500", children: "Loading seller module..." })
    ] });
  }
  return /* @__PURE__ */ jsxRuntimeExports.jsx(QueryClientProvider, { client: queryClient, children: /* @__PURE__ */ jsxRuntimeExports.jsx(App, {}) });
}

export { SellerDashboard as default };

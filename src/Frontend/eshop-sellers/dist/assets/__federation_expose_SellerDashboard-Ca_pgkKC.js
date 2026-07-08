import { importShared } from './__federation_fn_import-Yp2-s75R.js';
import { Q as QueryClient, s as setAccessToken, j as jsxDevRuntimeExports, a as QueryClientProvider, A as App } from './App-BnRdwO9S.js';

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
    return /* @__PURE__ */ jsxDevRuntimeExports.jsxDEV("div", { className: "p-8 text-center", children: [
      /* @__PURE__ */ jsxDevRuntimeExports.jsxDEV("div", { className: "animate-spin h-8 w-8 border-4 border-amber-400 border-t-transparent rounded-full mx-auto" }, void 0, false, {
        fileName: "C:/github/lucas/eShopAcademy/src/Frontend/eshop-sellers/src/SellerDashboard.tsx",
        lineNumber: 32,
        columnNumber: 9
      }, this),
      /* @__PURE__ */ jsxDevRuntimeExports.jsxDEV("p", { className: "mt-4 text-gray-500", children: "Loading seller module..." }, void 0, false, {
        fileName: "C:/github/lucas/eShopAcademy/src/Frontend/eshop-sellers/src/SellerDashboard.tsx",
        lineNumber: 33,
        columnNumber: 9
      }, this)
    ] }, void 0, true, {
      fileName: "C:/github/lucas/eShopAcademy/src/Frontend/eshop-sellers/src/SellerDashboard.tsx",
      lineNumber: 31,
      columnNumber: 7
    }, this);
  }
  return /* @__PURE__ */ jsxDevRuntimeExports.jsxDEV(QueryClientProvider, { client: queryClient, children: /* @__PURE__ */ jsxDevRuntimeExports.jsxDEV(App, {}, void 0, false, {
    fileName: "C:/github/lucas/eShopAcademy/src/Frontend/eshop-sellers/src/SellerDashboard.tsx",
    lineNumber: 40,
    columnNumber: 7
  }, this) }, void 0, false, {
    fileName: "C:/github/lucas/eShopAcademy/src/Frontend/eshop-sellers/src/SellerDashboard.tsx",
    lineNumber: 39,
    columnNumber: 5
  }, this);
}

export { SellerDashboard as default };

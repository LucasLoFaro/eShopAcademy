import { BrowserRouter, Routes, Route } from "react-router";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import Layout from "./components/Layout";
import RequireAuth from "./components/RequireAuth";
import ProductListPage from "./pages/ProductListPage";
import ProductDetailPage from "./pages/ProductDetailPage";
import BasketPage from "./pages/BasketPage";
import WishlistPage from "./pages/WishlistPage";
import CheckoutPage from "./pages/CheckoutPage";
import OrderListPage from "./pages/OrderListPage";
import OrderDetailPage from "./pages/OrderDetailPage";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, retry: 1 },
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route element={<Layout />}>
            <Route index element={<ProductListPage />} />
            <Route path="products/:id" element={<ProductDetailPage />} />
            <Route path="basket" element={<RequireAuth><BasketPage /></RequireAuth>} />
            <Route path="wishlist" element={<RequireAuth><WishlistPage /></RequireAuth>} />
            <Route path="checkout" element={<RequireAuth><CheckoutPage /></RequireAuth>} />
            <Route path="orders" element={<RequireAuth><OrderListPage /></RequireAuth>} />
            <Route path="orders/:id" element={<RequireAuth><OrderDetailPage /></RequireAuth>} />
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}

export default App

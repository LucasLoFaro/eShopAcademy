import { useState } from "react";
import { useSeller } from "./hooks/useSeller";
import { useSellerProducts, useDeleteProduct, type SellerProduct } from "./hooks/useProducts";
import PublishProductPage from "./PublishProductPage";

export default function App() {
  const { data: seller, isLoading, refetch } = useSeller();
  const { data: products = [], isLoading: loadingProducts } = useSellerProducts(seller?.id, seller?.publishedProductIds);
  const deleteProduct = useDeleteProduct();
  const [page, setPage] = useState<"dashboard" | "publish" | "edit">("dashboard");
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [editingProduct, setEditingProduct] = useState<SellerProduct | null>(null);
  const pageSize = 5;

  if (page === "publish") {
    return <PublishProductPage onBack={() => { setPage("dashboard"); refetch(); }} />;
  }

  if (page === "edit" && editingProduct) {
    return (
      <PublishProductPage
        onBack={() => { setPage("dashboard"); setEditingProduct(null); refetch(); }}
        editProduct={editingProduct}
      />
    );
  }

  if (isLoading) {
    return (
      <div className="p-8 text-center">
        <div className="animate-spin h-8 w-8 border-4 border-amber-400 border-t-transparent rounded-full mx-auto" />
        <p className="mt-4 text-gray-500">Loading seller dashboard...</p>
      </div>
    );
  }

  if (!seller) {
    return (
      <div className="p-8 text-center">
        <p className="text-gray-500">Seller account not found.</p>
      </div>
    );
  }

  if (seller.status === "PendingApproval") {
    return (
      <div className="p-8">
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

  if (seller.status === "Rejected") {
    return (
      <div className="p-8">
        <div className="bg-red-50 border border-red-200 rounded-xl p-8 text-center">
          <div className="text-5xl mb-4">❌</div>
          <h2 className="text-2xl font-bold text-red-800 mb-2">Registration Rejected</h2>
          <p className="text-red-700">
            Your seller registration was not approved. Please contact support for more information.
          </p>
        </div>
      </div>
    );
  }

  const netEarnings = seller.accumulatedSalesAmount - seller.accumulatedCommissionsAmount;

  return (
    <div className="py-8 px-4">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Seller Dashboard</h1>
          <p className="mt-1 text-gray-500">{seller.name} &middot; {seller.email}</p>
        </div>
        <span className="inline-flex items-center gap-1.5 bg-emerald-100 text-emerald-700 px-3 py-1 rounded-full text-sm font-medium">
          <span className="h-2 w-2 rounded-full bg-emerald-500" />
          Active
        </span>
      </div>

      {/* Financial Summary */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
        <div className="bg-white border border-gray-200 rounded-xl p-6">
          <p className="text-sm text-gray-500 mb-1">Total Sales</p>
          <p className="text-2xl font-bold text-gray-900">${seller.accumulatedSalesAmount.toFixed(2)}</p>
        </div>
        <div className="bg-white border border-gray-200 rounded-xl p-6">
          <p className="text-sm text-gray-500 mb-1">Commissions</p>
          <p className="text-2xl font-bold text-gray-900">${seller.accumulatedCommissionsAmount.toFixed(2)}</p>
        </div>
        <div className="bg-white border border-gray-200 rounded-xl p-6">
          <p className="text-sm text-gray-500 mb-1">Net Earnings</p>
          <p className={`text-2xl font-bold ${netEarnings >= 0 ? "text-emerald-600" : "text-red-600"}`}>
            ${netEarnings.toFixed(2)}
          </p>
        </div>
      </div>

      {/* Seller Info */}
      <div className="bg-white border border-gray-200 rounded-xl p-6 mb-8">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Business Information</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
          <div>
            <p className="text-gray-500">Tax ID</p>
            <p className="font-medium text-gray-900">{seller.taxId}</p>
          </div>
          <div>
            <p className="text-gray-500">Email</p>
            <p className="font-medium text-gray-900">{seller.email}</p>
          </div>
          <div>
            <p className="text-gray-500">Published Products</p>
            <p className="font-medium text-gray-900">{seller.publishedProductIds.length}</p>
          </div>
          <div>
            <p className="text-gray-500">Ledger Entries</p>
            <p className="font-medium text-gray-900">{seller.ledgerEntries}</p>
          </div>
        </div>
      </div>

      {/* Products */}
      <div className="bg-white border border-gray-200 rounded-xl p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">Products</h2>
          <button
            onClick={() => setPage("publish")}
            className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 transition"
          >
            + Publish Product
          </button>
        </div>

        {seller.publishedProductIds.length === 0 ? (
          <div className="text-center py-8">
            <div className="text-4xl mb-2">📦</div>
            <p className="text-gray-500">No products published yet.</p>
            <p className="text-sm text-gray-400 mt-1">Start listing your products to begin selling on eShop Academy.</p>
          </div>
        ) : (
          <ProductsTable
            products={products}
            loading={loadingProducts}
            searchTerm={searchTerm}
            onSearchChange={(v) => { setSearchTerm(v); setCurrentPage(1); }}
            currentPage={currentPage}
            pageSize={pageSize}
            onPageChange={setCurrentPage}
            onEdit={(product) => { setEditingProduct(product); setPage("edit"); }}
            onRemove={(product) => {
              if (confirm(`Remove "${product.name}"? This action cannot be undone.`)) {
                deleteProduct.mutate(product.id);
              }
            }}
          />
        )}
      </div>
    </div>
  );
}

function ProductsTable({
  products,
  loading,
  searchTerm,
  onSearchChange,
  currentPage,
  pageSize,
  onPageChange,
  onEdit,
  onRemove,
}: {
  products: SellerProduct[];
  loading: boolean;
  searchTerm: string;
  onSearchChange: (value: string) => void;
  currentPage: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onEdit: (product: SellerProduct) => void;
  onRemove: (product: SellerProduct) => void;
}) {
  const filtered = products.filter((p) =>
    p.name.toLowerCase().includes(searchTerm.toLowerCase())
  );
  const totalPages = Math.max(1, Math.ceil(filtered.length / pageSize));
  const paginated = filtered.slice((currentPage - 1) * pageSize, currentPage * pageSize);

  return (
    <div className="space-y-4">
      <input
        type="text"
        placeholder="Search products..."
        value={searchTerm}
        onChange={(e) => onSearchChange(e.target.value)}
        className="w-full md:w-64 rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
      />

      {loading ? (
        <div className="text-center py-6 text-gray-500 text-sm">Loading products...</div>
      ) : filtered.length === 0 ? (
        <div className="text-center py-6 text-gray-500 text-sm">
          {searchTerm ? "No products match your search." : "No products found."}
        </div>
      ) : (
        <>
          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left">
              <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
                <tr>
                  <th className="px-4 py-3">Product</th>
                  <th className="px-4 py-3">Category</th>
                  <th className="px-4 py-3">Price</th>
                  <th className="px-4 py-3">Created</th>
                  <th className="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {paginated.map((product) => (
                  <tr key={product.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <img
                          src={product.imageUrl}
                          alt={product.name}
                          className="h-10 w-10 rounded object-cover bg-gray-100"
                        />
                        <span className="font-medium text-gray-900 truncate max-w-[200px]">
                          {product.name}
                        </span>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-gray-600">
                      {product.category?.name ?? "—"}
                    </td>
                    <td className="px-4 py-3 text-gray-900 font-medium">
                      ${product.price.toFixed(2)}
                    </td>
                    <td className="px-4 py-3 text-gray-500">
                      {new Date(product.createdAt).toLocaleDateString()}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => onEdit(product)}
                          className="rounded px-2.5 py-1.5 text-xs font-medium text-indigo-700 bg-indigo-50 hover:bg-indigo-100 transition"
                        >
                          Edit
                        </button>
                        <button
                          onClick={() => onRemove(product)}
                          className="rounded px-2.5 py-1.5 text-xs font-medium text-red-700 bg-red-50 hover:bg-red-100 transition"
                        >
                          Remove
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          <div className="flex items-center justify-between pt-2">
            <p className="text-xs text-gray-500">
              Showing {(currentPage - 1) * pageSize + 1}–{Math.min(currentPage * pageSize, filtered.length)} of {filtered.length}
            </p>
            <div className="flex items-center gap-1">
              <button
                onClick={() => onPageChange(currentPage - 1)}
                disabled={currentPage === 1}
                className="rounded px-2.5 py-1.5 text-xs font-medium text-gray-700 bg-gray-100 hover:bg-gray-200 disabled:opacity-40 disabled:cursor-not-allowed transition"
              >
                Previous
              </button>
              {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
                <button
                  key={p}
                  onClick={() => onPageChange(p)}
                  className={`rounded px-2.5 py-1.5 text-xs font-medium transition ${
                    p === currentPage
                      ? "bg-indigo-600 text-white"
                      : "text-gray-700 bg-gray-100 hover:bg-gray-200"
                  }`}
                >
                  {p}
                </button>
              ))}
              <button
                onClick={() => onPageChange(currentPage + 1)}
                disabled={currentPage === totalPages}
                className="rounded px-2.5 py-1.5 text-xs font-medium text-gray-700 bg-gray-100 hover:bg-gray-200 disabled:opacity-40 disabled:cursor-not-allowed transition"
              >
                Next
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

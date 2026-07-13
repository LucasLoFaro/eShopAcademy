import { useState, useRef, useEffect } from "react";
import { useSeller } from "./hooks/useSeller";

type SortDir = "asc" | "desc";
type ProductSortKey = "name" | "category" | "price" | "createdAt";
type SalesSortKey = "customer" | "product" | "qty" | "status" | "updated";

function SortableHeader({
  label,
  active,
  dir,
  onClick,
  align = "left",
}: {
  label: string;
  active: boolean;
  dir: SortDir;
  onClick: () => void;
  align?: "left" | "right";
}) {
  return (
    <th className={`px-4 py-3 ${align === "right" ? "text-right" : ""}`}>
      <button
        onClick={onClick}
        className={`inline-flex items-center gap-1 uppercase text-xs font-semibold transition hover:text-gray-900 ${active ? "text-gray-900" : "text-gray-600"}`}
      >
        {label}
        <span className="text-[10px] leading-none">
          {active ? (dir === "asc" ? "▲" : "▼") : "↕"}
        </span>
      </button>
    </th>
  );
}
import { useSellerProducts, useDeleteProduct, useProductStock, useUpdateProductStock, type SellerProduct } from "./hooks/useProducts";
import { useSellerPackages, useMarkReadyForPickup, useStartProcessing, useReportIssue, useShippingHistory, type PendingPackage } from "./hooks/useSales";
import PublishProductPage from "./PublishProductPage";

export default function App() {
  const { data: seller, isLoading, refetch } = useSeller();
  const { data: products = [], isLoading: loadingProducts } = useSellerProducts(seller?.id, seller?.publishedProductIds);
  const deleteProduct = useDeleteProduct();
  const { data: sellerPackages = [], isLoading: loadingPackages } = useSellerPackages(seller?.id);
  const markReady = useMarkReadyForPickup(seller?.id);
  const startProcessing = useStartProcessing(seller?.id);
  const reportIssue = useReportIssue(seller?.id);
  const [page, setPage] = useState<"dashboard" | "publish" | "edit" | "manage-stock">("dashboard");
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [productSortKey, setProductSortKey] = useState<ProductSortKey>("createdAt");
  const [productSortDir, setProductSortDir] = useState<SortDir>("desc");
  const [editingProduct, setEditingProduct] = useState<SellerProduct | null>(null);
  const [stockProduct, setStockProduct] = useState<SellerProduct | null>(null);
  const [showBusinessInfo, setShowBusinessInfo] = useState(false);
  const [orderDetailsPkg, setOrderDetailsPkg] = useState<PendingPackage | null>(null);
  const [reportIssuePkg, setReportIssuePkg] = useState<PendingPackage | null>(null);
  const [trackingOrderId, setTrackingOrderId] = useState<string | undefined>(undefined);
  const [issueDetails, setIssueDetails] = useState("");
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

  if (page === "manage-stock" && stockProduct && seller) {
    return (
      <ManageStockPage
        sellerId={seller.id}
        product={stockProduct}
        onBack={() => { setStockProduct(null); setPage("dashboard"); }}
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

  const netEarnings = (seller?.accumulatedSalesAmount ?? 0) - (seller?.accumulatedCommissionsAmount ?? 0);

  return (
    <div className="py-8 px-4">
      {/* Header with collapsible business info */}
      <div className="bg-white border border-gray-200 rounded-xl p-6 mb-6">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div>
              <h1 className="text-2xl font-bold text-gray-900">Sales</h1>
              <p className="text-sm text-gray-500">{seller.name} &middot; {seller.email}</p>
            </div>
          </div>
          <div className="flex items-center gap-3">
            <span className="inline-flex items-center gap-1.5 bg-emerald-100 text-emerald-700 px-3 py-1 rounded-full text-sm font-medium">
              <span className="h-2 w-2 rounded-full bg-emerald-500" />
              Active
            </span>
            <button
              onClick={() => setShowBusinessInfo(!showBusinessInfo)}
              className="text-sm text-gray-500 hover:text-gray-700 flex items-center gap-1 transition"
            >
              {showBusinessInfo ? "Hide" : "Show"} Business Info
              <svg className={`w-4 h-4 transition-transform ${showBusinessInfo ? "rotate-180" : ""}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </button>
          </div>
        </div>

        {showBusinessInfo && (
          <div className="mt-4 pt-4 border-t border-gray-100 grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
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
        )}
      </div>

      {/* Financial Summary - single row */}
      <div className="bg-white border border-gray-200 rounded-xl p-4 mb-6">
        <div className="flex items-center justify-between divide-x divide-gray-200">
          <div className="flex-1 text-center px-4">
            <p className="text-xs text-gray-500 uppercase tracking-wide">Revenue</p>
            <p className="text-xl font-bold text-gray-900">${seller.accumulatedSalesAmount.toFixed(2)}</p>
          </div>
          <div className="flex-1 text-center px-4">
            <p className="text-xs text-gray-500 uppercase tracking-wide">Commissions</p>
            <p className="text-xl font-bold text-gray-900">${seller.accumulatedCommissionsAmount.toFixed(2)}</p>
          </div>
          <div className="flex-1 text-center px-4">
            <p className="text-xs text-gray-500 uppercase tracking-wide">Net Earnings</p>
            <p className={`text-xl font-bold ${netEarnings >= 0 ? "text-emerald-600" : "text-red-600"}`}>
              ${netEarnings.toFixed(2)}
            </p>
          </div>
        </div>
      </div>

      {/* Latest Sells */}
      <div className="bg-white border border-gray-200 rounded-xl p-6 mb-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Latest Sells</h2>
        <SalesTable
          packages={sellerPackages}
          loading={loadingPackages}
          onRowClick={(pkg) => setOrderDetailsPkg(pkg)}
          onConfirm={(pkg) => startProcessing.mutate(pkg.orderId)}
          confirmDisabled={startProcessing.isPending}
          onMarkReady={(pkg) => markReady.mutate(pkg.orderId)}
          markReadyDisabled={markReady.isPending}
          onReportIssue={(pkg) => { setReportIssuePkg(pkg); setIssueDetails(""); }}
          reportIssueDisabled={reportIssue.isPending}
          onTrackShipping={(pkg) => setTrackingOrderId(pkg.orderId)}
        />
      </div>

      {/* Order Details Modal */}
      {orderDetailsPkg && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40" onClick={() => setOrderDetailsPkg(null)}>
          <div className="bg-white rounded-xl shadow-xl max-w-lg w-full mx-4 p-6" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900">Order Details</h3>
              <button onClick={() => setOrderDetailsPkg(null)} className="text-gray-400 hover:text-gray-600 text-xl">&times;</button>
            </div>
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <p className="text-gray-500">Order ID</p>
                  <p className="font-medium text-gray-900 break-all">{orderDetailsPkg.orderId}</p>
                </div>
                <div>
                  <p className="text-gray-500">Status</p>
                  <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${
                    orderDetailsPkg.status === "Pending" ? "bg-yellow-100 text-yellow-800" :
                    orderDetailsPkg.status === "Preparing" ? "bg-blue-100 text-blue-800" :
                    orderDetailsPkg.status === "ReadyForPickup" ? "bg-emerald-100 text-emerald-800" :
                    orderDetailsPkg.status === "Dispatched" ? "bg-purple-100 text-purple-800" :
                    orderDetailsPkg.status === "Failed" ? "bg-red-100 text-red-800" :
                    "bg-gray-100 text-gray-800"
                  }`}>
                    {orderDetailsPkg.status === "ReadyForPickup" ? "Ready for Pickup" : orderDetailsPkg.status}
                  </span>
                </div>
              </div>
              <div className="border-t border-gray-100 pt-4">
                <h4 className="text-sm font-medium text-gray-700 mb-2">Customer Information</h4>
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <p className="text-gray-500">Name</p>
                    <p className="font-medium text-gray-900">{orderDetailsPkg.customerName || "—"}</p>
                  </div>
                  <div>
                    <p className="text-gray-500">Email</p>
                    <p className="font-medium text-gray-900">{orderDetailsPkg.customerEmail || "—"}</p>
                  </div>
                </div>
              </div>
              <div className="border-t border-gray-100 pt-4">
                <h4 className="text-sm font-medium text-gray-700 mb-2">Products</h4>
                {orderDetailsPkg.items.length === 0 ? (
                  <p className="text-sm text-gray-400">No product details available.</p>
                ) : (
                  <div className="space-y-2">
                    {orderDetailsPkg.items.map((item, idx) => (
                      <div key={idx} className="flex items-center justify-between bg-gray-50 rounded-lg px-3 py-2 text-sm">
                        <span className="font-medium text-gray-900">{item.productName || item.productId.slice(0, 8)}</span>
                        <span className="text-gray-600">x{item.quantity}</span>
                      </div>
                    ))}
                    <div className="flex items-center justify-between px-3 py-2 text-sm font-medium border-t border-gray-200 mt-2 pt-2">
                      <span className="text-gray-700">Total Items</span>
                      <span className="text-gray-900">{orderDetailsPkg.items.reduce((sum, i) => sum + i.quantity, 0)}</span>
                    </div>
                  </div>
                )}
              </div>
              {orderDetailsPkg.preparedAt && (
                <div className="border-t border-gray-100 pt-4 text-sm">
                  <p className="text-gray-500">Prepared At</p>
                  <p className="font-medium text-gray-900">{new Date(orderDetailsPkg.preparedAt).toLocaleString()}</p>
                </div>
              )}
              {orderDetailsPkg.readyAt && (
                <div className="border-t border-gray-100 pt-4 text-sm">
                  <p className="text-gray-500">Ready At</p>
                  <p className="font-medium text-gray-900">{new Date(orderDetailsPkg.readyAt).toLocaleString()}</p>
                </div>
              )}
              {orderDetailsPkg.status === "Failed" && (
                <div className="border-t border-gray-100 pt-4 text-sm">
                  <p className="text-gray-500">Issue</p>
                  <p className="font-medium text-red-700">{orderDetailsPkg.issueType}: {orderDetailsPkg.issueDetails}</p>
                </div>
              )}
            </div>
            <div className="mt-6 flex justify-end">
              <button
                onClick={() => setOrderDetailsPkg(null)}
                className="rounded-lg bg-gray-100 text-gray-700 px-4 py-2 text-sm font-medium hover:bg-gray-200 transition"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Report Issue Modal */}
      {reportIssuePkg && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40" onClick={() => setReportIssuePkg(null)}>
          <div className="bg-white rounded-xl shadow-xl max-w-md w-full mx-4 p-6" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900">Report Issue & Cancel Order</h3>
              <button onClick={() => setReportIssuePkg(null)} className="text-gray-400 hover:text-gray-600 text-xl">&times;</button>
            </div>
            <p className="text-sm text-gray-600 mb-4">
              Order <span className="font-medium">{reportIssuePkg.orderId.slice(0, 8)}...</span> for <span className="font-medium">{reportIssuePkg.customerName || "unknown customer"}</span> will be cancelled.
            </p>
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">What went wrong?</label>
              <textarea
                value={issueDetails}
                onChange={(e) => setIssueDetails(e.target.value)}
                placeholder="Describe the issue (e.g., product out of stock, damaged item, incorrect order...)"
                rows={4}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-red-500 focus:ring-1 focus:ring-red-500"
              />
            </div>
            <div className="flex items-center justify-end gap-3">
              <button
                onClick={() => setReportIssuePkg(null)}
                className="rounded-lg bg-gray-100 text-gray-700 px-4 py-2 text-sm font-medium hover:bg-gray-200 transition"
              >
                Cancel
              </button>
              <button
                onClick={() => {
                  reportIssue.mutate(
                    { orderId: reportIssuePkg.orderId, issueType: "SellerReported", details: issueDetails },
                    { onSuccess: () => setReportIssuePkg(null) }
                  );
                }}
                disabled={reportIssue.isPending || !issueDetails.trim()}
                className="rounded-lg bg-red-600 text-white px-4 py-2 text-sm font-medium hover:bg-red-700 transition disabled:opacity-50"
              >
                {reportIssue.isPending ? "Submitting..." : "Report & Cancel Order"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Shipping Tracking Modal */}
      {trackingOrderId && (
        <ShippingTrackingModal orderId={trackingOrderId} onClose={() => setTrackingOrderId(undefined)} />
      )}

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
            sortKey={productSortKey}
            sortDir={productSortDir}
            onSortChange={(key, dir) => { setProductSortKey(key); setProductSortDir(dir); setCurrentPage(1); }}
            onEdit={(product) => { setEditingProduct(product); setPage("edit"); }}
            onRemove={(product) => {
              if (confirm(`Remove "${product.name}"? This action cannot be undone.`)) {
                deleteProduct.mutate(product.id);
              }
            }}
            onManageStock={(product) => { setStockProduct(product); setPage("manage-stock"); }}
          />
        )}
      </div>
    </div>
  );
}

function ShippingTrackingModal({ orderId, onClose }: { orderId: string; onClose: () => void }) {
  const { data: history = [], isLoading, isError } = useShippingHistory(orderId);
  const simulatorUrl = import.meta.env.VITE_SHIPPING_SIMULATOR_URL as string | undefined;

  const statusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case "accepted": case "confirmed": return "bg-yellow-100 text-yellow-800 border-yellow-300";
      case "picked_up": case "shipped": return "bg-blue-100 text-blue-800 border-blue-300";
      case "out_for_delivery": return "bg-indigo-100 text-indigo-800 border-indigo-300";
      case "delivered": return "bg-emerald-100 text-emerald-800 border-emerald-300";
      case "failed": case "cancelled": return "bg-red-100 text-red-800 border-red-300";
      default: return "bg-gray-100 text-gray-800 border-gray-300";
    }
  };

  const latest = history.length > 0 ? history[history.length - 1] : null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40" onClick={onClose}>
      <div className="bg-white rounded-xl shadow-xl max-w-lg w-full mx-4 p-6" onClick={(e) => e.stopPropagation()}>
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold text-gray-900">Shipping Tracking</h3>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl">&times;</button>
        </div>

        {isLoading && <p className="text-sm text-gray-400 py-4 text-center">Loading shipping info...</p>}
        {isError && <p className="text-sm text-red-500 py-4 text-center">Unable to load shipping information.</p>}
        {!isLoading && !isError && history.length === 0 && (
          <div className="text-center py-8">
            <div className="text-4xl mb-2">📦</div>
            <p className="text-gray-500">No shipping information available yet.</p>
            <p className="text-sm text-gray-400 mt-1">Tracking will appear once the carrier picks up the package.</p>
          </div>
        )}

        {latest && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <p className="text-gray-500">Carrier</p>
                <p className="font-medium text-gray-900">{latest.carrier || "—"}</p>
              </div>
              <div>
                <p className="text-gray-500">Tracking Number</p>
                <p className="font-medium text-gray-900">{latest.trackingNumber || "—"}</p>
              </div>
            </div>

            <div className="border-t border-gray-100 pt-4">
              <h4 className="text-sm font-medium text-gray-700 mb-3">Status History</h4>
              <div className="relative">
                <div className="absolute left-3 top-2 bottom-2 w-0.5 bg-gray-200" />
                <div className="space-y-4">
                  {[...history].reverse().map((entry, idx) => (
                    <div key={idx} className="flex items-start gap-3 relative">
                      <div className={`w-6 h-6 rounded-full border-2 flex items-center justify-center shrink-0 z-10 bg-white ${statusColor(entry.status)}`}>
                        <span className="text-xs">
                          {idx === 0 ? "●" : "○"}
                        </span>
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${statusColor(entry.status)}`}>
                            {entry.status.replace(/_/g, " ")}
                          </span>
                        </div>
                        <p className="text-xs text-gray-500 mt-0.5">
                          {new Date(entry.occurredAt).toLocaleString()}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        )}

        <div className="mt-6 flex items-center justify-between gap-3">
          {simulatorUrl ? (
            <a
              href={`${simulatorUrl}/shipment/by-order/${orderId}`}
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center gap-1.5 rounded-lg border border-indigo-200 bg-indigo-50 px-4 py-2 text-sm font-medium text-indigo-700 hover:bg-indigo-100 transition"
            >
              🚚 View on shipping provider
            </a>
          ) : <span />}
          <button onClick={onClose} className="rounded-lg bg-gray-100 text-gray-700 px-4 py-2 text-sm font-medium hover:bg-gray-200 transition">
            Close
          </button>
        </div>
      </div>
    </div>
  );
}

function SalesTable({
  packages,
  loading,
  onRowClick,
  onConfirm,
  confirmDisabled,
  onMarkReady,
  markReadyDisabled,
  onReportIssue,
  reportIssueDisabled,
  onTrackShipping,
}: {
  packages: PendingPackage[];
  loading: boolean;
  onRowClick: (pkg: PendingPackage) => void;
  onConfirm: (pkg: PendingPackage) => void;
  confirmDisabled: boolean;
  onMarkReady: (pkg: PendingPackage) => void;
  markReadyDisabled: boolean;
  onReportIssue: (pkg: PendingPackage) => void;
  reportIssueDisabled: boolean;
  onTrackShipping: (pkg: PendingPackage) => void;
}) {
  const [search, setSearch] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [sortKey, setSortKey] = useState<SalesSortKey>("updated");
  const [sortDir, setSortDir] = useState<SortDir>("desc");
  const pageSize = 5;

  const productLabel = (pkg: PendingPackage) =>
    pkg.items.length > 0
      ? pkg.items.length === 1
        ? pkg.items[0].productName || pkg.items[0].productId.slice(0, 8)
        : `${pkg.items[0].productName || pkg.items[0].productId.slice(0, 8)} +${pkg.items.length - 1}`
      : "";
  const qtyOf = (pkg: PendingPackage) => pkg.items.reduce((sum, i) => sum + i.quantity, 0);

  const term = search.toLowerCase();
  const filtered = packages.filter((pkg) =>
    (pkg.customerName || "").toLowerCase().includes(term) ||
    productLabel(pkg).toLowerCase().includes(term) ||
    pkg.status.toLowerCase().includes(term)
  );

  const sorted = [...filtered].sort((a, b) => {
    let cmp = 0;
    switch (sortKey) {
      case "customer":
        cmp = (a.customerName || "").localeCompare(b.customerName || "");
        break;
      case "product":
        cmp = productLabel(a).localeCompare(productLabel(b));
        break;
      case "qty":
        cmp = qtyOf(a) - qtyOf(b);
        break;
      case "status":
        cmp = a.status.localeCompare(b.status);
        break;
      case "updated":
        cmp = new Date(a.updatedAt).getTime() - new Date(b.updatedAt).getTime();
        break;
    }
    return sortDir === "asc" ? cmp : -cmp;
  });

  const totalPages = Math.max(1, Math.ceil(sorted.length / pageSize));
  const safePage = Math.min(currentPage, totalPages);
  const paginated = sorted.slice((safePage - 1) * pageSize, safePage * pageSize);

  const toggleSort = (key: SalesSortKey) => {
    if (sortKey === key) {
      setSortDir(sortDir === "asc" ? "desc" : "asc");
    } else {
      setSortKey(key);
      setSortDir("asc");
    }
    setCurrentPage(1);
  };

  if (loading) {
    return <p className="text-sm text-gray-400">Loading...</p>;
  }

  if (packages.length === 0) {
    return (
      <div className="text-center py-8">
        <div className="text-4xl mb-2">🛒</div>
        <p className="text-gray-500">No sells yet.</p>
        <p className="text-sm text-gray-400 mt-1">Orders will appear here as customers purchase your products.</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <input
        type="text"
        placeholder="Search sells..."
        value={search}
        onChange={(e) => { setSearch(e.target.value); setCurrentPage(1); }}
        className="w-full md:w-64 rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
      />

      {filtered.length === 0 ? (
        <div className="text-center py-6 text-gray-500 text-sm">No sells match your search.</div>
      ) : (
        <>
          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left">
              <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
                <tr>
                  <SortableHeader label="Customer" active={sortKey === "customer"} dir={sortDir} onClick={() => toggleSort("customer")} />
                  <SortableHeader label="Product" active={sortKey === "product"} dir={sortDir} onClick={() => toggleSort("product")} />
                  <SortableHeader label="Qty" active={sortKey === "qty"} dir={sortDir} onClick={() => toggleSort("qty")} />
                  <SortableHeader label="Status" active={sortKey === "status"} dir={sortDir} onClick={() => toggleSort("status")} />
                  <SortableHeader label="Updated" active={sortKey === "updated"} dir={sortDir} onClick={() => toggleSort("updated")} />
                  <th className="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {paginated.map((pkg) => (
                  <tr key={pkg.orderId} className="hover:bg-gray-50 cursor-pointer" onClick={() => onRowClick(pkg)}>
                    <td className="px-4 py-3 font-medium text-gray-900">{pkg.customerName || "—"}</td>
                    <td className="px-4 py-3 text-gray-700">{productLabel(pkg) || "—"}</td>
                    <td className="px-4 py-3 text-gray-700">{qtyOf(pkg) || "—"}</td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${
                        pkg.status === "Pending" ? "bg-yellow-100 text-yellow-800" :
                        pkg.status === "Preparing" ? "bg-blue-100 text-blue-800" :
                        pkg.status === "ReadyForPickup" ? "bg-emerald-100 text-emerald-800" :
                        pkg.status === "Dispatched" ? "bg-purple-100 text-purple-800" :
                        pkg.status === "Failed" ? "bg-red-100 text-red-800" :
                        "bg-gray-100 text-gray-800"
                      }`}>
                        {pkg.status === "ReadyForPickup" ? "Ready for Pickup" : pkg.status}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-gray-500">
                      {new Date(pkg.updatedAt).toLocaleDateString()}
                    </td>
                    <td className="px-4 py-3 text-right" onClick={(e) => e.stopPropagation()}>
                      <div className="flex items-center justify-end gap-2">
                        {pkg.status === "Pending" && (
                          <>
                            <button
                              onClick={() => onConfirm(pkg)}
                              disabled={confirmDisabled}
                              className="rounded-lg bg-emerald-50 text-emerald-700 px-3 py-1.5 text-xs font-medium hover:bg-emerald-100 transition disabled:opacity-50"
                            >
                              ✓ Confirm
                            </button>
                            <button
                              onClick={() => onReportIssue(pkg)}
                              disabled={reportIssueDisabled}
                              className="rounded-lg bg-red-50 text-red-700 px-3 py-1.5 text-xs font-medium hover:bg-red-100 transition disabled:opacity-50"
                            >
                              ⚠ Report Issue
                            </button>
                          </>
                        )}
                        {pkg.status === "Preparing" && (
                          <>
                            <button
                              onClick={() => onMarkReady(pkg)}
                              disabled={markReadyDisabled}
                              className="rounded-lg bg-emerald-50 text-emerald-700 px-3 py-1.5 text-xs font-medium hover:bg-emerald-100 transition disabled:opacity-50"
                            >
                              ✓ Ready for Pickup
                            </button>
                            <button
                              onClick={() => onReportIssue(pkg)}
                              disabled={reportIssueDisabled}
                              className="rounded-lg bg-red-50 text-red-700 px-3 py-1.5 text-xs font-medium hover:bg-red-100 transition disabled:opacity-50"
                            >
                              ⚠ Report Issue
                            </button>
                          </>
                        )}
                        {(pkg.status === "ReadyForPickup" || pkg.status === "Dispatched") && (
                          <button
                            onClick={() => onTrackShipping(pkg)}
                            className="rounded-lg bg-purple-50 text-purple-700 px-3 py-1.5 text-xs font-medium hover:bg-purple-100 transition"
                          >
                            🚚 Track Shipping
                          </button>
                        )}
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
              Showing {(safePage - 1) * pageSize + 1}–{Math.min(safePage * pageSize, sorted.length)} of {sorted.length}
            </p>
            <div className="flex items-center gap-1">
              <button
                onClick={() => setCurrentPage(safePage - 1)}
                disabled={safePage === 1}
                className="rounded px-2.5 py-1.5 text-xs font-medium text-gray-700 bg-gray-100 hover:bg-gray-200 disabled:opacity-40 disabled:cursor-not-allowed transition"
              >
                Previous
              </button>
              {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
                <button
                  key={p}
                  onClick={() => setCurrentPage(p)}
                  className={`rounded px-2.5 py-1.5 text-xs font-medium transition ${
                    p === safePage
                      ? "bg-indigo-600 text-white"
                      : "text-gray-700 bg-gray-100 hover:bg-gray-200"
                  }`}
                >
                  {p}
                </button>
              ))}
              <button
                onClick={() => setCurrentPage(safePage + 1)}
                disabled={safePage === totalPages}
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

function ProductsTable({
  products,
  loading,
  searchTerm,
  onSearchChange,
  currentPage,
  pageSize,
  onPageChange,
  sortKey,
  sortDir,
  onSortChange,
  onEdit,
  onRemove,
  onManageStock,
}: {
  products: SellerProduct[];
  loading: boolean;
  searchTerm: string;
  onSearchChange: (value: string) => void;
  currentPage: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  sortKey: ProductSortKey;
  sortDir: SortDir;
  onSortChange: (key: ProductSortKey, dir: SortDir) => void;
  onEdit: (product: SellerProduct) => void;
  onRemove: (product: SellerProduct) => void;
  onManageStock: (product: SellerProduct) => void;
}) {
  const filtered = products.filter((p) =>
    p.name.toLowerCase().includes(searchTerm.toLowerCase())
  );
  const sorted = [...filtered].sort((a, b) => {
    let cmp = 0;
    switch (sortKey) {
      case "name":
        cmp = a.name.localeCompare(b.name);
        break;
      case "category":
        cmp = (a.category?.name ?? "").localeCompare(b.category?.name ?? "");
        break;
      case "price":
        cmp = a.price - b.price;
        break;
      case "createdAt":
        cmp = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
        break;
    }
    return sortDir === "asc" ? cmp : -cmp;
  });
  const totalPages = Math.max(1, Math.ceil(sorted.length / pageSize));
  const paginated = sorted.slice((currentPage - 1) * pageSize, currentPage * pageSize);

  const toggleSort = (key: ProductSortKey) => {
    if (sortKey === key) {
      onSortChange(key, sortDir === "asc" ? "desc" : "asc");
    } else {
      onSortChange(key, "asc");
    }
  };

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
          <div className="overflow-visible">
            <table className="w-full text-sm text-left">
              <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
                <tr>
                  <SortableHeader label="Product" active={sortKey === "name"} dir={sortDir} onClick={() => toggleSort("name")} />
                  <SortableHeader label="Category" active={sortKey === "category"} dir={sortDir} onClick={() => toggleSort("category")} />
                  <SortableHeader label="Price" active={sortKey === "price"} dir={sortDir} onClick={() => toggleSort("price")} />
                  <SortableHeader label="Created" active={sortKey === "createdAt"} dir={sortDir} onClick={() => toggleSort("createdAt")} />
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
                      <MeatballsMenu
                        product={product}
                        onEdit={onEdit}
                        onRemove={onRemove}
                        onManageStock={onManageStock}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          <div className="flex items-center justify-between pt-2">
            <p className="text-xs text-gray-500">
              Showing {(currentPage - 1) * pageSize + 1}–{Math.min(currentPage * pageSize, sorted.length)} of {sorted.length}
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

              function MeatballsMenu({
                product,
                onEdit,
                onRemove,
                onManageStock,
              }: {
                product: SellerProduct;
                onEdit: (product: SellerProduct) => void;
                onRemove: (product: SellerProduct) => void;
                onManageStock: (product: SellerProduct) => void;
              }) {
                const [open, setOpen] = useState(false);
                const menuRef = useRef<HTMLDivElement>(null);

                useEffect(() => {
                  const handleClickOutside = (e: MouseEvent) => {
                    if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
                      setOpen(false);
                    }
                  };
                  document.addEventListener("mousedown", handleClickOutside);
                  return () => document.removeEventListener("mousedown", handleClickOutside);
                }, []);

                return (
                  <div className="relative inline-block text-left" ref={menuRef}>
                    <button
                      onClick={() => setOpen(!open)}
                      className="rounded p-1.5 hover:bg-gray-100 transition"
                      aria-label="Actions"
                    >
                      <svg className="w-5 h-5 text-gray-500" fill="currentColor" viewBox="0 0 20 20">
                        <circle cx="4" cy="10" r="2" />
                        <circle cx="10" cy="10" r="2" />
                        <circle cx="16" cy="10" r="2" />
                      </svg>
                    </button>
                    {open && (
                      <div className="absolute right-0 z-10 mt-1 w-40 origin-top-right rounded-lg bg-white shadow-lg ring-1 ring-black/5 py-1">
                        <button
                          onClick={() => { setOpen(false); onEdit(product); }}
                          className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                        >
                          ✏️ Edit
                        </button>
                        <button
                          onClick={() => { setOpen(false); onManageStock(product); }}
                          className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                        >
                          📦 Manage Stock
                        </button>
                        <button
                          onClick={() => { setOpen(false); onRemove(product); }}
                          className="w-full text-left px-4 py-2 text-sm text-red-600 hover:bg-red-50"
                        >
                          🗑️ Remove
                        </button>
                      </div>
                    )}
                  </div>
                );
              }

              function ManageStockPage({
                sellerId,
                product,
                onBack,
              }: {
                sellerId: string;
                product: SellerProduct;
                onBack: () => void;
              }) {
                const { data: stockData, isLoading } = useProductStock(sellerId, product.id);
                const updateStock = useUpdateProductStock(sellerId);
                const [quantity, setQuantity] = useState("");

                const currentStock = stockData?.[0]?.quantity ?? 0;

                return (
                  <div className="py-8 px-4 max-w-xl mx-auto">
                    <div className="flex items-center gap-4 mb-8">
                      <button onClick={onBack} className="text-gray-500 hover:text-gray-700">← Back</button>
                      <h1 className="text-2xl font-bold text-gray-900">Manage Stock</h1>
                    </div>

                    <div className="bg-white border border-gray-200 rounded-xl p-6 space-y-6">
                      <div className="flex items-center gap-4">
                        <img src={product.imageUrl} alt={product.name} className="h-16 w-16 rounded object-cover bg-gray-100" />
                        <div>
                          <p className="font-semibold text-gray-900">{product.name}</p>
                          <p className="text-sm text-gray-500">${product.price.toFixed(2)}</p>
                        </div>
                      </div>

                      <div className="bg-gray-50 rounded-lg p-4">
                        <p className="text-sm text-gray-500 mb-1">Current Stock</p>
                        {isLoading ? (
                          <p className="text-gray-400 text-sm">Loading...</p>
                        ) : (
                          <p className="text-3xl font-bold text-gray-900">{currentStock} <span className="text-base font-normal text-gray-500">units</span></p>
                        )}
                      </div>

                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Add Stock</label>
                        <div className="flex gap-3">
                          <input
                            type="number"
                            min="1"
                            step="1"
                            value={quantity}
                            onChange={(e) => setQuantity(e.target.value)}
                            placeholder="Quantity to add"
                            className="flex-1 rounded-lg border border-gray-300 px-4 py-2 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                          />
                          <button
                            onClick={() => {
                              const qty = parseInt(quantity);
                              if (qty > 0) {
                                updateStock.mutate({ productId: product.id, quantity: qty });
                                setQuantity("");
                              }
                            }}
                            disabled={!quantity || parseInt(quantity) <= 0 || updateStock.isPending}
                            className="rounded-lg bg-indigo-600 px-6 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition"
                          >
                            {updateStock.isPending ? "Updating..." : "Add Stock"}
                          </button>
                        </div>
                      </div>

                      {updateStock.isSuccess && (
                        <p className="text-emerald-600 text-sm">✓ Stock updated successfully.</p>
                      )}
                      {updateStock.isError && (
                        <p className="text-red-600 text-sm">Failed to update stock. Please try again.</p>
                      )}
                    </div>
                  </div>
                );
              }

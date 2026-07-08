import { useSeller } from "./hooks/useSeller";

export default function App() {
  const { data: seller, isLoading } = useSeller();

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
            <p className="font-medium text-gray-900">{seller.publishedProducts}</p>
          </div>
          <div>
            <p className="text-gray-500">Ledger Entries</p>
            <p className="font-medium text-gray-900">{seller.ledgerEntries}</p>
          </div>
        </div>
      </div>

      {/* Products */}
      <div className="bg-white border border-gray-200 rounded-xl p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Products</h2>
        {seller.publishedProducts === 0 ? (
          <div className="text-center py-8">
            <div className="text-4xl mb-2">📦</div>
            <p className="text-gray-500">No products published yet.</p>
            <p className="text-sm text-gray-400 mt-1">Start listing your products to begin selling on eShop Academy.</p>
          </div>
        ) : (
          <p className="text-gray-600">
            You have <span className="font-semibold">{seller.publishedProducts}</span> published product(s).
          </p>
        )}
      </div>
    </div>
  );
}

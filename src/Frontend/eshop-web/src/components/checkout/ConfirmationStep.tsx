import type { Address } from "../../types";
import type { BasketItemWithDetails } from "../../types";

interface ConfirmationStepProps {
  address: Address;
  paymentMethod: string;
  items: BasketItemWithDetails[];
  total: number;
  isProcessing: boolean;
  error?: string;
  onConfirm: () => void;
  onBack: () => void;
}

const PAYMENT_METHOD_NAMES: Record<string, string> = {
  "credit-card": "Credit or Debit Card",
  "paypal": "PayPal",
  "apple-pay": "Apple Pay",
  "google-pay": "Google Pay",
  "bank-transfer": "Bank Transfer",
};

export default function ConfirmationStep({
  address,
  paymentMethod,
  items,
  total,
  isProcessing,
  error,
  onConfirm,
  onBack,
}: ConfirmationStepProps) {
  const formatAddress = (addr: Address): string => {
    const parts = [
      `${addr.street} ${addr.number}`,
      addr.additionalInformation,
      `${addr.zipCode} ${addr.city}`,
      addr.state,
      addr.country,
    ].filter(Boolean);
    return parts.join(", ");
  };

  return (
    <div>
      <h2 className="mb-6 text-xl font-semibold">Review & Confirm</h2>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Order Details - Takes 2 columns */}
        <div className="lg:col-span-2 space-y-6">
          {/* Order Items */}
          <div className="rounded-lg bg-white border border-gray-200 p-6">
            <h3 className="mb-4 font-semibold text-gray-900">Order Items</h3>
            <div className="divide-y">
              {items.map((item) => (
                <div key={item.product.id} className="flex gap-4 py-3 first:pt-0 last:pb-0">
                  <div className="flex-1">
                    <p className="font-medium text-gray-900">{item.product.name}</p>
                    <p className="text-sm text-gray-500">Quantity: {item.quantity}</p>
                  </div>
                  <div className="text-right">
                    <p className="font-medium text-gray-900">
                      ${(item.product.price * item.quantity).toFixed(2)}
                    </p>
                    <p className="text-sm text-gray-500">
                      ${item.product.price.toFixed(2)} each
                    </p>
                  </div>
                </div>
              ))}
            </div>
            <div className="mt-4 pt-4 border-t border-gray-200 flex justify-between">
              <span className="font-bold text-lg text-gray-900">Total</span>
              <span className="font-bold text-lg text-red-700">${total.toFixed(2)}</span>
            </div>
          </div>

          {/* Shipping Address */}
          <div className="rounded-lg bg-white border border-gray-200 p-6">
            <div className="flex items-start justify-between mb-3">
              <h3 className="font-semibold text-gray-900">Shipping Address</h3>
              <button
                onClick={onBack}
                className="text-sm text-amber-600 hover:text-amber-700 font-medium"
              >
                Edit
              </button>
            </div>
            <div className="flex gap-3">
              <svg
                className="h-5 w-5 text-gray-400 flex-shrink-0 mt-0.5"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"
                />
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"
                />
              </svg>
              <p className="text-gray-700">{formatAddress(address)}</p>
            </div>
          </div>

          {/* Payment Method */}
          <div className="rounded-lg bg-white border border-gray-200 p-6">
            <div className="flex items-start justify-between mb-3">
              <h3 className="font-semibold text-gray-900">Payment Method</h3>
              <button
                onClick={onBack}
                className="text-sm text-amber-600 hover:text-amber-700 font-medium"
              >
                Edit
              </button>
            </div>
            <div className="flex gap-3">
              <svg
                className="h-5 w-5 text-gray-400 flex-shrink-0 mt-0.5"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"
                />
              </svg>
              <p className="text-gray-700">{PAYMENT_METHOD_NAMES[paymentMethod] || paymentMethod}</p>
            </div>
          </div>
        </div>

        {/* Action Panel - Takes 1 column */}
        <div>
          <div className="sticky top-4 space-y-4">
            {/* Order Summary Card */}
            <div className="rounded-lg bg-gray-50 border border-gray-200 p-6">
              <h3 className="mb-4 font-semibold text-gray-900">Order Summary</h3>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-600">Subtotal</span>
                  <span className="text-gray-900">${total.toFixed(2)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Shipping</span>
                  <span className="text-green-600 font-medium">FREE</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Tax</span>
                  <span className="text-gray-900">Calculated at payment</span>
                </div>
                <div className="pt-3 mt-3 border-t border-gray-300 flex justify-between font-bold text-base">
                  <span className="text-gray-900">Total</span>
                  <span className="text-red-700">${total.toFixed(2)}</span>
                </div>
              </div>
            </div>

            {/* Place Order Button */}
            <button
              onClick={onConfirm}
              disabled={isProcessing}
              className="w-full rounded-full bg-amber-400 py-4 font-bold text-gray-900 hover:bg-amber-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors shadow-md hover:shadow-lg"
            >
              {isProcessing ? (
                <span className="flex items-center justify-center gap-2">
                  <div className="h-5 w-5 animate-spin rounded-full border-2 border-gray-800 border-t-transparent" />
                  Processing...
                </span>
              ) : (
                "Place Order"
              )}
            </button>

            {error && (
              <div className="rounded-lg bg-red-50 border border-red-200 p-4">
                <p className="text-sm text-red-600">{error}</p>
              </div>
            )}

            {/* Security Notice */}
            <div className="rounded-lg bg-green-50 border border-green-200 p-4">
              <div className="flex gap-2">
                <svg
                  className="h-5 w-5 text-green-600 flex-shrink-0"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
                  />
                </svg>
                <div className="text-xs text-green-800">
                  <p className="font-medium mb-1">Secure Checkout</p>
                  <p className="text-green-700">
                    Your information is encrypted and protected
                  </p>
                </div>
              </div>
            </div>

            <button
              onClick={onBack}
              disabled={isProcessing}
              className="w-full rounded-full border-2 border-gray-300 py-3 font-semibold text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              Back to Payment
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

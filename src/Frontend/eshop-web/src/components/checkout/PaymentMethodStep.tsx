import { useState } from "react";
import type { PaymentMethod } from "../../types";

interface PaymentMethodStepProps {
  selectedMethod?: string;
  onNext: (methodId: string) => void;
  onBack: () => void;
}

const PAYMENT_METHODS: PaymentMethod[] = [
  {
    id: "credit-card",
    name: "Credit or Debit Card",
    icon: "??",
    enabled: true,
  },
  {
    id: "paypal",
    name: "PayPal",
    icon: "???",
    enabled: false,
  },
  {
    id: "apple-pay",
    name: "Apple Pay",
    icon: "??",
    enabled: false,
  },
  {
    id: "google-pay",
    name: "Google Pay",
    icon: "??",
    enabled: false,
  },
  {
    id: "bank-transfer",
    name: "Bank Transfer",
    icon: "??",
    enabled: false,
  },
];

export default function PaymentMethodStep({
  selectedMethod: initialMethod,
  onNext,
  onBack,
}: PaymentMethodStepProps) {
  const [selectedMethod, setSelectedMethod] = useState<string>(
    initialMethod ?? "credit-card"
  );
  const [error, setError] = useState<string>("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    const method = PAYMENT_METHODS.find((m) => m.id === selectedMethod);
    if (!method?.enabled) {
      setError("Please select an available payment method");
      return;
    }

    onNext(selectedMethod);
  };

  const handleMethodSelect = (methodId: string) => {
    const method = PAYMENT_METHODS.find((m) => m.id === methodId);
    if (method?.enabled) {
      setSelectedMethod(methodId);
      setError("");
    }
  };

  return (
    <div>
      <h2 className="mb-6 text-xl font-semibold">Payment Method</h2>

      <form onSubmit={handleSubmit} className="max-w-2xl">
        <div className="space-y-3">
          {PAYMENT_METHODS.map((method) => (
            <button
              key={method.id}
              type="button"
              onClick={() => handleMethodSelect(method.id)}
              disabled={!method.enabled}
              className={`w-full rounded-lg border-2 p-4 text-left transition-all ${
                selectedMethod === method.id && method.enabled
                  ? "border-amber-500 bg-amber-50"
                  : method.enabled
                  ? "border-gray-300 bg-white hover:border-gray-400"
                  : "border-gray-200 bg-gray-50 cursor-not-allowed opacity-60"
              }`}
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <span className="text-2xl">{method.icon}</span>
                  <div>
                    <div className="font-medium text-gray-900">
                      {method.name}
                      {!method.enabled && (
                        <span className="ml-2 rounded-full bg-gray-200 px-2 py-0.5 text-xs text-gray-600">
                          Coming Soon
                        </span>
                      )}
                    </div>
                    {method.enabled && method.id === "credit-card" && (
                      <p className="text-xs text-gray-500 mt-0.5">
                        Visa, Mastercard, American Express
                      </p>
                    )}
                  </div>
                </div>

                <div className="flex items-center">
                  {method.enabled && (
                    <div
                      className={`h-5 w-5 rounded-full border-2 flex items-center justify-center ${
                        selectedMethod === method.id
                          ? "border-amber-500 bg-amber-500"
                          : "border-gray-300"
                      }`}
                    >
                      {selectedMethod === method.id && (
                        <div className="h-2 w-2 rounded-full bg-white" />
                      )}
                    </div>
                  )}
                  {!method.enabled && (
                    <svg
                      className="h-5 w-5 text-gray-400"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                      />
                    </svg>
                  )}
                </div>
              </div>
            </button>
          ))}
        </div>

        {error && (
          <div className="mt-4 rounded-lg bg-red-50 border border-red-200 p-3">
            <p className="text-sm text-red-600">{error}</p>
          </div>
        )}

        <div className="mt-6 rounded-lg bg-blue-50 border border-blue-200 p-4">
          <div className="flex gap-2">
            <svg
              className="h-5 w-5 text-blue-600 flex-shrink-0 mt-0.5"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            <div className="text-sm text-blue-800">
              <p className="font-medium mb-1">Secure Payment</p>
              <p className="text-blue-700">
                Your payment information is encrypted and secure. We never store your card details.
              </p>
            </div>
          </div>
        </div>

        <div className="mt-8 flex gap-3">
          <button
            type="button"
            onClick={onBack}
            className="flex-1 rounded-full border-2 border-gray-300 py-3 font-semibold text-gray-700 hover:bg-gray-50 transition-colors"
          >
            Back to Address
          </button>
          <button
            type="submit"
            disabled={!PAYMENT_METHODS.find((m) => m.id === selectedMethod)?.enabled}
            className="flex-1 rounded-full bg-amber-400 py-3 font-semibold text-gray-900 hover:bg-amber-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            Continue to Review
          </button>
        </div>
      </form>
    </div>
  );
}

import { useParams, Link, useLocation } from "react-router";
import { useEffect } from "react";
import { useOrder } from "../hooks/useOrders";
import { useOrderStatusStream } from "../hooks/useOrderStatusStream";
import type { OrderStatus } from "../types";

// Define status pairs with their display info
type StatusPair = {
  pending: { statuses: OrderStatus[]; label: string };
  completed: { status: OrderStatus; label: string };
  errorMessage?: string;
};

const STATUS_PAIRS: StatusPair[] = [
  {
    pending: { statuses: ["Created"], label: "Processing payment" },
    completed: { status: "Paid", label: "Paid" },
    errorMessage: "Payment failed. Please try again."
  },
  {
    pending: { statuses: ["Confirmed", "Processing"], label: "Processing order" },
    completed: { status: "ReadyForPickup", label: "Packaged" },
    errorMessage: "Order processing failed. Contact support."
  },
  {
    pending: { statuses: ["ReadyForPickup"], label: "Ready for pickup" },
    completed: { status: "Shipped", label: "Shipped" },
    errorMessage: "Shipping issue occurred. We're working on it."
  },
  {
    pending: { statuses: ["Shipped"], label: "In transit" },
    completed: { status: "Delivered", label: "Delivered" },
    errorMessage: "Delivery issue occurred. Check tracking for details."
  },
];

function getStatusPairIndex(status: OrderStatus): { pairIndex: number; isCompleted: boolean; isFailed: boolean } {
  const isFailed = status === "Cancelled" || status === "Error";
  
  for (let i = 0; i < STATUS_PAIRS.length; i++) {
    const pair = STATUS_PAIRS[i];
    if (pair.pending.statuses.includes(status)) {
      return { pairIndex: i, isCompleted: false, isFailed: false };
    }
    if (status === pair.completed.status) {
      return { pairIndex: i, isCompleted: true, isFailed: false };
    }
  }
  
  // Default to first pair if status not recognized
  return { pairIndex: 0, isCompleted: false, isFailed };
}

function StatusTimeline({ current }: { current: OrderStatus }) {
  const { pairIndex, isCompleted, isFailed } = getStatusPairIndex(current);
  
  if (isFailed) {
    return (
      <div className="flex flex-col items-center justify-center py-12">
        <div className="max-w-2xl">
          <div className="flex items-center justify-center gap-3">
            <div className="flex h-24 w-24 items-center justify-center rounded-full bg-red-600 text-white">
              <svg className="h-12 w-12" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </div>
          </div>
          <div className="mt-6 text-center">
            <h3 className="text-xl font-semibold text-red-600">Order {current}</h3>
            <p className="mt-2 text-gray-600">
              {current === "Cancelled" 
                ? "This order has been cancelled. If you didn't request this, please contact support."
                : "An error occurred with your order. Our team has been notified and will contact you shortly."}
            </p>
            <Link 
              to="/orders" 
              className="mt-4 inline-block rounded-full bg-gray-900 px-6 py-2 text-sm font-medium text-white hover:bg-gray-800"
            >
              View all orders
            </Link>
          </div>
        </div>
      </div>
    );
  }

  // When a pair is completed, the next pair becomes current/pending
  const activePairIndex = isCompleted && pairIndex < STATUS_PAIRS.length - 1 ? pairIndex + 1 : pairIndex;

  return (
    <div className="flex flex-col items-center justify-center py-8">
      <div className="flex items-center justify-center gap-4 md:gap-8">
        {STATUS_PAIRS.map((pair, idx) => {
          // A pair is "passed" if it's before the active pair, OR if it's the completed pair
          const isPassed = idx < pairIndex || (idx === pairIndex && isCompleted);
          // The active pair is the one currently in progress (yellow)
          const isCurrent = idx === activePairIndex;
          // Show the appropriate label based on state
          const displayLabel = isPassed 
            ? pair.completed.label 
            : isCurrent 
            ? pair.pending.label 
            : pair.completed.label;
          
          return (
            <div key={idx} className="flex items-center">
              <div className="flex flex-col items-center">
                <div
                  className={`flex h-16 w-16 md:h-20 md:w-20 items-center justify-center rounded-full text-white font-bold text-sm md:text-base transition-all ${
                    isPassed
                      ? "bg-green-500"
                      : isCurrent
                      ? "bg-amber-500 ring-4 ring-amber-200"
                      : "bg-gray-300"
                  }`}
                >
                  {isPassed ? (
                    <svg className="h-8 w-8 md:h-10 md:w-10" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}>
                      <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                    </svg>
                  ) : (
                    <span className="text-xl md:text-2xl">{idx + 1}</span>
                  )}
                </div>
                <span className={`mt-3 text-xs md:text-sm font-medium text-center max-w-[80px] md:max-w-none ${
                  isCurrent ? "text-gray-900 font-semibold" : "text-gray-500"
                }`}>
                  {displayLabel}
                </span>
              </div>
              {idx < STATUS_PAIRS.length - 1 && (
                <div
                  className={`mx-2 md:mx-4 h-1 w-8 md:w-16 rounded ${
                    isPassed ? "bg-green-500" : "bg-gray-300"
                  }`}
                />
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default function OrderDetailPage() {
const { id } = useParams<{ id: string }>();
const { data: order, isLoading, error } = useOrder(id!);
const location = useLocation();
const paymentUrl = (location.state as { paymentUrl?: string })?.paymentUrl;

// Subscribe to real-time order status updates via SSE
const isTerminal = order?.status === "Delivered" || order?.status === "Cancelled";
useOrderStatusStream(isTerminal ? undefined : id);

// Auto-open payment URL in popup
  useEffect(() => {
    if (paymentUrl && order?.status === "Created") {
      const width = 600;
      const height = 700;
      const left = (window.screen.width - width) / 2;
      const top = (window.screen.height - height) / 2;
      
      window.open(
        paymentUrl,
        "payment",
        `width=${width},height=${height},left=${left},top=${top},resizable=yes,scrollbars=yes`
      );
    }
  }, [paymentUrl, order?.status]);

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-amber-500" />
      </div>
    );
  }

  if (error || !order) {
    return <p className="py-12 text-center text-red-600">Order not found.</p>;
  }

  const isPendingPayment = order.status === "Created";

  return (
    <>
      <Link to="/orders" className="mb-4 inline-block text-sm text-indigo-600 hover:underline">
        ? Back to orders
      </Link>

      <h1 className="mb-2 text-3xl font-bold">Order Details</h1>
      <p className="mb-6 font-mono text-xs text-gray-500">{order.id}</p>

      {paymentUrl && isPendingPayment && (
        <div className="mb-6 rounded-lg border-2 border-amber-400 bg-amber-50 p-4">
          <div className="flex items-start gap-3">
            <svg className="h-6 w-6 text-amber-600 flex-shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <div className="flex-1">
              <p className="font-semibold text-amber-900">Order placed! Complete your payment:</p>
              <a
                href={paymentUrl}
                target="_blank"
                rel="noreferrer"
                className="mt-1 inline-block text-sm text-amber-700 underline hover:text-amber-800 break-all"
              >
                {paymentUrl}
              </a>
              <p className="mt-2 text-xs text-amber-800">
                A payment window should have opened automatically. If not, click the link above.
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Timeline */}
      <StatusTimeline current={order.status} />

      <div className="mt-8 grid gap-6 lg:grid-cols-2">
        {/* Items */}
        <section className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
          <h2 className="mb-3 font-semibold">Items</h2>
          <div className="divide-y">
            {order.items?.map((item, idx) => (
              <div key={idx} className="flex justify-between py-2 text-sm">
                <span>
                  {item.product?.name ?? item.productID} × {item.quantity}
                </span>
                <span className="font-medium">${(item.price * item.quantity).toFixed(2)}</span>
              </div>
            ))}
          </div>
          <div className="mt-2 border-t pt-2 text-right font-bold">
            Total: ${order.totalPrice.toFixed(2)}
          </div>
        </section>

        {/* Payment & Shipping info */}
        <div className="space-y-4">
          <section className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
            <h2 className="mb-2 font-semibold">Payment</h2>
            <dl className="space-y-1 text-sm">
              <div className="flex justify-between">
                <dt className="text-gray-500">Status</dt>
                <dd className="font-medium">{order.payment?.status ?? "–"}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-gray-500">Amount</dt>
                <dd>${order.payment?.amount?.toFixed(2) ?? "–"}</dd>
              </div>
              {order.payment?.paidAt && (
                <div className="flex justify-between">
                  <dt className="text-gray-500">Paid at</dt>
                  <dd>{new Date(order.payment.paidAt).toLocaleString()}</dd>
                </div>
              )}
            </dl>
          </section>

          <section className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
            <h2 className="mb-2 font-semibold">Shipping</h2>
            <dl className="space-y-1 text-sm">
              <div className="flex justify-between">
                <dt className="text-gray-500">Status</dt>
                <dd className="font-medium">{order.shipping?.status ?? "–"}</dd>
              </div>
              {order.shipping?.carrier && (
                <div className="flex justify-between">
                  <dt className="text-gray-500">Carrier</dt>
                  <dd>{order.shipping.carrier}</dd>
                </div>
              )}
              {order.shipping?.trackingUrl && (
                <div className="flex justify-between">
                  <dt className="text-gray-500">Tracking</dt>
                  <dd>
                    <a
                      href={order.shipping.trackingUrl}
                      target="_blank"
                      rel="noreferrer"
                      className="text-indigo-600 underline"
                    >
                      Track package
                    </a>
                  </dd>
                </div>
              )}
              {order.shipping?.shippedAt && (
                <div className="flex justify-between">
                  <dt className="text-gray-500">Shipped</dt>
                  <dd>{new Date(order.shipping.shippedAt).toLocaleString()}</dd>
                </div>
              )}
              {order.shipping?.deliveredAt && (
                <div className="flex justify-between">
                  <dt className="text-gray-500">Delivered</dt>
                  <dd>{new Date(order.shipping.deliveredAt).toLocaleString()}</dd>
                </div>
              )}
            </dl>
          </section>
        </div>
      </div>
    </>
  );
}

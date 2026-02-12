import { useParams, Link, useLocation } from "react-router";
import { useOrder } from "../hooks/useOrders";
import type { OrderStatus } from "../types";

const TIMELINE_STEPS: OrderStatus[] = [
  "Created",
  "Paid",
  "Confirmed",
  "Processing",
  "ReadyForPickup",
  "Shipped",
  "Delivered",
];

function StatusTimeline({ current }: { current: OrderStatus }) {
  const currentIdx = TIMELINE_STEPS.indexOf(current);
  const isFailed = current === "Cancelled" || current === "Error";

  return (
    <div className="flex items-center gap-1 overflow-x-auto py-4">
      {TIMELINE_STEPS.map((step, idx) => {
        const reached = !isFailed && idx <= currentIdx;
        return (
          <div key={step} className="flex items-center">
            <div className="flex flex-col items-center">
              <div
                className={`flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold ${
                  reached
                    ? "bg-indigo-600 text-white"
                    : "bg-gray-200 text-gray-500"
                }`}
              >
                {idx + 1}
              </div>
              <span className="mt-1 text-[10px] text-gray-500">{step}</span>
            </div>
            {idx < TIMELINE_STEPS.length - 1 && (
              <div
                className={`mx-1 h-0.5 w-6 ${
                  !isFailed && idx < currentIdx ? "bg-indigo-600" : "bg-gray-200"
                }`}
              />
            )}
          </div>
        );
      })}

      {isFailed && (
        <div className="ml-4 flex flex-col items-center">
          <div className="flex h-8 w-8 items-center justify-center rounded-full bg-red-600 text-xs font-bold text-white">
            ?
          </div>
          <span className="mt-1 text-[10px] text-red-600">{current}</span>
        </div>
      )}
    </div>
  );
}

export default function OrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: order, isLoading, error } = useOrder(id!);
  const location = useLocation();
  const paymentUrl = (location.state as { paymentUrl?: string })?.paymentUrl;

  if (isLoading) return <p className="py-12 text-center">Loading order…</p>;
  if (error || !order) return <p className="py-12 text-center text-red-600">Order not found.</p>;

  return (
    <>
      <Link to="/orders" className="mb-4 inline-block text-sm text-indigo-600 hover:underline">
        ? Back to orders
      </Link>

      <h1 className="mb-2 text-2xl font-bold">Order Detail</h1>
      <p className="mb-6 font-mono text-xs text-gray-500">{order.id}</p>

      {paymentUrl && (
        <div className="mb-6 rounded-lg border border-blue-200 bg-blue-50 p-4">
          <p className="font-semibold text-blue-800">Order placed! Complete your payment:</p>
          <a
            href={paymentUrl}
            target="_blank"
            rel="noreferrer"
            className="mt-1 inline-block text-sm text-blue-600 underline"
          >
            {paymentUrl}
          </a>
        </div>
      )}

      {/* Timeline */}
      <StatusTimeline current={order.status} />

      <div className="mt-6 grid gap-6 lg:grid-cols-2">
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
                <dd className="font-medium">{order.payment?.status ?? "—"}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-gray-500">Amount</dt>
                <dd>${order.payment?.amount?.toFixed(2) ?? "—"}</dd>
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
                <dd className="font-medium">{order.shipping?.status ?? "—"}</dd>
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

import { Link } from "react-router";
import { useOrders } from "../hooks/useOrders";

const statusColor: Record<string, string> = {
  Created: "bg-gray-200 text-gray-800",
  Paid: "bg-blue-100 text-blue-800",
  Confirmed: "bg-blue-200 text-blue-900",
  Processing: "bg-yellow-100 text-yellow-800",
  ReadyForPickup: "bg-orange-100 text-orange-800",
  Shipped: "bg-purple-100 text-purple-800",
  Delivered: "bg-green-100 text-green-800",
  Cancelled: "bg-red-100 text-red-800",
  Error: "bg-red-200 text-red-900",
};

export default function OrderListPage() {
  const { data: orders, isLoading, error } = useOrders();

  if (isLoading) return <p className="py-12 text-center">Loading orders…</p>;
  if (error) return <p className="py-12 text-center text-red-600">Failed to load orders.</p>;

  return (
    <>
      <h1 className="mb-6 text-2xl font-bold">My Orders</h1>

      {orders?.length === 0 && (
        <p className="text-gray-500">No orders yet.</p>
      )}

      <div className="space-y-4">
        {orders?.map((order) => (
          <Link
            key={order.id}
            to={`/orders/${order.id}`}
            className="block rounded-lg border border-gray-200 bg-white p-4 shadow-sm transition hover:shadow-md"
          >
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-gray-500">Order</p>
                <p className="font-mono text-xs">{order.id}</p>
              </div>
              <span
                className={`rounded-full px-3 py-1 text-xs font-semibold ${statusColor[order.status] ?? "bg-gray-100"}`}
              >
                {order.status}
              </span>
            </div>

            <div className="mt-2 flex items-center justify-between text-sm">
              <span className="text-gray-600">
                {order.items?.length ?? 0} item{order.items?.length !== 1 ? "s" : ""}
              </span>
              <span className="font-bold text-indigo-700">
                ${order.totalPrice.toFixed(2)}
              </span>
            </div>
          </Link>
        ))}
      </div>
    </>
  );
}

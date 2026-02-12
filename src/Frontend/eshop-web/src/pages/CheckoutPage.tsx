import { useEffect } from "react";
import { Link, useNavigate } from "react-router";
import { useBasket } from "../hooks/useBasket";
import { usePlaceOrder } from "../hooks/useOrders";
import { useCustomer, useEnsureCustomer } from "../hooks/useCustomer";

export default function CheckoutPage() {
  const { data: basket, isLoading } = useBasket();
  const { data: customer, isLoading: customerLoading } = useCustomer();
  const ensureCustomer = useEnsureCustomer();
  const placeOrder = usePlaceOrder();
  const navigate = useNavigate();

  // Ensure customer exists on mount
  useEffect(() => {
    if (!customerLoading && !customer) {
      ensureCustomer.mutate();
    }
  }, [customerLoading, customer]);

  const items = basket?.items ?? [];
  const total = items.reduce((sum, i) => sum + i.product.price * i.quantity, 0);

  const handlePlaceOrder = async () => {
    const cust = customer ?? ensureCustomer.data;
    if (!cust) return;

    const result = await placeOrder.mutateAsync({
      customerId: cust.id,
      items: items.map((i) => ({
        productID: i.product.id,
        quantity: i.quantity,
        price: i.product.price,
      })),
    });
    navigate(`/orders/${result.orderId}`, { state: { paymentUrl: result.paymentUrl } });
  };

  if (isLoading || customerLoading) return <div className="flex justify-center py-12"><div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-amber-500" /></div>;

  if (items.length === 0) {
    return (
      <div className="py-12 text-center">
        <p className="text-gray-500">Your basket is empty.</p>
        <Link to="/" className="mt-4 inline-block text-blue-700 hover:underline">Browse products</Link>
      </div>
    );
  }

  const resolvedCustomer = customer ?? ensureCustomer.data;

  return (
    <>
      <h1 className="mb-6 text-2xl font-bold">Checkout</h1>

      <div className="grid gap-8 lg:grid-cols-2">
        <div>
          <h2 className="mb-4 text-lg font-semibold">Order Summary</h2>
          <div className="divide-y rounded-lg bg-white shadow-sm">
            {items.map((item) => (
              <div key={item.product.id} className="flex justify-between p-3 text-sm">
                <span>{item.product.name} x {item.quantity}</span>
                <span className="font-medium">${(item.product.price * item.quantity).toFixed(2)}</span>
              </div>
            ))}
            <div className="flex justify-between p-3 font-bold text-lg">
              <span>Total</span>
              <span className="text-red-700">${total.toFixed(2)}</span>
            </div>
          </div>
        </div>

        <div>
          <h2 className="mb-4 text-lg font-semibold">Customer</h2>
          <div className="rounded-lg bg-white p-4 shadow-sm">
            {resolvedCustomer ? (
              <div className="text-sm space-y-1 mb-4">
                <p><span className="text-gray-500">Name:</span> {resolvedCustomer.name}</p>
                <p><span className="text-gray-500">Email:</span> {resolvedCustomer.mail}</p>
              </div>
            ) : (
              <p className="text-sm text-gray-500 mb-4">Setting up your account...</p>
            )}

            <button
              onClick={handlePlaceOrder}
              disabled={placeOrder.isPending || !resolvedCustomer}
              className="w-full rounded-full bg-amber-400 py-3 font-semibold text-gray-900 hover:bg-amber-500 disabled:opacity-50"
            >
              {placeOrder.isPending ? "Placing Order..." : "Place Order"}
            </button>

            {placeOrder.isError && (
              <p className="mt-2 text-sm text-red-600">{placeOrder.error.message || "Failed to place order."}</p>
            )}
          </div>
        </div>
      </div>
    </>
  );
}


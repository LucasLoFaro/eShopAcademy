import { Link } from "react-router";
import { useBasket, useRemoveFromBasket, useAddToBasket } from "../hooks/useBasket";

export default function BasketPage() {
  const { data: basket, isLoading, error } = useBasket();
  const removeItem = useRemoveFromBasket();
  const addItem = useAddToBasket();

  if (isLoading) return <div className="flex justify-center py-12"><div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-amber-500" /></div>;
  if (error) return <p className="py-12 text-center text-red-600">Failed to load basket.</p>;

  const items = basket?.items ?? [];
  const total = items.reduce((sum, i) => sum + i.product.price * i.quantity, 0);
  const itemCount = items.reduce((sum, i) => sum + i.quantity, 0);

  return (
    <>
      <h1 className="mb-6 text-2xl font-bold">Shopping Cart</h1>

      {items.length === 0 ? (
        <div className="rounded-lg bg-white p-8 text-center shadow-sm">
          <p className="text-lg text-gray-600">Your cart is empty.</p>
          <Link to="/" className="mt-4 inline-block text-sm text-blue-700 hover:text-orange-600 hover:underline">Continue shopping</Link>
        </div>
      ) : (
        <div className="flex flex-col gap-6 lg:flex-row">
          <div className="flex-1">
            <div className="divide-y rounded-lg bg-white shadow-sm">
              {items.map((item) => (
                <div key={item.product.id} className="flex items-center gap-4 p-4 sm:px-6">
                  <div className="h-20 w-20 flex-shrink-0 rounded bg-gray-50 flex items-center justify-center">
                    <span className="text-xs text-gray-400">IMG</span>
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="font-medium text-blue-700">{item.product.name}</p>
                    <p className="text-xs text-green-700 mt-0.5">In Stock</p>
                    <p className="text-xs text-gray-500 mt-0.5">${item.product.price.toFixed(2)} each</p>
                  </div>
                  <div className="flex items-center gap-1 border rounded-lg px-1">
                    <button onClick={() => removeItem.mutate({ productID: item.product.id, quantity: 1 })} disabled={removeItem.isPending} className="px-2 py-1 text-lg hover:bg-gray-100 rounded disabled:opacity-50">-</button>
                    <span className="w-8 text-center font-medium">{item.quantity}</span>
                    <button onClick={() => addItem.mutate({ productID: item.product.id, quantity: 1 })} disabled={addItem.isPending} className="px-2 py-1 text-lg hover:bg-gray-100 rounded disabled:opacity-50">+</button>
                  </div>
                  <p className="w-20 text-right font-bold text-gray-900">${(item.product.price * item.quantity).toFixed(2)}</p>
                </div>
              ))}
            </div>
          </div>
          <div className="lg:w-72">
            <div className="rounded-lg bg-white p-6 shadow-sm">
              <h2 className="text-lg font-bold mb-2">Order Summary</h2>
              <div className="flex justify-between text-sm mb-1"><span className="text-gray-600">Items ({itemCount}):</span><span>${total.toFixed(2)}</span></div>
              <div className="flex justify-between text-sm mb-1"><span className="text-gray-600">Shipping:</span><span className="text-green-700">FREE</span></div>
              <hr className="my-3" />
              <div className="flex justify-between font-bold text-lg text-red-700"><span>Order total:</span><span>${total.toFixed(2)}</span></div>
              <Link to="/checkout" className="mt-4 block w-full rounded-full bg-amber-400 py-2.5 text-center text-sm font-semibold text-gray-900 hover:bg-amber-500">Proceed to Checkout</Link>
            </div>
          </div>
        </div>
      )}
    </>
  );
}


import { Link } from "react-router";
import { useProducts } from "../hooks/useProducts";
import { useWishlist } from "../hooks/useWishlist";
import { useAddToBasket } from "../hooks/useBasket";
import { useIsAuthenticated } from "@azure/msal-react";

export default function WishlistPage() {
  const { data: allProducts, isLoading } = useProducts();
  const { wishlistIds, toggle } = useWishlist();
  const addToBasket = useAddToBasket();
  const isAuthenticated = useIsAuthenticated();

  if (isLoading) return <div className="flex justify-center py-12"><div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-amber-500" /></div>;

  const products = (allProducts ?? []).filter((p) => wishlistIds.includes(p.id));

  return (
    <>
      <h1 className="mb-4 text-xl font-bold">Your Wishlist</h1>

      {products.length === 0 ? (
        <div className="rounded-lg bg-white p-12 text-center shadow-sm">
          <svg xmlns="http://www.w3.org/2000/svg" className="mx-auto h-16 w-16 text-gray-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
          </svg>
          <p className="mt-4 text-gray-500">Your wishlist is empty.</p>
          <Link to="/" className="mt-4 inline-block rounded-full bg-amber-400 px-6 py-2 text-sm font-medium text-gray-900 hover:bg-amber-500">Browse Products</Link>
        </div>
      ) : (
        <div className="space-y-4">
          {products.map((p) => {
            const effectivePrice = p.dealPrice ?? p.price;
            return (
              <div key={p.id} className="flex gap-4 rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
                <Link to={`/products/${p.id}`} className="flex h-28 w-28 flex-shrink-0 items-center justify-center bg-white">
                  {p.imageUrl ? (
                    <img src={p.imageUrl} alt={p.name} className="max-h-full max-w-full object-contain" />
                  ) : (
                    <span className="text-sm text-gray-400">No image</span>
                  )}
                </Link>
                <div className="flex flex-1 flex-col">
                  <Link to={`/products/${p.id}`} className="text-sm font-medium text-blue-700 hover:text-orange-600 hover:underline line-clamp-2">
                    {p.name}
                  </Link>
                  {p.category && <p className="mt-0.5 text-xs text-gray-500">{p.category.name}</p>}
                  {p.rating != null && p.rating > 0 && (
                    <div className="mt-1 flex items-center gap-1">
                      <span className="text-xs text-amber-500">{"?".repeat(Math.round(p.rating))}{"?".repeat(5 - Math.round(p.rating))}</span>
                      <span className="text-xs text-gray-500">({p.reviewCount?.toLocaleString()})</span>
                    </div>
                  )}
                  <div className="mt-1">
                    {p.isDeal && p.dealPrice != null && (
                      <span className="text-xs text-gray-500 line-through mr-1">${p.price.toFixed(2)}</span>
                    )}
                    <span className="text-lg font-bold text-gray-900">
                      <span className="text-xs align-top">$</span>{Math.floor(effectivePrice)}
                      <span className="text-xs align-top">{(effectivePrice % 1).toFixed(2).slice(1)}</span>
                    </span>
                    {p.isDeal && <span className="ml-1 text-xs text-red-600 font-semibold">Deal</span>}
                  </div>
                  <p className="text-xs text-green-700">In Stock · FREE delivery</p>
                </div>
                <div className="flex flex-col items-end gap-2">
                  {isAuthenticated && (
                    <button
                      onClick={() => addToBasket.mutate({ productID: p.id, quantity: 1 })}
                      className="rounded-full bg-amber-400 px-4 py-1.5 text-sm font-medium text-gray-900 hover:bg-amber-500"
                    >
                      Add to Cart
                    </button>
                  )}
                  <button
                    onClick={() => toggle(p.id)}
                    className="text-xs text-red-500 hover:text-red-700 hover:underline"
                  >
                    Remove
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </>
  );
}

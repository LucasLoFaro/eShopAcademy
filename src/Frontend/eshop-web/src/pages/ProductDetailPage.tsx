import { useState } from "react";
import { useParams, Link } from "react-router";
import { useProduct } from "../hooks/useProducts";
import { useAddToBasket } from "../hooks/useBasket";
import { useWishlist } from "../hooks/useWishlist";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { handleLogin } from "../auth/authHelpers";

function Stars({ rating }: { rating: number }) {
  return (
    <span className="inline-flex items-center gap-1 text-amber-500">
      {Array.from({ length: 5 }, (_, i) => (
        <svg key={i} xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill={i < Math.round(rating) ? "currentColor" : "none"} stroke="currentColor" strokeWidth={1}>
          <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
        </svg>
      ))}
      <span className="text-sm text-gray-500 ml-1">{rating.toFixed(1)}</span>
    </span>
  );
}

export default function ProductDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: product, isLoading, error } = useProduct(id!);
  const addToBasket = useAddToBasket();
  const { toggle, isFavorite } = useWishlist();
  const isAuthenticated = useIsAuthenticated();
  const { instance } = useMsal();
  const [selectedImg, setSelectedImg] = useState(0);
  const [openFaq, setOpenFaq] = useState<number | null>(null);

  if (isLoading) return <div className="flex justify-center py-12"><div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-amber-500" /></div>;
  if (error || !product) return <p className="py-12 text-center text-red-600">Product not found.</p>;

  const allImages = [product.imageUrl, ...(product.additionalImages ?? [])].filter(Boolean);
  const effectivePrice = product.dealPrice ?? product.price;

  return (
    <>
      <Link to="/" className="mb-4 inline-block text-sm text-blue-700 hover:text-orange-600 hover:underline">&larr; Back to results</Link>

      {/* Main product section */}
      <div className="rounded-lg bg-white p-6 shadow-sm">
        <div className="flex flex-col gap-8 lg:flex-row">
          {/* Image gallery */}
          <div className="lg:w-1/2">
            <div className="flex items-center justify-center mb-3 bg-white rounded-lg" style={{ minHeight: 320 }}>
              <img src={allImages[selectedImg] || allImages[0]} alt={product.name} className="max-h-80 max-w-full object-contain" />
            </div>
            {allImages.length > 1 && (
              <div className="flex gap-2 overflow-x-auto">
                {allImages.map((img, i) => (
                  <button key={i} onClick={() => setSelectedImg(i)} className={`flex-shrink-0 rounded border-2 p-1 ${i === selectedImg ? "border-amber-400" : "border-gray-200"}`}>
                    <img src={img} alt="" className="h-14 w-14 object-contain" />
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Product info */}
          <div className="flex-1 lg:w-1/3">
            <h1 className="text-2xl font-medium text-gray-900">{product.name}</h1>
            {product.category && <p className="mt-1 text-sm text-blue-700">in {product.category.name}</p>}

            {product.rating != null && product.rating > 0 && (
              <div className="mt-2 flex items-center gap-2">
                <Stars rating={product.rating} />
                <span className="text-sm text-blue-700">{product.reviewCount?.toLocaleString()} ratings</span>
              </div>
            )}
            {product.isBestSeller && <span className="mt-2 inline-block rounded bg-orange-100 px-2 py-0.5 text-xs font-bold text-orange-800">#1 Best Seller</span>}

            <hr className="my-3" />

            {product.isDeal && product.dealPrice != null && (
              <div className="mb-1">
                <span className="rounded bg-red-600 px-2 py-0.5 text-xs font-bold text-white mr-2">Deal</span>
                <span className="text-sm text-gray-500 line-through">${product.price.toFixed(2)}</span>
              </div>
            )}

            <p className="text-3xl font-bold text-gray-900">
              <span className="text-sm align-top">$</span>{Math.floor(effectivePrice)}
              <span className="text-sm align-top">{(effectivePrice % 1).toFixed(2).slice(1)}</span>
            </p>
            <p className="mt-1 text-sm text-green-700">FREE delivery</p>
            <p className="mt-1 text-sm text-green-700 font-bold">In Stock</p>

            <hr className="my-3" />
            <p className="text-sm text-gray-700 leading-relaxed">{product.description || "No description available."}</p>
          </div>

          {/* Buy box */}
          <div className="lg:w-52">
            <div className="rounded-lg border border-gray-200 p-4 sticky top-32">
              <p className="text-2xl font-bold text-gray-900 mb-1">
                <span className="text-sm align-top">$</span>{Math.floor(effectivePrice)}
                <span className="text-sm align-top">{(effectivePrice % 1).toFixed(2).slice(1)}</span>
              </p>
              <p className="text-sm text-green-700 mb-3">FREE delivery</p>
              <p className="text-sm text-green-700 font-bold mb-4">In Stock</p>
              {isAuthenticated ? (
                <>
                  <button onClick={() => addToBasket.mutate({ productID: product.id, quantity: 1 })} disabled={addToBasket.isPending} className="w-full rounded-full bg-amber-400 py-2 text-sm font-medium text-gray-900 hover:bg-amber-500 disabled:opacity-50">
                    {addToBasket.isPending ? "Adding..." : "Add to Cart"}
                  </button>
                  {addToBasket.isSuccess && <p className="mt-2 text-sm text-green-600 text-center">Added!</p>}
                  <button
                    onClick={() => toggle(product.id)}
                    className="mt-2 flex w-full items-center justify-center gap-2 rounded-full border border-gray-300 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 24 24" fill={isFavorite(product.id) ? "#ef4444" : "none"} stroke={isFavorite(product.id) ? "#ef4444" : "currentColor"} strokeWidth={2}>
                      <path strokeLinecap="round" strokeLinejoin="round" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                    </svg>
                    {isFavorite(product.id) ? "In Wishlist" : "Add to Wishlist"}
                  </button>
                </>
              ) : (
                <button onClick={() => handleLogin(instance)} className="w-full rounded-full border border-gray-300 py-2 text-sm font-medium text-blue-700 hover:bg-gray-50">Sign in to buy</button>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* About this item */}
      {product.aboutHtml && (
        <div className="mt-6 rounded-lg bg-white p-6 shadow-sm">
          <h2 className="text-lg font-bold mb-3">About this item</h2>
          <div className="text-sm text-gray-700 leading-relaxed prose prose-sm max-w-none" dangerouslySetInnerHTML={{ __html: product.aboutHtml }} />
        </div>
      )}

      {/* Technical specs */}
      {product.specs && product.specs.length > 0 && (
        <div className="mt-6 rounded-lg bg-white p-6 shadow-sm">
          <h2 className="text-lg font-bold mb-3">Technical Specifications</h2>
          <table className="w-full text-sm">
            <tbody>
              {product.specs.map((spec, i) => (
                <tr key={i} className={i % 2 === 0 ? "bg-gray-50" : ""}>
                  <td className="py-2 px-4 font-medium text-gray-600 w-1/3">{spec.label}</td>
                  <td className="py-2 px-4 text-gray-900">{spec.value}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* FAQ */}
      {product.faqs && product.faqs.length > 0 && (
        <div className="mt-6 rounded-lg bg-white p-6 shadow-sm">
          <h2 className="text-lg font-bold mb-3">Frequently Asked Questions</h2>
          <div className="divide-y">
            {product.faqs.map((faq, i) => (
              <div key={i}>
                <button onClick={() => setOpenFaq(openFaq === i ? null : i)} className="flex w-full items-center justify-between py-3 text-left text-sm font-medium text-gray-900 hover:text-blue-700">
                  <span>Q: {faq.question}</span>
                  <svg xmlns="http://www.w3.org/2000/svg" className={`h-4 w-4 flex-shrink-0 transition-transform ${openFaq === i ? "rotate-180" : ""}`} fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}><path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" /></svg>
                </button>
                {openFaq === i && <p className="pb-3 text-sm text-gray-600 pl-4">A: {faq.answer}</p>}
              </div>
            ))}
          </div>
        </div>
      )}
    </>
  );
}

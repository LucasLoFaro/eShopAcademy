import { useState, useEffect } from "react";
import { Link, useSearchParams } from "react-router";
import { useProductSearch } from "../hooks/useProducts";
import { useAddToBasket } from "../hooks/useBasket";
import { useWishlist } from "../hooks/useWishlist";
import { useIsAuthenticated } from "@azure/msal-react";
import SearchFilterSidebar from "../components/SearchFilterSidebar";
import type { ProductSearchFilter } from "../types";

const PAGE_SIZE = 12;

const titleMap: Record<string, string> = {
  "best-sellers": "Best Sellers",
  "new": "New Releases",
  "rating": "Top Rated",
  "price-asc": "Price: Low to High",
  "price-desc": "Price: High to Low",
};

export default function ProductListPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  const buildFilterFromParams = (): ProductSearchFilter => ({
    searchText: searchParams.get("searchText") ?? undefined,
    category: searchParams.get("cat") ?? undefined,
    deals: searchParams.get("deals") === "true" || undefined,
    sort: searchParams.get("sort") ?? undefined,
    minPrice: searchParams.get("minPrice") ? Number(searchParams.get("minPrice")) : undefined,
    maxPrice: searchParams.get("maxPrice") ? Number(searchParams.get("maxPrice")) : undefined,
    minRating: searchParams.get("minRating") ? Number(searchParams.get("minRating")) : undefined,
    page: 1,
    pageSize: PAGE_SIZE,
  });

  const [filter, setFilter] = useState<ProductSearchFilter>(buildFilterFromParams);
  const [page, setPage] = useState(1);

  // Sync from URL → filter state when URL changes externally (nav links, header search)
  useEffect(() => {
    const fromUrl = buildFilterFromParams();
    setFilter(fromUrl);
    setPage(1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams.toString()]);

  const activeFilter = { ...filter, page, pageSize: PAGE_SIZE };

  const { data: searchResult, isLoading, error } = useProductSearch(activeFilter);

  const addToBasket = useAddToBasket();
  const { toggle, isFavorite } = useWishlist();
  const isAuthenticated = useIsAuthenticated();

  const handleFilterChange = (patch: Partial<ProductSearchFilter>) => {
    const next = { ...filter, ...patch };
    setFilter(next);
    setPage(1);

    // Sync key filters to URL so the browser back button works
    const params = new URLSearchParams();
    if (next.searchText) params.set("searchText", next.searchText);
    if (next.category) params.set("cat", next.category);
    if (next.deals) params.set("deals", "true");
    if (next.sort) params.set("sort", next.sort);
    if (next.minPrice != null) params.set("minPrice", String(next.minPrice));
    if (next.maxPrice != null) params.set("maxPrice", String(next.maxPrice));
    if (next.minRating != null) params.set("minRating", String(next.minRating));
    setSearchParams(params, { replace: true });
  };

  const handleClearFilters = () => {
    const cleared: ProductSearchFilter = { searchText: filter.searchText, page: 1, pageSize: PAGE_SIZE };
    setFilter(cleared);
    setPage(1);
    const params = new URLSearchParams();
    if (cleared.searchText) params.set("searchText", cleared.searchText);
    setSearchParams(params, { replace: true });
  };

  if (isLoading) return <div className="flex justify-center py-12"><div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-amber-500" /></div>;
  if (error) return <p className="text-center py-12 text-red-600">Failed to load products.</p>;

  const items = searchResult?.items ?? [];
  const totalCount = searchResult?.totalCount ?? 0;
  const totalPages = searchResult?.totalPages ?? 1;

  const title = filter.searchText
    ? `Search: "${filter.searchText}"`
    : filter.deals
      ? "Today's Deals"
      : filter.sort && titleMap[filter.sort]
        ? titleMap[filter.sort]
        : filter.category
          ? `Category: ${filter.category}`
          : "Results";

  return (
    <div className="flex gap-6">
      <SearchFilterSidebar
        filter={filter}
        onChange={handleFilterChange}
        onClear={handleClearFilters}
        totalCount={totalCount}
      />

      <div className="flex-1 min-w-0">
        <div className="mb-4 flex items-center justify-between">
          <div>
            <h1 className="text-xl font-bold">{title}</h1>
            <p className="text-sm text-gray-500">{totalCount} products</p>
          </div>
        </div>

        {items.length === 0 && <p className="text-gray-500">No products match your filters.</p>}

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {items.map((p) => (
            <div key={p.id} className="group relative flex flex-col rounded-lg border border-gray-200 bg-white shadow-sm transition hover:shadow-lg">
              {isAuthenticated && (
                <button
                  onClick={() => toggle(p.id)}
                  className="absolute right-2 top-2 z-10 rounded-full bg-white/80 p-1.5 shadow-sm hover:bg-white transition"
                  title={isFavorite(p.id) ? "Remove from Wishlist" : "Add to Wishlist"}
                >
                  <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 24 24" fill={isFavorite(p.id) ? "#ef4444" : "none"} stroke={isFavorite(p.id) ? "#ef4444" : "#9ca3af"} strokeWidth={2}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                  </svg>
                </button>
              )}
              <Link to={`/products/${p.id}`}>
                {p.imageUrl ? (
                  <div className="flex h-48 items-center justify-center bg-white p-4">
                    <img src={p.imageUrl} alt={p.name} className="max-h-full max-w-full object-contain" />
                  </div>
                ) : (
                  <div className="flex h-48 items-center justify-center bg-gray-50 text-gray-400">No image</div>
                )}
              </Link>
              <div className="flex flex-1 flex-col p-4">
                <Link to={`/products/${p.id}`} className="text-sm font-medium text-blue-700 hover:text-orange-600 hover:underline line-clamp-2">
                  {p.name}
                </Link>
                {p.category && <p className="mt-1 text-xs text-gray-500">{p.category.name}</p>}
                {p.rating != null && p.rating > 0 && (
                  <div className="mt-1 flex items-center gap-1">
                    <span className="text-xs text-amber-500">{"★".repeat(Math.round(p.rating))}{"☆".repeat(5 - Math.round(p.rating))}</span>
                    <span className="text-xs text-gray-500">({p.reviewCount?.toLocaleString()})</span>
                  </div>
                )}
                {p.isBestSeller && <span className="mt-1 inline-block rounded bg-orange-100 px-1.5 py-0.5 text-[10px] font-bold text-orange-800 w-fit">Best Seller</span>}
                <div className="mt-2">
                  {p.isDeal && p.dealPrice != null && (
                    <span className="text-xs text-gray-500 line-through mr-1">${p.price.toFixed(2)}</span>
                  )}
                  <span className="text-lg font-bold text-gray-900">
                    <span className="text-xs align-top">$</span>{Math.floor(p.dealPrice ?? p.price)}
                    <span className="text-xs align-top">{((p.dealPrice ?? p.price) % 1).toFixed(2).slice(1)}</span>
                  </span>
                  {p.isDeal && <span className="ml-1 text-xs text-red-600 font-semibold">Deal</span>}
                </div>
                <p className="mt-1 text-xs text-green-700">FREE delivery</p>
                {isAuthenticated && (
                  <button
                    onClick={() => addToBasket.mutate({ productID: p.id, quantity: 1 })}
                    className="mt-auto pt-3 w-full rounded-full bg-amber-400 py-1.5 text-sm font-medium text-gray-900 hover:bg-amber-500"
                  >
                    Add to Cart
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>

        {totalPages > 1 && (
          <div className="mt-8 flex items-center justify-center gap-2">
            <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page === 1} className="rounded border px-3 py-1 text-sm disabled:opacity-30 hover:bg-gray-100">Previous</button>
            {Array.from({ length: totalPages }, (_, i) => (
              <button key={i + 1} onClick={() => setPage(i + 1)} className={`rounded border px-3 py-1 text-sm ${page === i + 1 ? "border-amber-500 bg-amber-50 font-bold" : "hover:bg-gray-100"}`}>
                {i + 1}
              </button>
            ))}
            <button onClick={() => setPage((p) => Math.min(totalPages, p + 1))} disabled={page === totalPages} className="rounded border px-3 py-1 text-sm disabled:opacity-30 hover:bg-gray-100">Next</button>
          </div>
        )}
      </div>
    </div>
  );
}

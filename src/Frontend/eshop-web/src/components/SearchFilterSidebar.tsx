import type { ProductSearchFilter } from "../types";

const categories = [
  "Peripherals",
  "Audio",
  "Accessories",
  "Monitors",
  "Storage",
  "Networking",
  "Gaming",
  "Laptops",
];

const priceRanges = [
  { label: "Under $25", min: undefined, max: 25 },
  { label: "$25 to $50", min: 25, max: 50 },
  { label: "$50 to $100", min: 50, max: 100 },
  { label: "$100 to $200", min: 100, max: 200 },
  { label: "$200 & Above", min: 200, max: undefined },
];

const sortOptions = [
  { label: "Newest Arrivals", value: "new" },
  { label: "Price: Low to High", value: "price-asc" },
  { label: "Price: High to Low", value: "price-desc" },
  { label: "Avg. Customer Review", value: "rating" },
  { label: "Best Sellers", value: "best-sellers" },
];

const ratingOptions = [4, 3, 2, 1];

interface Props {
  filter: ProductSearchFilter;
  onChange: (patch: Partial<ProductSearchFilter>) => void;
  onClear: () => void;
  totalCount: number;
}

export default function SearchFilterSidebar({ filter, onChange, onClear, totalCount }: Props) {
  const activePriceIdx = priceRanges.findIndex(
    (r) => r.min === filter.minPrice && r.max === filter.maxPrice
  );

  const hasFilters =
    !!filter.category || filter.minPrice != null || filter.maxPrice != null ||
    filter.deals === true || !!filter.sort || !!filter.minRating;

  return (
    <aside className="w-56 shrink-0 space-y-5 text-sm">
      {hasFilters && (
        <button
          onClick={onClear}
          className="text-xs text-blue-700 hover:text-orange-600 hover:underline"
        >
          ✕ Clear all filters
        </button>
      )}

      {/* Category */}
      <section>
        <h3 className="mb-2 font-bold text-gray-900">Department</h3>
        <ul className="space-y-1">
          {categories.map((c) => (
            <li key={c}>
              <button
                onClick={() => onChange({ category: filter.category === c ? undefined : c })}
                className={`w-full text-left hover:text-orange-600 ${filter.category === c ? "font-bold text-orange-700" : "text-gray-700"}`}
              >
                {filter.category === c && <span className="mr-1">‹</span>}
                {c}
              </button>
            </li>
          ))}
        </ul>
      </section>

      {/* Rating */}
      <section>
        <h3 className="mb-2 font-bold text-gray-900">Customer Review</h3>
        <ul className="space-y-1">
          {ratingOptions.map((r) => (
            <li key={r}>
              <button
                onClick={() => onChange({ minRating: filter.minRating === r ? undefined : r })}
                className={`flex items-center gap-1 hover:text-orange-600 ${filter.minRating === r ? "font-bold" : ""}`}
              >
                <span className="text-amber-500">
                  {"★".repeat(r)}{"☆".repeat(5 - r)}
                </span>
                <span className="text-xs text-gray-600">& Up</span>
              </button>
            </li>
          ))}
        </ul>
      </section>

      {/* Price */}
      <section>
        <h3 className="mb-2 font-bold text-gray-900">Price</h3>
        <ul className="space-y-1">
          {priceRanges.map((r, i) => (
            <li key={i}>
              <button
                onClick={() =>
                  onChange(
                    activePriceIdx === i
                      ? { minPrice: undefined, maxPrice: undefined }
                      : { minPrice: r.min, maxPrice: r.max }
                  )
                }
                className={`w-full text-left hover:text-orange-600 ${activePriceIdx === i ? "font-bold text-orange-700" : "text-gray-700"}`}
              >
                {r.label}
              </button>
            </li>
          ))}
        </ul>
        <div className="mt-2 flex items-center gap-1">
          <input
            type="number"
            placeholder="Min"
            value={filter.minPrice ?? ""}
            onChange={(e) => onChange({ minPrice: e.target.value ? Number(e.target.value) : undefined })}
            className="w-16 rounded border px-2 py-1 text-xs"
          />
          <span className="text-gray-400">–</span>
          <input
            type="number"
            placeholder="Max"
            value={filter.maxPrice ?? ""}
            onChange={(e) => onChange({ maxPrice: e.target.value ? Number(e.target.value) : undefined })}
            className="w-16 rounded border px-2 py-1 text-xs"
          />
          <button
            onClick={() => onChange({})}
            className="rounded bg-gray-100 px-2 py-1 text-xs hover:bg-gray-200"
          >
            Go
          </button>
        </div>
      </section>

      {/* Deals */}
      <section>
        <h3 className="mb-2 font-bold text-gray-900">Deals & Discounts</h3>
        <label className="flex items-center gap-2 cursor-pointer text-gray-700">
          <input
            type="checkbox"
            checked={filter.deals === true}
            onChange={(e) => onChange({ deals: e.target.checked || undefined })}
            className="accent-amber-500"
          />
          Today's Deals
        </label>
      </section>

      {/* Sort */}
      <section>
        <h3 className="mb-2 font-bold text-gray-900">Sort by</h3>
        <ul className="space-y-1">
          {sortOptions.map((s) => (
            <li key={s.value}>
              <button
                onClick={() => onChange({ sort: filter.sort === s.value ? undefined : s.value })}
                className={`w-full text-left hover:text-orange-600 ${filter.sort === s.value ? "font-bold text-orange-700" : "text-gray-700"}`}
              >
                {s.label}
              </button>
            </li>
          ))}
        </ul>
      </section>

      <p className="pt-2 text-xs text-gray-400">{totalCount} results</p>
    </aside>
  );
}

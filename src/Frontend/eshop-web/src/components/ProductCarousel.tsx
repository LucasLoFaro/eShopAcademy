import { useRef } from "react";
import { Link } from "react-router";
import type { Product } from "../types";

interface Props {
  title: string;
  products: Product[];
  linkTo?: string;
  linkLabel?: string;
}

export default function ProductCarousel({ title, products, linkTo, linkLabel }: Props) {
  const scrollRef = useRef<HTMLDivElement>(null);

  const scroll = (dir: "left" | "right") => {
    if (!scrollRef.current) return;
    const amount = scrollRef.current.clientWidth * 0.8;
    scrollRef.current.scrollBy({ left: dir === "left" ? -amount : amount, behavior: "smooth" });
  };

  if (products.length === 0) return null;

  return (
    <section className="bg-white rounded-lg shadow-sm p-5">
      <div className="flex items-center justify-between mb-3">
        <h2 className="text-lg font-bold text-gray-900">{title}</h2>
        {linkTo && (
          <Link to={linkTo} className="text-sm text-blue-700 hover:text-orange-600 hover:underline">
            {linkLabel ?? "See more"}
          </Link>
        )}
      </div>

      <div className="relative group">
        <button
          onClick={() => scroll("left")}
          className="absolute left-0 top-1/2 -translate-y-1/2 z-10 hidden group-hover:flex h-20 w-10 items-center justify-center bg-white/90 border border-gray-200 rounded-r-md shadow hover:bg-gray-50"
          aria-label="Scroll left"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5 text-gray-700" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}><path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" /></svg>
        </button>

        <div ref={scrollRef} className="flex gap-4 overflow-x-auto scroll-smooth scrollbar-hide pb-1">
          {products.map((p) => (
            <Link
              key={p.id}
              to={`/products/${p.id}`}
              className="flex-shrink-0 w-44 group/card"
            >
              <div className="h-44 flex items-center justify-center bg-gray-50 rounded-md p-2 mb-2">
                {p.imageUrl ? (
                  <img src={p.imageUrl} alt={p.name} className="max-h-full max-w-full object-contain group-hover/card:scale-105 transition" />
                ) : (
                  <span className="text-gray-300 text-xs">No image</span>
                )}
              </div>
              <p className="text-sm text-blue-700 group-hover/card:text-orange-600 line-clamp-2 leading-tight">{p.name}</p>
              {p.rating != null && p.rating > 0 && (
                <div className="flex items-center gap-1 mt-0.5">
                  <span className="text-xs text-amber-500">{"★".repeat(Math.round(p.rating))}{"☆".repeat(5 - Math.round(p.rating))}</span>
                  <span className="text-xs text-gray-400">({p.reviewCount?.toLocaleString()})</span>
                </div>
              )}
              <div className="mt-0.5">
                {p.isDeal && p.dealPrice != null && (
                  <span className="text-xs text-gray-400 line-through mr-1">${p.price.toFixed(2)}</span>
                )}
                <span className="text-sm font-bold text-gray-900">${(p.dealPrice ?? p.price).toFixed(2)}</span>
                {p.isDeal && <span className="ml-1 text-xs text-red-600 font-semibold">Deal</span>}
              </div>
            </Link>
          ))}
        </div>

        <button
          onClick={() => scroll("right")}
          className="absolute right-0 top-1/2 -translate-y-1/2 z-10 hidden group-hover:flex h-20 w-10 items-center justify-center bg-white/90 border border-gray-200 rounded-l-md shadow hover:bg-gray-50"
          aria-label="Scroll right"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5 text-gray-700" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}><path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" /></svg>
        </button>
      </div>
    </section>
  );
}

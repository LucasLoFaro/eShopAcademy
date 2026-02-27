import { useState, useEffect, useCallback } from "react";
import { Link } from "react-router";
import { useProducts } from "../hooks/useProducts";
import ProductCarousel from "../components/ProductCarousel";

const heroSlides = [
  {
    gradient: "from-gray-900 via-gray-800 to-gray-900",
    image: "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=1600&h=500&fit=crop&auto=format&q=60",
    tag: "eShop Academy",
    title: <>Tech you love.<br /><span className="text-amber-400">Prices you'll love more.</span></>,
    subtitle: "Shop the latest in electronics, peripherals, and accessories with free delivery and exclusive deals every day.",
    cta: { label: "Shop Today's Deals", to: "/search?deals=true" },
    ctaSecondary: { label: "Browse All Products", to: "/search" },
    accent: "amber",
  },
  {
    gradient: "from-emerald-900 via-emerald-800 to-teal-900",
    image: "https://images.unsplash.com/photo-1593640408182-31c70c8268f5?w=1600&h=500&fit=crop&auto=format&q=60",
    tag: "Limited Time Offer",
    title: <>Free Shipping<br /><span className="text-emerald-300">on orders $50+</span></>,
    subtitle: "All qualifying orders over $50 ship free — no promo code needed. Stock up on your favorite tech essentials.",
    cta: { label: "Start Shopping", to: "/search" },
    ctaSecondary: { label: "See What's New", to: "/search?sort=new" },
    accent: "emerald",
  },
  {
    gradient: "from-violet-900 via-purple-900 to-indigo-900",
    image: "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=1600&h=500&fit=crop&auto=format&q=60",
    tag: "Back to School",
    title: <>Back-to-School<br /><span className="text-violet-300">Essentials</span></>,
    subtitle: "Gear up for the new semester with laptops, peripherals, and study-ready accessories at unbeatable prices.",
    cta: { label: "Shop Laptops", to: "/search?cat=Laptops" },
    ctaSecondary: { label: "See All Accessories", to: "/search?cat=Accessories" },
    accent: "violet",
  },
  {
    gradient: "from-rose-900 via-pink-900 to-red-900",
    image: "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=1600&h=500&fit=crop&auto=format&q=60",
    tag: "Gift Guide",
    title: <>Perfect Gift Ideas<br /><span className="text-rose-300">for Everyone</span></>,
    subtitle: "Surprise them with premium audio, sleek accessories, and cutting-edge gadgets they'll actually use.",
    cta: { label: "Shop Audio", to: "/search?cat=Audio" },
    ctaSecondary: { label: "Browse Gift Ideas", to: "/search?sort=best-sellers" },
    accent: "rose",
  },
  {
    gradient: "from-orange-900 via-amber-900 to-yellow-900",
    image: "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=1600&h=500&fit=crop&auto=format&q=60",
    tag: "Bundle & Save",
    title: <>Buy 3, Get<br /><span className="text-amber-300">20% Off</span></>,
    subtitle: "Mix and match across peripherals, storage, and networking — the more you add, the more you save.",
    cta: { label: "Shop Peripherals", to: "/search?cat=Peripherals" },
    ctaSecondary: { label: "View All Deals", to: "/search?deals=true" },
    accent: "amber",
  },
];

const ctaColors: Record<string, { primary: string; secondary: string }> = {
  amber:   { primary: "bg-amber-400 text-gray-900 hover:bg-amber-500",   secondary: "border-white/30 hover:bg-white/10" },
  emerald: { primary: "bg-emerald-400 text-gray-900 hover:bg-emerald-500", secondary: "border-white/30 hover:bg-white/10" },
  violet:  { primary: "bg-violet-400 text-gray-900 hover:bg-violet-500",  secondary: "border-white/30 hover:bg-white/10" },
  rose:    { primary: "bg-rose-400 text-gray-900 hover:bg-rose-500",    secondary: "border-white/30 hover:bg-white/10" },
};

const categoryCards = [
  { cat: "Peripherals", title: "Type like a pro",         image: "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=300&h=300&fit=crop&auto=format&q=80" },
  { cat: "Audio",       title: "Sound that moves you",    image: "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=300&h=300&fit=crop&auto=format&q=80" },
  { cat: "Monitors",    title: "See the bigger picture",  image: "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=300&h=300&fit=crop&auto=format&q=80" },
  { cat: "Gaming",      title: "Get your game on",        image: "https://images.unsplash.com/photo-1593640408182-31c70c8268f5?w=300&h=300&fit=crop&auto=format&q=80" },
  { cat: "Laptops",     title: "Power on the go",         image: "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=300&h=300&fit=crop&auto=format&q=80" },
  { cat: "Storage",     title: "Never run out of space",  image: "https://images.unsplash.com/photo-1597754742025-5bb5de4bfb6b?w=300&h=300&fit=crop&auto=format&q=80" },
  { cat: "Accessories", title: "Must-have add-ons",       image: "https://images.unsplash.com/photo-1558171813-d13d0c5e7b49?w=300&h=300&fit=crop&auto=format&q=80" },
  { cat: "Networking",  title: "Stay connected",          image: "https://images.unsplash.com/photo-1562408590-e32931084e23?w=300&h=300&fit=crop&auto=format&q=80" },
];

// Amazon-style themed 4-card collection grids
const themedCollections = [
  {
    title: "Level up your setup",
    items: [
      { label: "Keyboards", cat: "Peripherals", image: "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=200&h=200&fit=crop&auto=format&q=80" },
      { label: "Mice", cat: "Peripherals", image: "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=200&h=200&fit=crop&auto=format&q=80" },
      { label: "Monitors", cat: "Monitors", image: "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=200&h=200&fit=crop&auto=format&q=80" },
      { label: "Audio", cat: "Audio", image: "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=200&h=200&fit=crop&auto=format&q=80" },
    ],
    link: "/search?cat=Peripherals",
  },
  {
    title: "Upgrade your tech",
    items: [
      { label: "Laptops", cat: "Laptops", image: "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=200&h=200&fit=crop&auto=format&q=80" },
      { label: "Storage", cat: "Storage", image: "https://images.unsplash.com/photo-1597754742025-5bb5de4bfb6b?w=200&h=200&fit=crop&auto=format&q=80" },
      { label: "Networking", cat: "Networking", image: "https://images.unsplash.com/photo-1562408590-e32931084e23?w=200&h=200&fit=crop&auto=format&q=80" },
      { label: "Accessories", cat: "Accessories", image: "https://images.unsplash.com/photo-1558171813-d13d0c5e7b49?w=200&h=200&fit=crop&auto=format&q=80" },
    ],
    link: "/search?cat=Laptops",
  },
  {
    title: "Top gaming picks",
    items: [
      { label: "Gaming gear", cat: "Gaming", image: "https://images.unsplash.com/photo-1593640408182-31c70c8268f5?w=200&h=200&fit=crop&auto=format&q=80" },
      { label: "Headsets", cat: "Audio", image: "https://images.unsplash.com/photo-1599669851046-5fce00fd3f3d?w=200&h=200&fit=crop&auto=format&q=80" },
      { label: "Keyboards", cat: "Peripherals", image: "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=200&h=200&fit=crop&auto=format&q=80" },
      { label: "Monitors", cat: "Monitors", image: "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=200&h=200&fit=crop&auto=format&q=80" },
    ],
    link: "/search?cat=Gaming",
  },
  {
    title: "Work from home essentials",
    items: [
      { label: "Webcams", cat: "Accessories", image: "https://images.unsplash.com/photo-1558171813-d13d0c5e7b49?w=200&h=200&fit=crop&auto=format&q=80" },
      { label: "Headphones", cat: "Audio", image: "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=200&h=200&fit=crop&auto=format&q=80" },
      { label: "Wi-Fi routers", cat: "Networking", image: "https://images.unsplash.com/photo-1562408590-e32931084e23?w=200&h=200&fit=crop&auto=format&q=80" },
      { label: "Monitors", cat: "Monitors", image: "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=200&h=200&fit=crop&auto=format&q=80" },
    ],
    link: "/search?cat=Accessories",
  },
];

export default function HomePage() {
  const { data: allProducts, isLoading } = useProducts();

  const products = allProducts ?? [];

  const deals = products.filter((p) => p.isDeal);
  const bestSellers = products.filter((p) => p.isBestSeller);
  const newReleases = products.filter((p) => p.isNewRelease);
  const topRated = [...products].sort((a, b) => (b.rating ?? 0) - (a.rating ?? 0)).slice(0, 12);
  const under50 = products.filter((p) => (p.dealPrice ?? p.price) < 50);

  return (
    <div className="-mt-6 -mx-4">
      {/* Hero carousel */}
      <HeroCarousel />

      {/* Category cards — overlapping the hero */}
      <div className="relative z-10 -mt-24 px-4 mb-5">
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          {categoryCards.map((card) => (
            <Link
              key={card.cat}
              to={`/search?cat=${encodeURIComponent(card.cat)}`}
              className="bg-white rounded-lg shadow-sm p-4 hover:shadow-md transition group"
            >
              <h3 className="font-bold text-gray-900 text-sm">{card.title}</h3>
              <div className="h-40 flex items-center justify-center overflow-hidden rounded-md my-2">
                <img src={card.image} alt={card.cat} className="max-h-full max-w-full object-contain group-hover:scale-105 transition" />
              </div>
              <p className="text-xs text-blue-700 group-hover:text-orange-600">Shop {card.cat}</p>
            </Link>
          ))}
        </div>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-12 px-4">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-amber-500" />
        </div>
      ) : (
        <div className="space-y-5 px-4">
          {/* Deals banner */}
          {deals.length > 0 && (
            <div className="bg-gradient-to-r from-red-600 to-orange-500 rounded-lg p-5 text-white">
              <div className="flex items-center justify-between mb-3">
                <div>
                  <h2 className="text-xl font-extrabold">🔥 Today's Deals</h2>
                  <p className="text-white/80 text-sm">Limited time savings on top products</p>
                </div>
                <Link to="/search?deals=true" className="text-sm font-semibold bg-white/20 rounded-full px-4 py-1.5 hover:bg-white/30 transition">
                  See all deals
                </Link>
              </div>
              <div className="flex gap-4 overflow-x-auto scroll-smooth pb-1">
                {deals.slice(0, 8).map((p) => (
                  <Link key={p.id} to={`/products/${p.id}`} className="flex-shrink-0 w-40 bg-white rounded-lg p-3 text-gray-900 hover:shadow-lg transition">
                    <div className="h-32 flex items-center justify-center mb-2">
                      {p.imageUrl && <img src={p.imageUrl} alt={p.name} className="max-h-full max-w-full object-contain" />}
                    </div>
                    <p className="text-xs line-clamp-2 mb-1">{p.name}</p>
                    <div className="flex items-baseline gap-1">
                      <span className="text-red-600 font-bold text-sm">${(p.dealPrice ?? p.price).toFixed(2)}</span>
                      {p.dealPrice != null && <span className="text-xs text-gray-400 line-through">${p.price.toFixed(2)}</span>}
                    </div>
                    <span className="inline-block mt-1 bg-red-100 text-red-700 text-[10px] font-bold px-1.5 py-0.5 rounded">
                      {p.dealPrice != null ? `${Math.round((1 - p.dealPrice / p.price) * 100)}% off` : "Deal"}
                    </span>
                  </Link>
                ))}
              </div>
            </div>
          )}

          <ProductCarousel title="Best Sellers" products={bestSellers} linkTo="/search?sort=best-sellers" linkLabel="See all best sellers" />

          {/* Themed collection grid row 1 */}
          <ThemedGrid collections={themedCollections.slice(0, 4)} />

          <ProductCarousel title="New Releases" products={newReleases} linkTo="/search?sort=new" linkLabel="Explore new releases" />
          <ProductCarousel title="Top Rated" products={topRated} linkTo="/search?sort=rating" linkLabel="See top rated" />

          {/* Value picks banner */}
          {under50.length > 0 && (
            <section className="bg-white rounded-lg shadow-sm p-5">
              <div className="flex items-center justify-between mb-3">
                <div>
                  <h2 className="text-lg font-bold text-gray-900">💰 Great picks under $50</h2>
                  <p className="text-xs text-gray-500">Quality tech that won't break the bank</p>
                </div>
                <Link to="/search?maxPrice=50" className="text-sm text-blue-700 hover:text-orange-600 hover:underline">See all</Link>
              </div>
              <div className="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-6 gap-3">
                {under50.slice(0, 6).map((p) => (
                  <Link key={p.id} to={`/products/${p.id}`} className="group text-center">
                    <div className="h-28 flex items-center justify-center bg-gray-50 rounded-md p-2 mb-1">
                      {p.imageUrl && <img src={p.imageUrl} alt={p.name} className="max-h-full max-w-full object-contain group-hover:scale-105 transition" />}
                    </div>
                    <p className="text-xs text-blue-700 group-hover:text-orange-600 line-clamp-1">{p.name}</p>
                    <p className="text-sm font-bold">${(p.dealPrice ?? p.price).toFixed(2)}</p>
                  </Link>
                ))}
              </div>
            </section>
          )}

          {/* Bottom marketing banner */}
          <div className="bg-gradient-to-r from-gray-900 to-gray-800 rounded-lg p-8 text-white text-center">
            <h2 className="text-2xl font-extrabold mb-2">
              Free Delivery on <span className="text-amber-400">Every Order</span>
            </h2>
            <p className="text-gray-400 text-sm mb-4">No minimum order. No hidden fees. Just great tech delivered to your door.</p>
            <Link to="/search" className="inline-block rounded-full bg-amber-400 px-8 py-2.5 text-sm font-bold text-gray-900 hover:bg-amber-500 transition">
              Start Shopping
            </Link>
          </div>
        </div>
      )}
    </div>
  );
}

function HeroCarousel() {
  const [current, setCurrent] = useState(0);
  const total = heroSlides.length;

  const next = useCallback(() => setCurrent((i) => (i + 1) % total), [total]);
  const prev = useCallback(() => setCurrent((i) => (i - 1 + total) % total), [total]);

  // Auto-advance every 6 seconds
  useEffect(() => {
    const timer = setInterval(next, 6000);
    return () => clearInterval(timer);
  }, [next]);

  const slide = heroSlides[current];
  const colors = ctaColors[slide.accent] ?? ctaColors.amber;

  return (
    <div className="relative overflow-hidden">
      {/* Slides */}
      <div
        className={`relative bg-gradient-to-r ${slide.gradient} text-white transition-all duration-700`}
      >
        <div className="absolute inset-0 opacity-20 transition-opacity duration-700">
          <div
            className="absolute inset-0 bg-cover bg-center"
            style={{ backgroundImage: `url('${slide.image}')` }}
          />
        </div>
        {/* Bottom gradient fade for card overlap */}
        <div className="absolute bottom-0 left-0 right-0 h-32 bg-gradient-to-t from-gray-100 to-transparent" />
        <div className="relative mx-auto max-w-7xl px-6 py-16 md:py-28 pb-32">
          <p className="font-semibold text-sm tracking-widest uppercase mb-2 opacity-80">
            {slide.tag}
          </p>
          <h1 className="text-3xl md:text-5xl font-extrabold leading-tight mb-4">
            {slide.title}
          </h1>
          <p className="text-gray-300 max-w-lg mb-6">{slide.subtitle}</p>
          <div className="flex gap-3">
            <Link
              to={slide.cta.to}
              className={`rounded-full px-6 py-2.5 text-sm font-bold transition ${colors.primary}`}
            >
              {slide.cta.label}
            </Link>
            {slide.ctaSecondary && (
              <Link
                to={slide.ctaSecondary.to}
                className={`rounded-full border px-6 py-2.5 text-sm font-semibold transition ${colors.secondary}`}
              >
                {slide.ctaSecondary.label}
              </Link>
            )}
          </div>
        </div>
      </div>

      {/* Left / Right arrows */}
      <button
        onClick={prev}
        className="absolute left-2 top-1/2 -translate-y-1/2 flex h-10 w-10 items-center justify-center rounded-full bg-black/30 text-white/80 hover:bg-black/50 hover:text-white transition"
        aria-label="Previous slide"
      >
        <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2.5}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
        </svg>
      </button>
      <button
        onClick={next}
        className="absolute right-2 top-1/2 -translate-y-1/2 flex h-10 w-10 items-center justify-center rounded-full bg-black/30 text-white/80 hover:bg-black/50 hover:text-white transition"
        aria-label="Next slide"
      >
        <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2.5}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" />
        </svg>
      </button>

      {/* Dots */}
      <div className="absolute bottom-3 left-1/2 -translate-x-1/2 flex gap-2">
        {heroSlides.map((_, i) => (
          <button
            key={i}
            onClick={() => setCurrent(i)}
            className={`h-2 rounded-full transition-all ${i === current ? "w-6 bg-white" : "w-2 bg-white/40 hover:bg-white/60"}`}
            aria-label={`Go to slide ${i + 1}`}
          />
        ))}
      </div>
    </div>
  );
}

function ThemedGrid({ collections }: { collections: typeof themedCollections }) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      {collections.map((col) => (
        <div key={col.title} className="bg-white rounded-lg shadow-sm p-4">
          <h3 className="font-bold text-gray-900 text-sm mb-3">{col.title}</h3>
          <div className="grid grid-cols-2 gap-2 mb-3">
            {col.items.map((item) => (
              <Link
                key={item.label}
                to={`/search?cat=${encodeURIComponent(item.cat)}`}
                className="group"
              >
                <div className="aspect-square bg-gray-50 rounded-md overflow-hidden flex items-center justify-center p-1">
                  <img
                    src={item.image}
                    alt={item.label}
                    className="max-h-full max-w-full object-contain group-hover:scale-105 transition"
                  />
                </div>
                <p className="text-xs text-gray-700 mt-1 leading-tight">{item.label}</p>
              </Link>
            ))}
          </div>
          <Link
            to={col.link}
            className="text-xs text-blue-700 hover:text-orange-600 hover:underline"
          >
            See more
          </Link>
        </div>
      ))}
    </div>
  );
}

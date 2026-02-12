import { useState, useRef, useEffect } from "react";
import { Link } from "react-router";
import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { handleLogin, handleLogout } from "../auth/authHelpers";
import { useUser } from "../hooks/useUser";
import { useBasket, useRemoveFromBasket, useAddToBasket } from "../hooks/useBasket";
import { useWishlist } from "../hooks/useWishlist";

const CartIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 100 4 2 2 0 000-4z" />
  </svg>
);

const categories = [
  { name: "Peripherals", href: "/?cat=Peripherals" },
  { name: "Audio", href: "/?cat=Audio" },
  { name: "Accessories", href: "/?cat=Accessories" },
  { name: "Monitors", href: "/?cat=Monitors" },
  { name: "Storage", href: "/?cat=Storage" },
  { name: "Networking", href: "/?cat=Networking" },
  { name: "Gaming", href: "/?cat=Gaming" },
  { name: "Laptops", href: "/?cat=Laptops" },
];
const navLinks = [
  { label: "Best Sellers", href: "/?sort=best-sellers" },
  { label: "New Releases", href: "/?sort=new" },
  { label: "Today's Deals", href: "/?deals=true" },
  { label: "Top Rated", href: "/?sort=rating" },
];

export default function Header() {
  const { instance } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const user = useUser();
  const { data: basket } = useBasket();
  const removeItem = useRemoveFromBasket();
  const addItem = useAddToBasket();
  const { count: wishlistCount } = useWishlist();
  const [basketOpen, setBasketOpen] = useState(false);
  const [catOpen, setCatOpen] = useState(false);
  const basketRef = useRef<HTMLDivElement>(null);
  const catRef = useRef<HTMLDivElement>(null);
  const items = basket?.items ?? [];
  const itemCount = items.reduce((sum, i) => sum + i.quantity, 0);
  const total = items.reduce((sum, i) => sum + i.product.price * i.quantity, 0);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (basketRef.current && !basketRef.current.contains(e.target as Node)) setBasketOpen(false);
      if (catRef.current && !catRef.current.contains(e.target as Node)) setCatOpen(false);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  return (
    <header className="sticky top-0 z-50">
      <div className="bg-gray-900 text-white">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-2">
          <Link to="/" className="flex items-center gap-2 text-xl font-bold tracking-tight">
            <span className="text-2xl">&#128722;</span>
            <span>eShop <span className="font-light text-amber-400">Academy</span></span>
          </Link>
          <div className="mx-8 hidden flex-1 md:block">
            <div className="flex rounded-md overflow-hidden">
              <input type="text" placeholder="Search products..." className="w-full bg-white px-4 py-2 text-sm text-gray-900 placeholder-gray-400 outline-none" />
              <button className="bg-amber-400 px-4 text-gray-900 hover:bg-amber-500">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
              </button>
            </div>
          </div>
          <div className="flex items-center gap-4">
            {isAuthenticated && user ? (
              <div className="hidden sm:flex items-center gap-2">
                {user.photoUrl ? (
                  <img src={user.photoUrl} alt={user.name} className="h-8 w-8 rounded-full border-2 border-amber-400 object-cover" />
                ) : (
                  <div className="flex h-8 w-8 items-center justify-center rounded-full border-2 border-amber-400 bg-gray-700 text-xs font-bold">{user.name.charAt(0).toUpperCase()}</div>
                )}
                <div className="text-xs">
                  <p className="text-gray-400">Hello, {user.name.split(" ")[0]}</p>
                  <button onClick={() => handleLogout(instance)} className="font-semibold text-white hover:text-amber-400">Sign out</button>
                </div>
              </div>
            ) : (
              <button onClick={() => handleLogin(instance)} className="text-xs text-left">
                <span className="text-gray-400">Hello, sign in</span>
                <p className="font-bold hover:text-amber-400">Account</p>
              </button>
            )}
            {isAuthenticated && (
              <Link to="/orders" className="hidden sm:block text-xs hover:text-amber-400">
                <span className="text-gray-400">Returns</span>
                <p className="font-bold">&amp; Orders</p>
              </Link>
            )}
            {isAuthenticated && (
              <Link to="/wishlist" className="relative flex items-center gap-1 hover:text-amber-400">
                <div className="relative">
                  <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                  </svg>
                  {wishlistCount > 0 && <span className="absolute -top-2 -right-2 flex h-5 w-5 items-center justify-center rounded-full bg-red-500 text-xs font-bold text-white">{wishlistCount}</span>}
                </div>
                <span className="hidden text-xs font-bold sm:block">Wishlist</span>
              </Link>
            )}
            <div ref={basketRef} className="relative">
              <button onClick={() => isAuthenticated && setBasketOpen(!basketOpen)} onMouseEnter={() => isAuthenticated && setBasketOpen(true)} className="relative flex items-center gap-1 hover:text-amber-400">
                <div className="relative">
                  <CartIcon />
                  {itemCount > 0 && <span className="absolute -top-2 -right-2 flex h-5 w-5 items-center justify-center rounded-full bg-amber-400 text-xs font-bold text-gray-900">{itemCount}</span>}
                </div>
                <span className="hidden text-xs font-bold sm:block">Cart</span>
              </button>
              {basketOpen && isAuthenticated && (
                <div onMouseLeave={() => setBasketOpen(false)} className="absolute right-0 top-full mt-2 w-80 rounded-lg border border-gray-200 bg-white text-gray-900 shadow-xl">
                  {items.length === 0 ? (
                    <p className="p-4 text-center text-sm text-gray-500">Your cart is empty</p>
                  ) : (
                    <>
                      <div className="max-h-72 overflow-y-auto divide-y">
                        {items.map((item) => (
                          <div key={item.product.id} className="flex items-center gap-3 p-3">
                            <div className="h-12 w-12 flex-shrink-0 rounded bg-gray-100 flex items-center justify-center text-[10px] text-gray-400">IMG</div>
                            <div className="flex-1 min-w-0">
                              <p className="text-sm font-medium truncate">{item.product.name}</p>
                              <p className="text-xs text-gray-500">${item.product.price.toFixed(2)}</p>
                            </div>
                            <div className="flex items-center gap-1">
                              <button onClick={() => removeItem.mutate({ productID: item.product.id, quantity: 1 })} className="rounded border px-1.5 py-0.5 text-xs hover:bg-gray-100">-</button>
                              <span className="w-6 text-center text-xs font-medium">{item.quantity}</span>
                              <button onClick={() => addItem.mutate({ productID: item.product.id, quantity: 1 })} className="rounded border px-1.5 py-0.5 text-xs hover:bg-gray-100">+</button>
                            </div>
                          </div>
                        ))}
                      </div>
                      <div className="border-t p-3">
                        <div className="flex justify-between text-sm font-bold mb-2">
                          <span>Subtotal ({itemCount} items)</span>
                          <span>${total.toFixed(2)}</span>
                        </div>
                        <Link to="/basket" onClick={() => setBasketOpen(false)} className="block w-full rounded-lg bg-amber-400 py-2 text-center text-sm font-semibold text-gray-900 hover:bg-amber-500">Go to Cart</Link>
                      </div>
                    </>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
      <div className="relative z-40 bg-gray-800 text-white text-sm">
        <div className="mx-auto flex max-w-7xl items-center gap-6 px-4 py-1.5">
          <div ref={catRef} className="relative flex-shrink-0">
            <button onClick={() => setCatOpen(!catOpen)} className="flex items-center gap-1 font-bold hover:text-amber-400">
              <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}><path strokeLinecap="round" strokeLinejoin="round" d="M4 6h16M4 12h16M4 18h16" /></svg>
              All Categories
            </button>
            {catOpen && (
              <div className="absolute left-0 top-full mt-1 w-56 rounded-lg border border-gray-200 bg-white text-gray-900 shadow-xl z-50">
                {categories.map((cat) => <Link key={cat.name} to={cat.href} onClick={() => setCatOpen(false)} className="block px-4 py-2 text-sm hover:bg-gray-100">{cat.name}</Link>)}
              </div>
            )}
          </div>
          {navLinks.map((link) => <Link key={link.label} to={link.href} className="whitespace-nowrap hover:text-amber-400">{link.label}</Link>)}
          <span className="ml-auto whitespace-nowrap text-amber-400 font-semibold">Special Offers!</span>
        </div>
      </div>
    </header>
  );
}

import { useState, useRef, useEffect } from "react";
import { Link, useNavigate } from "react-router";
import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { handleLogin, handleLogout } from "../auth/authHelpers";
import { useUser } from "../hooks/useUser";
import { useBasket, useRemoveFromBasket, useAddToBasket } from "../hooks/useBasket";
import { useWishlist } from "../hooks/useWishlist";
import { useNotifications, useMarkNotificationRead, useMarkAllNotificationsRead } from "../hooks/useNotifications";
import { useSeller } from "../hooks/useSeller";

const BellIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
  </svg>
);

const CartIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 100 4 2 2 0 000-4z" />
  </svg>
);

const categories = [
  { name: "Peripherals", href: "/search?cat=Peripherals" },
  { name: "Audio", href: "/search?cat=Audio" },
  { name: "Accessories", href: "/search?cat=Accessories" },
  { name: "Monitors", href: "/search?cat=Monitors" },
  { name: "Storage", href: "/search?cat=Storage" },
  { name: "Networking", href: "/search?cat=Networking" },
  { name: "Gaming", href: "/search?cat=Gaming" },
  { name: "Laptops", href: "/search?cat=Laptops" },
];
const navLinks = [
  { label: "Best Sellers", href: "/search?sort=best-sellers" },
  { label: "New Releases", href: "/search?sort=new" },
  { label: "Today's Deals", href: "/search?deals=true" },
  { label: "Top Rated", href: "/search?sort=rating" },
];

function formatRelativeTime(dateString: string): string {
  const now = Date.now();
  const then = new Date(dateString).getTime();
  const seconds = Math.floor((now - then) / 1000);
  if (seconds < 60) return "just now";
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

export default function Header() {
  const { instance } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const user = useUser();
  const { data: basket } = useBasket();
  const removeItem = useRemoveFromBasket();
  const addItem = useAddToBasket();
  const { count: wishlistCount } = useWishlist();
  const { data: notifications = [] } = useNotifications();
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllNotificationsRead();
  const { data: sellerData } = useSeller();
  const [basketOpen, setBasketOpen] = useState(false);
  const [catOpen, setCatOpen] = useState(false);
  const [notifOpen, setNotifOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const basketRef = useRef<HTMLDivElement>(null);
  const catRef = useRef<HTMLDivElement>(null);
  const notifRef = useRef<HTMLDivElement>(null);
  const userMenuRef = useRef<HTMLDivElement>(null);
  const [searchText, setSearchText] = useState("");
  const navigate = useNavigate();

  const unreadCount = notifications.filter(n => !n.isRead).length;
  const items = basket?.items ?? [];
  const itemCount = items.reduce((sum, i) => sum + i.quantity, 0);
  const total = items.reduce((sum, i) => sum + i.product.price * i.quantity, 0);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (basketRef.current && !basketRef.current.contains(e.target as Node)) setBasketOpen(false);
      if (catRef.current && !catRef.current.contains(e.target as Node)) setCatOpen(false);
      if (notifRef.current && !notifRef.current.contains(e.target as Node)) setNotifOpen(false);
      if (userMenuRef.current && !userMenuRef.current.contains(e.target as Node)) setUserMenuOpen(false);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  return (
    <header className="sticky top-0 z-50">
      <div className="bg-gray-900 text-white">
        <div className="mx-auto flex items-center px-4 py-2 gap-4">
          <Link to="/" className="flex items-center gap-2 text-xl font-bold tracking-tight shrink-0">
            <span className="text-2xl">&#128722;</span>
            <span>eShop <span className="font-light text-amber-400">Academy</span></span>
          </Link>
          <div className="hidden flex-1 md:block">
            <form className="flex rounded-md overflow-hidden" onSubmit={(e) => { e.preventDefault(); if (searchText.trim()) navigate(`/search?searchText=${encodeURIComponent(searchText.trim())}`); }}>
              <input type="text" placeholder="Search eShop Academy" value={searchText} onChange={(e) => setSearchText(e.target.value)} className="w-full bg-white px-4 py-2.5 text-sm text-gray-900 placeholder-gray-400 outline-none" />
              <button type="submit" className="bg-amber-400 px-5 text-gray-900 hover:bg-amber-500 transition">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
              </button>
            </form>
          </div>
          <div className="flex items-center gap-4 shrink-0">
            {isAuthenticated && user ? (
              <div ref={userMenuRef} className="relative hidden sm:block">
                <button onClick={() => setUserMenuOpen(!userMenuOpen)} className="flex items-center gap-2 hover:text-amber-400 transition">
                  {user.photoUrl ? (
                    <img src={user.photoUrl} alt={user.name} className="h-8 w-8 rounded-full border-2 border-amber-400 object-cover" />
                  ) : (
                    <div className="flex h-8 w-8 items-center justify-center rounded-full border-2 border-amber-400 bg-gray-700 text-xs font-bold">{user.name.charAt(0).toUpperCase()}</div>
                  )}
                  <div className="text-xs text-left">
                    <p className="text-gray-400">Hello, {user.name.split(" ")[0]}</p>
                    <p className="font-semibold text-white">Account ▾</p>
                  </div>
                </button>
                {userMenuOpen && (
                  <div className="absolute right-0 top-full mt-2 w-56 rounded-lg border border-gray-200 bg-white text-gray-900 shadow-xl z-[100] py-1">
                    <div className="px-4 py-2 border-b border-gray-100">
                      <p className="text-sm font-semibold truncate">{user.name}</p>
                    </div>
                    <Link to="/wishlist" onClick={() => setUserMenuOpen(false)} className="flex items-center gap-3 px-4 py-2.5 text-sm hover:bg-gray-50 transition">
                      <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4 text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                      </svg>
                      Wishlist
                      {wishlistCount > 0 && <span className="ml-auto text-xs bg-red-100 text-red-600 px-1.5 py-0.5 rounded-full font-medium">{wishlistCount}</span>}
                    </Link>
                    <Link to="/orders" onClick={() => setUserMenuOpen(false)} className="flex items-center gap-3 px-4 py-2.5 text-sm hover:bg-gray-50 transition">
                      <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4 text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                      </svg>
                      Orders
                    </Link>
                    <div className="border-t border-gray-100 my-1" />
                    {sellerData && sellerData.status === "Active" ? (
                      <Link to="/sell/dashboard" onClick={() => setUserMenuOpen(false)} className="flex items-center gap-3 px-4 py-2.5 text-sm hover:bg-gray-50 transition">
                        <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4 text-emerald-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 100 4 2 2 0 000-4z" />
                        </svg>
                        <span className="text-emerald-700 font-medium">Seller Dashboard</span>
                      </Link>
                    ) : sellerData && sellerData.status === "PendingApproval" ? (
                      <div className="flex items-center gap-3 px-4 py-2.5 text-sm text-gray-400">
                        <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                        Seller (Pending Approval)
                      </div>
                    ) : (
                      <Link to="/sell/register" onClick={() => setUserMenuOpen(false)} className="flex items-center gap-3 px-4 py-2.5 text-sm hover:bg-gray-50 transition">
                        <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4 text-indigo-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                        Start Selling
                      </Link>
                    )}
                    <div className="border-t border-gray-100 my-1" />
                    <button onClick={() => { setUserMenuOpen(false); handleLogout(instance); }} className="flex items-center gap-3 px-4 py-2.5 text-sm hover:bg-gray-50 transition w-full text-left text-red-600">
                      <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                      </svg>
                      Sign out
                    </button>
                  </div>
                )}
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
              <div ref={notifRef} className="relative z-50">
                <button onClick={() => setNotifOpen(!notifOpen)} onMouseEnter={() => setNotifOpen(true)} className="relative flex items-center gap-1 hover:text-amber-400">
                  <div className="relative">
                    <BellIcon />
                    {unreadCount > 0 && <span className="absolute -top-2 -right-2 flex h-5 w-5 items-center justify-center rounded-full bg-red-500 text-xs font-bold text-white">{unreadCount}</span>}
                  </div>
                  <span className="hidden text-xs font-bold sm:block">Notifications</span>
                </button>
                {notifOpen && (
                  <div onMouseLeave={() => setNotifOpen(false)} className="absolute right-0 top-full mt-2 w-80 rounded-lg border border-gray-200 bg-white text-gray-900 shadow-xl z-[100]">
                    <div className="border-b p-3 flex items-center justify-between">
                      <h3 className="font-semibold text-sm">Notifications</h3>
                      {unreadCount > 0 && (
                        <button onClick={() => markAllRead.mutate()} className="text-xs text-amber-600 hover:text-amber-700 font-medium">
                          Mark all read
                        </button>
                      )}
                    </div>
                    {notifications.length === 0 ? (
                      <p className="p-4 text-center text-sm text-gray-500">No notifications</p>
                    ) : (
                      <div className="max-h-96 overflow-y-auto divide-y">
                        {notifications.map((notif) => (
                          <div
                            key={notif.id}
                            onClick={() => !notif.isRead && markRead.mutate(notif.id)}
                            className={`p-3 hover:bg-gray-50 cursor-pointer ${
                              !notif.isRead ? "bg-blue-50" : ""
                            }`}
                          >
                            <div className="flex items-start gap-2">
                              {!notif.isRead && (
                                <div className="mt-1.5 h-2 w-2 rounded-full bg-blue-600 flex-shrink-0" />
                              )}
                              <div className="flex-1 min-w-0">
                                <p className="text-sm font-medium text-gray-900 truncate">
                                  {notif.subject}
                                </p>
                                <p className="text-xs text-gray-600 mt-0.5">
                                  {notif.body}
                                </p>
                                <p className="text-xs text-gray-400 mt-1">{formatRelativeTime(notif.createdAt)}</p>
                              </div>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                    <div className="border-t p-2">
                      <Link
                        to="/notifications"
                        onClick={() => setNotifOpen(false)}
                        className="block text-center text-xs text-amber-600 hover:text-amber-700 font-medium py-1"
                      >
                        View all notifications
                      </Link>
                    </div>
                  </div>
                )}
              </div>
            )}
            <div ref={basketRef} className="relative z-50">
              <button onClick={() => isAuthenticated && setBasketOpen(!basketOpen)} onMouseEnter={() => isAuthenticated && setBasketOpen(true)} className="relative flex items-center gap-1 hover:text-amber-400">
                <div className="relative">
                  <CartIcon />
                  {itemCount > 0 && <span className="absolute -top-2 -right-2 flex h-5 w-5 items-center justify-center rounded-full bg-amber-400 text-xs font-bold text-gray-900">{itemCount}</span>}
                </div>
                <span className="hidden text-xs font-bold sm:block">Cart</span>
              </button>
              {basketOpen && isAuthenticated && (
                <div onMouseLeave={() => setBasketOpen(false)} className="absolute right-0 top-full mt-2 w-80 rounded-lg border border-gray-200 bg-white text-gray-900 shadow-xl z-[100]">
                  {items.length === 0 ? (
                    <p className="p-4 text-center text-sm text-gray-500">Your cart is empty</p>
                  ) : (
                    <>
                      <div className="max-h-72 overflow-y-auto divide-y">
                        {items.map((item) => (
                          <div key={item.product.id} className="flex items-start gap-3 p-3">
                            <div className="h-12 w-12 flex-shrink-0 rounded bg-gray-100 flex items-center justify-center text-[10px] text-gray-400">
                              {/* Product image placeholder */}
                              <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6 text-gray-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
                              </svg>
                            </div>
                            <div className="flex-1 min-w-0">
                              <p className="text-sm font-medium truncate leading-tight">{item.product.name || "Product"}</p>
                              <p className="text-xs text-gray-500 mt-0.5">${item.product.price.toFixed(2)} each</p>
                              <div className="flex items-center gap-2 mt-2">
                                <button 
                                  onClick={(e) => { e.stopPropagation(); removeItem.mutate({ productID: item.product.id, quantity: 1 }); }} 
                                  className="flex h-6 w-6 items-center justify-center rounded border border-gray-300 text-gray-600 hover:bg-gray-100 hover:border-gray-400"
                                  aria-label="Decrease quantity"
                                >
                                  <svg xmlns="http://www.w3.org/2000/svg" className="h-3 w-3" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={3}>
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M20 12H4" />
                                  </svg>
                                </button>
                                <span className="min-w-[2rem] text-center text-sm font-semibold text-gray-900">{item.quantity}</span>
                                <button 
                                  onClick={(e) => { e.stopPropagation(); addItem.mutate({ productID: item.product.id, quantity: 1 }); }} 
                                  className="flex h-6 w-6 items-center justify-center rounded border border-gray-300 text-gray-600 hover:bg-gray-100 hover:border-gray-400"
                                  aria-label="Increase quantity"
                                >
                                  <svg xmlns="http://www.w3.org/2000/svg" className="h-3 w-3" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={3}>
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m8-8H4" />
                                  </svg>
                                </button>
                              </div>
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
        <div className="mx-auto flex max-w-7xl items-center px-4 py-0">
          <div ref={catRef} className="relative flex-shrink-0">
            <button onClick={() => setCatOpen(!catOpen)} className="flex items-center gap-1.5 font-bold hover:bg-gray-700 px-3 py-2 rounded transition">
              <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}><path strokeLinecap="round" strokeLinejoin="round" d="M4 6h16M4 12h16M4 18h16" /></svg>
              All
            </button>
            {catOpen && (
              <div className="absolute left-0 top-full mt-0.5 w-64 rounded-lg border border-gray-200 bg-white text-gray-900 shadow-xl z-50 py-1">
                <p className="px-4 py-2 text-xs font-bold text-gray-500 uppercase tracking-wider">Shop by Category</p>
                {categories.map((cat) => (
                  <Link key={cat.name} to={cat.href} onClick={() => setCatOpen(false)} className="flex items-center justify-between px-4 py-2.5 text-sm hover:bg-gray-50 transition">
                    {cat.name}
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-3.5 w-3.5 text-gray-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}><path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" /></svg>
                  </Link>
                ))}
              </div>
            )}
          </div>
          <div className="h-5 w-px bg-gray-600 mx-1" />
          {navLinks.map((link) => <Link key={link.label} to={link.href} className="whitespace-nowrap hover:bg-gray-700 px-3 py-2 rounded transition">{link.label}</Link>)}
          {isAuthenticated && (
            <>
              <div className="h-5 w-px bg-gray-600 mx-1" />
              {sellerData && sellerData.status === "Active" ? (
                <Link to="/sell/dashboard" className="whitespace-nowrap flex items-center gap-1.5 bg-gradient-to-r from-emerald-500 to-teal-500 text-white font-bold px-3 py-1 rounded-full text-xs hover:from-emerald-400 hover:to-teal-400 transition">
                  <span>🏪</span> Seller Dashboard
                </Link>
              ) : sellerData && sellerData.status === "PendingApproval" ? (
                <span className="whitespace-nowrap flex items-center gap-1.5 bg-gray-600 text-gray-300 font-bold px-3 py-1 rounded-full text-xs cursor-default">
                  <span>⏳</span> Verification Pending
                </span>
              ) : (
                <Link to="/sell/register" className="whitespace-nowrap flex items-center gap-1.5 bg-gradient-to-r from-indigo-500 to-purple-500 text-white font-bold px-3 py-1 rounded-full text-xs hover:from-indigo-400 hover:to-purple-400 transition">
                  <span>💰</span> Start Selling
                </Link>
              )}
            </>
          )}
          <Link to="/search?deals=true" className="ml-auto flex items-center gap-1.5 whitespace-nowrap bg-gradient-to-r from-amber-500 to-orange-500 text-gray-900 font-bold px-3 py-1 rounded-full text-xs hover:from-amber-400 hover:to-orange-400 transition">
            <span>🔥</span> Special Offers
          </Link>
        </div>
      </div>
    </header>
  );
}

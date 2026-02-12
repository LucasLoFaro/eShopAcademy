import { Outlet } from "react-router";
import Header from "./Header";

export default function Layout() {
  return (
    <div className="flex min-h-screen flex-col bg-gray-100 text-gray-900">
      <Header />
      <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-6">
        <Outlet />
      </main>
      <footer className="bg-gray-800 text-gray-400 text-xs">
        <div className="mx-auto max-w-7xl px-4 py-6">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6 mb-6">
            <div>
              <h4 className="text-white font-bold mb-2">Get to Know Us</h4>
              <p>About eShop Academy</p><p>Careers</p><p>Press Releases</p>
            </div>
            <div>
              <h4 className="text-white font-bold mb-2">Make Money with Us</h4>
              <p>Sell products</p><p>Become an Affiliate</p><p>Advertise</p>
            </div>
            <div>
              <h4 className="text-white font-bold mb-2">Payment Products</h4>
              <p>Business Card</p><p>Shop with Points</p><p>Reload Your Balance</p>
            </div>
            <div>
              <h4 className="text-white font-bold mb-2">Let Us Help You</h4>
              <p>Your Account</p><p>Shipping Rates</p><p>Returns &amp; Replacements</p>
            </div>
          </div>
          <div className="border-t border-gray-700 pt-4 text-center">
            eShop Academy &mdash; Phase 1 Consumer Frontend &middot; &copy; {new Date().getFullYear()}
          </div>
        </div>
      </footer>
    </div>
  );
}

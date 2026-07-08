import { useIsAuthenticated, useMsal } from '@azure/msal-react';
import { DashboardPage } from './pages/DashboardPage';
import { handleLogin, handleLogout } from './auth/authHelpers';

const sellerId = import.meta.env.VITE_DEFAULT_SELLER_ID ?? '00000000-0000-0000-0000-000000000000';

export function App() {
  const isAuthenticated = useIsAuthenticated();
  const { instance, accounts } = useMsal();

  if (!isAuthenticated) {
    return (
      <main style={{ maxWidth: 1100, margin: '0 auto', padding: '2rem 1rem', fontFamily: 'Inter, Arial, sans-serif' }}>
        <header style={{ marginBottom: 24 }}>
          <h1 style={{ margin: 0 }}>Sellers Portal</h1>
          <p style={{ color: '#64748b', marginTop: 8 }}>
            Microfrontend starter for seller management, stock ownership and transaction insights.
          </p>
        </header>
        <button
          onClick={() => handleLogin(instance)}
          style={{ padding: '10px 20px', fontSize: '1rem', cursor: 'pointer' }}
        >
          Sign in
        </button>
      </main>
    );
  }

  return (
    <main style={{ maxWidth: 1100, margin: '0 auto', padding: '2rem 1rem', fontFamily: 'Inter, Arial, sans-serif' }}>
      <header style={{ marginBottom: 24, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ margin: 0 }}>Sellers Portal</h1>
          <p style={{ color: '#64748b', marginTop: 8 }}>
            Microfrontend starter for seller management, stock ownership and transaction insights.
          </p>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <span style={{ fontSize: '0.875rem', color: '#64748b' }}>{accounts[0]?.username}</span>
          <button
            onClick={() => handleLogout(instance)}
            style={{ padding: '6px 14px', fontSize: '0.875rem', cursor: 'pointer' }}
          >
            Sign out
          </button>
        </div>
      </header>
      <DashboardPage sellerId={sellerId} />
    </main>
  );
}

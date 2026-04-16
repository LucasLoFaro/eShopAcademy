import { DashboardPage } from './pages/DashboardPage';

const sellerId = import.meta.env.VITE_DEFAULT_SELLER_ID ?? '00000000-0000-0000-0000-000000000000';

export function App() {
  return (
    <main style={{ maxWidth: 1100, margin: '0 auto', padding: '2rem 1rem', fontFamily: 'Inter, Arial, sans-serif' }}>
      <header style={{ marginBottom: 24 }}>
        <h1 style={{ margin: 0 }}>Sellers Portal</h1>
        <p style={{ color: '#64748b', marginTop: 8 }}>
          Microfrontend starter for seller management, stock ownership and transaction insights.
        </p>
      </header>
      <DashboardPage sellerId={sellerId} />
    </main>
  );
}

import { useEffect, useState } from 'react';
import { getSeller, getSellerFinancialSummary, getSellerLedger, Seller } from '../api/sellersApi';
import { StatCard } from '../components/StatCard';

type DashboardPageProps = {
  sellerId: string;
};

export function DashboardPage({ sellerId }: DashboardPageProps) {
  const [seller, setSeller] = useState<Seller | null>(null);
  const [summary, setSummary] = useState<{
    accumulatedSalesAmount: number;
    accumulatedCommissionsAmount: number;
    netAmount: number;
    ledgerEntries: number;
  } | null>(null);
  const [ledger, setLedger] = useState<Array<{ entryId: string; createdAt: string; notes: string; netAmount: number }>>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function load() {
      try {
        const [sellerResponse, summaryResponse, ledgerResponse] = await Promise.all([
          getSeller(sellerId),
          getSellerFinancialSummary(sellerId),
          getSellerLedger(sellerId)
        ]);

        setSeller(sellerResponse);
        setSummary(summaryResponse);
        setLedger(
          ledgerResponse.map(entry => ({
            entryId: entry.entryId,
            createdAt: entry.createdAt,
            notes: entry.notes,
            netAmount: entry.netAmount
          }))
        );
      } catch (loadError) {
        setError(loadError instanceof Error ? loadError.message : 'Unable to load seller dashboard');
      }
    }

    void load();
  }, [sellerId]);

  if (error) {
    return <p style={{ color: '#b91c1c' }}>{error}</p>;
  }

  if (!seller || !summary) {
    return <p>Loading seller dashboard...</p>;
  }

  return (
    <div style={{ display: 'grid', gap: 20 }}>
      <section>
        <h2 style={{ margin: 0 }}>{seller.name}</h2>
        <p style={{ margin: '6px 0 0 0', color: '#64748b' }}>
          {seller.email} • Tax ID: {seller.taxId} • Status: {seller.status}
        </p>
      </section>

      <section style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
        <StatCard title="Total Sales" value={`$${summary.accumulatedSalesAmount.toFixed(2)}`} />
        <StatCard title="Total Commissions" value={`$${summary.accumulatedCommissionsAmount.toFixed(2)}`} />
        <StatCard title="Net Amount" value={`$${summary.netAmount.toFixed(2)}`} />
        <StatCard title="Transactions" value={summary.ledgerEntries.toString()} />
      </section>

      <section>
        <h3 style={{ marginBottom: 8 }}>Recent Transactions</h3>
        <div style={{ border: '1px solid #e2e8f0', borderRadius: 8, overflow: 'hidden' }}>
          {ledger.length === 0 ? (
            <p style={{ padding: 16, margin: 0 }}>No transactions yet.</p>
          ) : (
            ledger.map(entry => (
              <div
                key={entry.entryId}
                style={{
                  display: 'grid',
                  gridTemplateColumns: '180px 1fr 120px',
                  gap: 12,
                  padding: 12,
                  borderBottom: '1px solid #f1f5f9'
                }}
              >
                <span>{new Date(entry.createdAt).toLocaleString()}</span>
                <span>{entry.notes || 'Sale registered'}</span>
                <strong style={{ textAlign: 'right' }}>${entry.netAmount.toFixed(2)}</strong>
              </div>
            ))
          )}
        </div>
      </section>
    </div>
  );
}

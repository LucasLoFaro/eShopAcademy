export type Seller = {
  id: string;
  name: string;
  email: string;
  taxId: string;
  status: 'PendingApproval' | 'Active' | 'Suspended' | 'Rejected';
  accumulatedSalesAmount: number;
  accumulatedCommissionsAmount: number;
  publishedProducts: number;
  ledgerEntries: number;
};

const baseUrl = import.meta.env.VITE_SELLERS_API_BASE_URL ?? 'http://localhost:8010';

export async function getSeller(sellerId: string, token?: string): Promise<Seller> {
  const response = await fetch(`${baseUrl}/api/sellers/${sellerId}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined
  });

  if (!response.ok) {
    throw new Error(`Failed to load seller (${response.status})`);
  }

  return response.json() as Promise<Seller>;
}

export async function getSellerFinancialSummary(sellerId: string, token?: string): Promise<{
  sellerId: string;
  accumulatedSalesAmount: number;
  accumulatedCommissionsAmount: number;
  netAmount: number;
  ledgerEntries: number;
}> {
  const response = await fetch(`${baseUrl}/api/sellers/${sellerId}/financial-summary`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined
  });

  if (!response.ok) {
    throw new Error(`Failed to load financial summary (${response.status})`);
  }

  return response.json() as Promise<{
    sellerId: string;
    accumulatedSalesAmount: number;
    accumulatedCommissionsAmount: number;
    netAmount: number;
    ledgerEntries: number;
  }>;
}

export async function getSellerLedger(sellerId: string, token?: string): Promise<
  Array<{
    entryId: string;
    orderId: string;
    orderItemId: string;
    grossAmount: number;
    commissionAmount: number;
    netAmount: number;
    type: string;
    createdAt: string;
    notes: string;
  }>
> {
  const response = await fetch(`${baseUrl}/api/sellers/${sellerId}/ledger?take=20`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined
  });

  if (!response.ok) {
    throw new Error(`Failed to load ledger (${response.status})`);
  }

  return response.json() as Promise<
    Array<{
      entryId: string;
      orderId: string;
      orderItemId: string;
      grossAmount: number;
      commissionAmount: number;
      netAmount: number;
      type: string;
      createdAt: string;
      notes: string;
    }>
  >;
}

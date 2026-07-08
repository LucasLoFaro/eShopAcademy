import { msalInstance, loginRequest } from '../auth/msalConfig';

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

const baseUrl = '';

async function getAccessToken(): Promise<string | undefined> {
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length === 0) return undefined;

  try {
    const response = await msalInstance.acquireTokenSilent({
      ...loginRequest,
      account: accounts[0],
    });
    return response.accessToken;
  } catch {
    await msalInstance.loginRedirect(loginRequest);
    return undefined;
  }
}

async function authFetch(url: string): Promise<Response> {
  const token = await getAccessToken();
  const response = await fetch(url, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  });

  if (response.status === 401) {
    await msalInstance.loginRedirect(loginRequest);
    throw new Error('Authentication required - redirecting to login');
  }

  return response;
}

export async function getSeller(sellerId: string): Promise<Seller> {
  const response = await authFetch(`${baseUrl}/api/sellers/${sellerId}`);

  if (!response.ok) {
    throw new Error(`Failed to load seller (${response.status})`);
  }

  return response.json() as Promise<Seller>;
}

export async function getSellerFinancialSummary(sellerId: string): Promise<{
  sellerId: string;
  accumulatedSalesAmount: number;
  accumulatedCommissionsAmount: number;
  netAmount: number;
  ledgerEntries: number;
}> {
  const response = await authFetch(`${baseUrl}/api/sellers/${sellerId}/financial-summary`);

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

export async function getSellerLedger(sellerId: string): Promise<
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
  const response = await authFetch(`${baseUrl}/api/sellers/${sellerId}/ledger?take=20`);

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

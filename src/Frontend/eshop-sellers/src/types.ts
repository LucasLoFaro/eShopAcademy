export interface Seller {
  id: string;
  name: string;
  email: string;
  taxId: string;
  status: SellerStatus;
  address: SellerAddress;
  accumulatedSalesAmount: number;
  accumulatedCommissionsAmount: number;
  publishedProducts: number;
  ledgerEntries: number;
}

export type SellerStatus = "PendingApproval" | "Active" | "Suspended" | "Rejected";

export interface SellerAddress {
  street: string;
  number: string;
  additionalInformation: string;
  zipCode: string;
  city: string;
  state: string;
  country: string;
}

export interface SellerLedgerEntry {
  id: string;
  type: string;
  amount: number;
  notes: string;
  orderId: string;
  createdAt: string;
}

export interface SellerFinancialSummary {
  sellerId: string;
  totalSales: number;
  totalCommissions: number;
  netEarnings: number;
  ledgerEntriesCount: number;
}

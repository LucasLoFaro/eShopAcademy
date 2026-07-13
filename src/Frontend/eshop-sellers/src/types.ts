export interface Seller {
  id: string;
  name: string;
  email: string;
  taxId: string;
  status: SellerStatus;
  address: SellerAddress;
  accumulatedSalesAmount: number;
  accumulatedCommissionsAmount: number;
  publishedProductIds: string[];
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
  entryId: string;
  orderId: string;
  orderItemId: string;
  grossAmount: number;
  commissionAmount: number;
  netAmount: number;
  type: string;
  createdAt: string;
  notes: string;
  isProcessed: boolean;
  processedAt: string | null;
}

export interface SellerFinancialSummary {
  sellerId: string;
  totalSales: number;
  totalCommissions: number;
  netEarnings: number;
  ledgerEntriesCount: number;
}

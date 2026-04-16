namespace Domain.Sellers.Contracts;

public record SellerFinancialSummaryResponse(
    Guid SellerId,
    decimal AccumulatedSalesAmount,
    decimal AccumulatedCommissionsAmount,
    decimal NetAmount,
    int LedgerEntries);

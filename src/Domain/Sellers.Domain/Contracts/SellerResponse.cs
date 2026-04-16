using Domain.Sellers.Entities;
using Domain.Sellers.Enums;

namespace Domain.Sellers.Contracts;

public record SellerResponse(
    Guid Id,
    string Name,
    string Email,
    string TaxId,
    SellerStatus Status,
    SellerAddress Address,
    decimal AccumulatedSalesAmount,
    decimal AccumulatedCommissionsAmount,
    int LedgerEntries)
{
    public static SellerResponse FromSeller(Seller seller) =>
        new(
            seller.Id,
            seller.Name,
            seller.Email,
            seller.TaxId,
            seller.Status,
            seller.Address,
            seller.AccumulatedSalesAmount,
            seller.AccumulatedCommissionsAmount,
            seller.Ledger.Count);
}

using Domain.Sellers.Enums;

namespace Domain.Sellers.Entities;

public class Seller : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public SellerStatus Status { get; set; } = SellerStatus.PendingApproval;
    public SellerAddress Address { get; set; } = new();

    public decimal AccumulatedSalesAmount { get; set; }
    public decimal AccumulatedCommissionsAmount { get; set; }

    public List<Guid> PublishedProductIds { get; set; } = [];
    public List<SellerLedgerEntry> Ledger { get; set; } = [];
}

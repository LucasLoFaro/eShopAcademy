using Domain.Sellers.Enums;

namespace Domain.Sellers.Entities;

public class Seller : BaseEntity
{
    public string IdentityObjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public SellerStatus Status { get; set; } = SellerStatus.PendingApproval;
    public DocumentVerificationStatus VerificationStatus { get; set; } = DocumentVerificationStatus.Pending;
    public string? VerificationNotes { get; set; }
    public SellerAddress Address { get; set; } = new();

    public decimal AccumulatedSalesAmount { get; set; }
    public decimal AccumulatedCommissionsAmount { get; set; }

    public List<Guid> PublishedProductIds { get; set; } = [];
    public List<SellerLedgerEntry> Ledger { get; set; } = [];
}

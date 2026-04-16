using Domain.Sellers.Enums;

namespace Domain.Sellers.Entities;

public class SellerLedgerEntry
{
    public Guid EntryId { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid OrderItemId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount => GrossAmount - CommissionAmount;
    public SellerLedgerEntryType Type { get; set; } = SellerLedgerEntryType.Sale;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;
}

using Domain.Sellers.Entities;
using Domain.Sellers.Enums;

namespace Domain.Sellers.Contracts;

public record SellerLedgerEntryResponse(
    Guid EntryId,
    Guid OrderId,
    Guid OrderItemId,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal NetAmount,
    SellerLedgerEntryType Type,
    DateTime CreatedAt,
    string Notes)
{
    public static SellerLedgerEntryResponse FromEntry(SellerLedgerEntry entry) =>
        new(
            entry.EntryId,
            entry.OrderId,
            entry.OrderItemId,
            entry.GrossAmount,
            entry.CommissionAmount,
            entry.NetAmount,
            entry.Type,
            entry.CreatedAt,
            entry.Notes);
}

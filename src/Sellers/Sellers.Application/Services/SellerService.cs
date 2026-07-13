using Domain.Common.Events.Sellers;
using Domain.Sellers.Contracts;
using Domain.Sellers.Entities;
using Domain.Sellers.Enums;
using MassTransit;
using Sellers.Application.Repositories;

namespace Sellers.Application.Services;

public class SellerService : ISellerService
{
    private readonly ISellerRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;

    public SellerService(ISellerRepository repository, IPublishEndpoint publishEndpoint)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Seller> CreateAsync(CreateSellerRequest request, CancellationToken cancellationToken)
    {
        var seller = new Seller
        {
            Name = request.Name,
            Email = request.Email,
            TaxId = request.TaxId,
            Address = request.Address
        };

        return await _repository.CreateAsync(seller, cancellationToken);
    }

    public async Task<Seller> RegisterAsync(string identityObjectId, RegisterSellerRequest request, CancellationToken cancellationToken)
    {
        var seller = new Seller
        {
            IdentityObjectId = identityObjectId,
            Name = request.Name,
            Email = request.Email,
            TaxId = request.TaxId,
            Address = request.Address,
            Status = SellerStatus.PendingApproval,
            VerificationStatus = DocumentVerificationStatus.Pending
        };

        var created = await _repository.CreateAsync(seller, cancellationToken);

        await _publishEndpoint.Publish(new SellerRegistrationRequestedEvent
        {
            SellerId = created.Id,
            Name = created.Name,
            Email = created.Email,
            TaxId = created.TaxId,
            DocumentUrl = request.DocumentUrl
        }, cancellationToken);

        await _publishEndpoint.Publish(new SellerTaxVerificationRequestedEvent
        {
            SellerId = created.Id,
            Name = created.Name,
            Email = created.Email,
            TaxId = created.TaxId
        }, cancellationToken);

        return created;
    }

    public Task<Seller?> GetByIdentityAsync(string identityObjectId, CancellationToken cancellationToken)
        => _repository.GetByIdentityAsync(identityObjectId, cancellationToken);

    public Task<IReadOnlyList<Seller>> GetAllAsync(CancellationToken cancellationToken)
        => _repository.GetAllAsync(cancellationToken);

    public Task<Seller?> GetByIdAsync(Guid sellerId, CancellationToken cancellationToken)
        => _repository.GetByIdAsync(sellerId, cancellationToken);

    public async Task<Seller?> UpdateStatusAsync(Guid sellerId, SellerStatus status, CancellationToken cancellationToken)
    {
        var seller = await _repository.GetByIdAsync(sellerId, cancellationToken);
        if (seller is null)
        {
            return null;
        }

        seller.Status = status;
        return await _repository.UpdateAsync(seller, cancellationToken);
    }

    public async Task<Seller?> AssignPublishedProductsAsync(
        Guid sellerId,
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var seller = await _repository.GetByIdAsync(sellerId, cancellationToken);
        if (seller is null)
        {
            return null;
        }

        seller.PublishedProductIds = productIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        return await _repository.UpdateAsync(seller, cancellationToken);
    }

    public async Task<Seller?> RegisterSaleAsync(
        Guid sellerId,
        Guid orderId,
        Guid orderItemId,
        decimal grossAmount,
        decimal commissionAmount,
        string notes,
        CancellationToken cancellationToken)
    {
        var seller = await _repository.GetByIdAsync(sellerId, cancellationToken);

        if (seller is null)
        {
            return null;
        }

        if (seller.Ledger.Any(entry =>
                entry.OrderId == orderId &&
                entry.OrderItemId == orderItemId &&
                entry.Type == SellerLedgerEntryType.Sale))
        {
            return seller;
        }

        seller.AccumulatedSalesAmount += grossAmount;
        seller.AccumulatedCommissionsAmount += commissionAmount;
        seller.Ledger.Add(new SellerLedgerEntry
        {
            OrderId = orderId,
            OrderItemId = orderItemId,
            GrossAmount = grossAmount,
            CommissionAmount = commissionAmount,
            Notes = notes
        });

        var updatedSeller = await _repository.UpdateAsync(seller, cancellationToken);

        await _publishEndpoint.Publish<SellerSaleRegisteredEvent>(new
        {
            SellerId = updatedSeller.Id,
            OrderId = orderId,
            OrderItemId = orderItemId,
            GrossAmount = grossAmount,
            CommissionAmount = commissionAmount,
            updatedSeller.AccumulatedSalesAmount,
            updatedSeller.AccumulatedCommissionsAmount,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

        return updatedSeller;
    }

    public async Task<Seller?> MarkSaleAsProcessedAsync(Guid sellerId, Guid entryId, CancellationToken cancellationToken)
    {
        var seller = await _repository.GetByIdAsync(sellerId, cancellationToken);
        if (seller is null)
        {
            return null;
        }

        var entry = seller.Ledger.FirstOrDefault(e => e.EntryId == entryId);
        if (entry is null)
        {
            return null;
        }

        entry.IsProcessed = true;
        entry.ProcessedAt = DateTime.UtcNow;

        return await _repository.UpdateAsync(seller, cancellationToken);
    }
}

using Domain.Common.Events.Sellers;
using Domain.Sellers.Contracts;
using Domain.Sellers.Entities;
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

    public Task<IReadOnlyList<Seller>> GetAllAsync(CancellationToken cancellationToken)
        => _repository.GetAllAsync(cancellationToken);

    public Task<Seller?> GetByIdAsync(Guid sellerId, CancellationToken cancellationToken)
        => _repository.GetByIdAsync(sellerId, cancellationToken);

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
}

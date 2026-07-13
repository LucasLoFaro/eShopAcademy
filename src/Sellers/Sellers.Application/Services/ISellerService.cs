using Domain.Sellers.Contracts;
using Domain.Sellers.Entities;
using Domain.Sellers.Enums;

namespace Sellers.Application.Services;

public interface ISellerService
{
    Task<Seller> CreateAsync(CreateSellerRequest request, CancellationToken cancellationToken);
    Task<Seller> RegisterAsync(string identityObjectId, RegisterSellerRequest request, CancellationToken cancellationToken);
    Task<Seller?> GetByIdentityAsync(string identityObjectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Seller>> GetAllAsync(CancellationToken cancellationToken);
    Task<Seller?> GetByIdAsync(Guid sellerId, CancellationToken cancellationToken);
    Task<Seller?> UpdateStatusAsync(Guid sellerId, SellerStatus status, CancellationToken cancellationToken);
    Task<Seller?> AssignPublishedProductsAsync(Guid sellerId, IEnumerable<Guid> productIds, CancellationToken cancellationToken);
    Task<Seller?> RegisterSaleAsync(Guid sellerId, Guid orderId, Guid orderItemId, decimal grossAmount, decimal commissionAmount, string notes, CancellationToken cancellationToken);
    Task<Seller?> MarkSaleAsProcessedAsync(Guid sellerId, Guid entryId, CancellationToken cancellationToken);
}

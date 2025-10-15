using Domain.Product.Entities;

namespace Core.Application.Interfaces.Services;

public interface IProductMessagingService
{
    public Task SendProductUpdate(Product product, CancellationToken ct = default);
    public Task SendProductDelete(Product product, CancellationToken ct = default);
}

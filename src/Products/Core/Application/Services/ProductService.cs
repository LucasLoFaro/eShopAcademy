using Core.Application.Interfaces.Data;
using Domain.Products.Contracts;
using Domain.Products.Entities;
using Core.Application.Interfaces.Services;


namespace Core.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductsRepository _productsRepository;
    private readonly IProductMessagingService _messaging;
    private readonly IContentModerationService _moderation;


    public ProductService(IProductsRepository productsRepository, IProductMessagingService messagingServiceClient, IContentModerationService moderation)
    {
        _productsRepository = productsRepository;
        _messaging = messagingServiceClient;
        _moderation = moderation;
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _productsRepository.GetAllAsync(cancellationToken);

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _productsRepository.GetByIdAsync(id, cancellationToken);

    public async Task<Product?> GetMostExpensive(CancellationToken cancellationToken = default)
        => await _productsRepository.GetMostExpensive(cancellationToken);

    // These two should send the stock integration events as well
    public async Task AddOrUpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        var moderationResult = await _moderation.ModerateProductAsync(product, cancellationToken);
        if (!moderationResult.IsApproved)
            throw new InvalidOperationException($"Product content moderation failed: {moderationResult.RejectionReason}");

        var isNew = await _productsRepository.GetByIdAsync(product.Id, cancellationToken) is null;

        await _productsRepository.AddOrUpdateAsync(product, cancellationToken);
        await _messaging.SendProductUpdate(product, cancellationToken);

        if (isNew)
        {
            await _messaging.SendProductPublished(product, cancellationToken);
        }
    }

    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _productsRepository.DeleteAsync(product, cancellationToken);
        await _messaging.SendProductDelete(product, cancellationToken);
    }

    public async Task<PagedResult<Product>> SearchAsync(ProductSearchFilter filter, CancellationToken cancellationToken = default)
        => await _productsRepository.SearchAsync(filter, cancellationToken);
}

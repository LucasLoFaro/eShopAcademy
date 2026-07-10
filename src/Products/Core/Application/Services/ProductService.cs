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

    public async Task<IEnumerable<Product>> GetAllAsync()
        => await _productsRepository.GetAllAsync();

    public async Task<Product?> GetByIdAsync(Guid id)
        => await _productsRepository.GetByIdAsync(id);

    public async Task<Product?> GetMostExpensive()
        => await _productsRepository.GetMostExpensive();

    // These two should send the stock integration events as well
    public async Task AddOrUpdateAsync(Product product)
    {
        var moderationResult = await _moderation.ModerateProductAsync(product);
        if (!moderationResult.IsApproved)
            throw new InvalidOperationException($"Product content moderation failed: {moderationResult.RejectionReason}");

        await _productsRepository.AddOrUpdateAsync(product);
        await _messaging.SendProductUpdate(product);
    }

    public async Task DeleteAsync(Product product)
    {
        await _productsRepository.DeleteAsync(product);
        await _messaging.SendProductDelete(product);
    }

    public async Task<PagedResult<Product>> SearchAsync(ProductSearchFilter filter)
        => await _productsRepository.SearchAsync(filter);
}

using Core.Application.Interfaces.Data;
using Core.Domain.Entities;
using Core.Application.Interfaces.Services;


namespace Core.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductsRepository _productsRepository;
        private readonly IMessagingServiceClient _messaging;


        public ProductService(IProductsRepository productsRepository, IMessagingServiceClient messagingServiceClient)
        {
            _productsRepository = productsRepository;
            _messaging = messagingServiceClient;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _productsRepository.GetAllAsync();
        }

        public async Task<Product> GetByIdAsync(Guid id)
        {
            return await _productsRepository.GetByIdAsync(id);
        }

        public async Task<Product> GetMostExpensive()
        {
            return await _productsRepository.GetMostExpensive();
        }

        // These two should send the stock integration events as well
        public async Task AddOrUpdateAsync(Product product)
        {
            await _productsRepository.AddAsync(product);
            await _messaging.SendProductUpdate(product);
        }

        public async Task DeleteAsync(Product product)
        {
            await _productsRepository.DeleteAsync(product);
            await _messaging.SendProductDelete(product);

        }
    }
}

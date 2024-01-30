using Application.Interfaces.Data;
using Application.Interfaces.Services;
using Domain.Entities;

namespace Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductsRepository productsRepository;
        public ProductService(IProductsRepository _productsRepository)
        {
            productsRepository = _productsRepository;
        }

        async Task<IEnumerable<Product>> IProductService.ListProducts()
        {
            return await productsRepository.GetAllAsync();
        }
    }
}

using Application.Interfaces.Data;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.DTOs;
using Application.Interfaces.Services;

namespace Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductsRepository productsRepository;
        private readonly IBlobStorageClient storageClient;

        public ProductService(IProductsRepository _productsRepository, IBlobStorageClient _storageClient)
        {
            productsRepository = _productsRepository;
            storageClient = _storageClient;
        }

        async Task<Product> IProductService.GetMostExpensiveProduct()
        {
            return await productsRepository.GetMostExpensive();
        }
        public async Task Create(ProductWithImage product)
        {
            Product p = new Product
            {
                CategoryDescription = product.CategoryDescription,
                Description = product.Description,
                Image = product.Image,
                Name = product.Name,
                Price = product.Price,
                ProductId = product.ProductId,
            };

            await productsRepository.AddAsync(p);
            try
            {
                await storageClient.UploadBlob(product.Image, product.File);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"No se pudo subir la imagen del producto", ex);
            }
        }
    }
}

using Core.Application.Interfaces.Services;
using Core.Application.Services;
using Grpc.Core;


namespace gRPC.Services;

public class ProductGrpcService : ProductService.ProductServiceBase
{
    private readonly IProductService _productService;

    public ProductGrpcService(IProductService productService)
    {
        _productService = productService;
    }

    public override async Task<ProductResponse> GetProductById(ProductRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var productId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid GUID format"));
        }

        var product = await _productService.GetByIdAsync(productId);
        if (product is null)
            throw new RpcException(new Status(StatusCode.NotFound, "Product not found"));
        
        return new ProductResponse
        {
            Id = product.Id.ToString(),
            Name = product.Name,
            Price = (double)product.Price,
            Description = product.Description ?? "",
            ImageUrl = product.ImageUrl ?? "",
            CategoryId = product.CategoryId,
            SellerId = product.SellerId.ToString()
        };
    }
}

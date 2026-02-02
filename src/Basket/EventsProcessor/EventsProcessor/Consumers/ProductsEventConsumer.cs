using AutoMapper;
using Common.Domain.Events.Products;
using Data.Interfaces;
using Domain.Basket.Contracts;
using MassTransit;


namespace Consumers;

public class ProductsEventConsumer : IConsumer<ProductUpdatedEvent>
{
    private readonly IProductCache _productCache;
    private readonly IMapper _mapper;

    public ProductsEventConsumer(IProductCache productRepository, IMapper mapper)
    {
        _productCache = productRepository;
        _mapper = mapper;
    }

    public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
    {
        await _productCache.AddOrUpdateProduct(_mapper.Map<ProductDTO>(context.Message));
    }
}

using Core.Domain.Contracts;
using Core.Domain.Events;
using Data.Interfaces;
using MassTransit;
using AutoMapper;


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

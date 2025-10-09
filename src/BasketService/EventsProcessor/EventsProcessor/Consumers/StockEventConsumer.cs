using Core.Domain.Events;
using Data.Interfaces;
using Domain.DTOs;
using MassTransit;
using AutoMapper;


namespace Consumers;

public class StockEventConsumer : IConsumer<StockUpdatedEvent>
{
    private readonly IProductCache _productCache;
    private readonly IMapper _mapper;

    public StockEventConsumer(IProductCache productRepository, IMapper mapper)
    {
        _productCache = productRepository;
        _mapper = mapper;
    }

    public async Task Consume(ConsumeContext<StockUpdatedEvent> context)
    {
        await _productCache.UpdateProductStock(_mapper.Map<AlterStockDTO>(context.Message));
    }
}

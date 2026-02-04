using AutoMapper;
using Common.Domain.Events.Stock;
using Data.Interfaces;
using Domain.Basket.Contracts;
using MassTransit;


namespace Basket.EventsProcessor.Consumers;

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

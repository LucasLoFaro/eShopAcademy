using Core.Domain.Events;
using Data.Interfaces;
using MassTransit;
using AutoMapper;
using Domain.DTOs;

namespace EventsProcessor.Consumers
{
    public class StockEventConsumer : IConsumer<StockUpdatedEvent>
    {
        private readonly ILogger _logger;
        private readonly IProductCache _productCache;
        private readonly IMapper _mapper;

        public StockEventConsumer(ILoggerFactory loggerFactory, IProductCache productRepository, IMapper mapper)
        {
            _logger = loggerFactory.CreateLogger<StockEventConsumer>();
            _productCache = productRepository;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<StockUpdatedEvent> context)
        {
            await _productCache.UpdateProductStock(_mapper.Map<AlterStockDTO>(context.Message));
        }
    }
}

using Core.Domain.Events;
using Data.Interfaces;
using MassTransit;
using Core.Domain.DTOs;
using AutoMapper;

namespace EventsProcessor.Consumers
{
    public class ProductsEventConsumer : IConsumer<ProductUpdatedEvent>
    {
        private readonly ILogger _logger;
        private readonly IProductCache _productCache;
        private readonly IMapper _mapper;

        public ProductsEventConsumer(ILoggerFactory loggerFactory, IProductCache productRepository, IMapper mapper)
        {
            _logger = loggerFactory.CreateLogger<ProductsEventConsumer>();
            _productCache = productRepository;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
        {
            await _productCache.AddOrUpdateProduct(_mapper.Map<ProductDTO>(context.Message));
        }
    }
}

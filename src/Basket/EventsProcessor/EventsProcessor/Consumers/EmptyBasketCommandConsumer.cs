using Domain.Common.Commands.Basket;
using Data.Interfaces;
using MassTransit;


namespace Basket.EventsProcessor.Consumers;

public class EmptyBasketCommandConsumer : IConsumer<EmptyBasketCommand>
{
    private readonly IBasketCache _basketCache;

    public EmptyBasketCommandConsumer(IBasketCache basketCache)
    {
        _basketCache = basketCache;
    }

    public async Task Consume(ConsumeContext<EmptyBasketCommand> context)
    {
        if (context.Message.ClientId == Guid.Empty)
        {
            throw new ArgumentException("Client identifier is required.");
        }

        await _basketCache.EmptyBasket(context.Message.ClientId, context.CancellationToken);
    }
}

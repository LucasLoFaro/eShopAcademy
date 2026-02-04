using Common.Domain.Commands.Basket;
using Data.Interfaces;
using Domain.Basket.Entities;
using MassTransit;


namespace Basket.EventsProcessor.Consumers;

public class ReinstateBasketCommandConsumer : IConsumer<ReinstateBasketCommand>
{
    private readonly IBasketCache _basketCache;

    public ReinstateBasketCommandConsumer(IBasketCache basketCache)
    {
        _basketCache = basketCache;
    }

    public async Task Consume(ConsumeContext<ReinstateBasketCommand> context)
    {
        var command = context.Message;
        var items = command.Items.Select(item => new Item
        {
            ProductID = item.ProductID,
            Quantity = item.Quantity
        }).ToList();

        await _basketCache.ReinstateBasket(command.ClientId, items);
    }
}

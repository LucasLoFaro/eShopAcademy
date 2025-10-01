using Core.Domain.Events;
using Core.Domain.DTOs;
using MassTransit;


namespace Infrastructure.Services;

public class StockMessagingClient
{
    private readonly IPublishEndpoint _publishEndpoint;

    public StockMessagingClient(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }


    public async Task SendStockUpdate(AlterStockDTO stock, CancellationToken ct = default)
    {
        var command = new StockUpdatedEvent
        {
            Stock = new AlterStockDTO
            {
                ProductGuid = stock.ProductGuid,
                Quantity = stock.Quantity,
                Warehouse = stock.Warehouse
            }
        };

        await _publishEndpoint.Publish(command, ct);
    }
}
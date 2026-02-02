using Common.Domain.Events.Stock;
using Domain.Stock.Contracts;
using MassTransit;


namespace Infrastructure.Services;

public class StockMessagingClient
{
    private readonly IPublishEndpoint _publishEndpoint;

    public StockMessagingClient(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }


    public async Task SendStockUpdate(AlterStockRequest stock, CancellationToken ct = default)
    {
        var command = new StockUpdatedEvent
        {
            ProductId = stock.ProductGuid,
            Quantity = stock.Quantity,
            WarehouseId = stock.Warehouse
        };

        await _publishEndpoint.Publish(command, ct);
    }

    public async Task SendStockReserved(ReserveStockRequest request, Guid reservationId, CancellationToken ct = default)
    {
        var command = new StockReservationCreatedEvent
        {
            OrderId = request.OrderId,
            ReservationId = reservationId
        };

        await _publishEndpoint.Publish(command, ct);
    }

    public async Task SendStockReservationCommitted(ReserveStockRequest request, Guid reservationId, CancellationToken ct = default)
    {
        var command = new StockReservationCommittedEvent
        {
            OrderId = request.OrderId,
            ReservationId = reservationId
        };

        await _publishEndpoint.Publish(command, ct);
    }

    public async Task SendStockReleased(Guid orderId, Guid reservationId, string reason, CancellationToken ct = default)
    {
        var command = new StockReleasedEvent
        {
            OrderId = orderId,
            ReservationId = reservationId,
            Reason = reason
        };

        await _publishEndpoint.Publish(command, ct);
    }
}
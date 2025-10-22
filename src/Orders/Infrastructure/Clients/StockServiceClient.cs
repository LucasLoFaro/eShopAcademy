using DomainContracts = Domain.Orders.Contracts;
using Google.Protobuf.WellKnownTypes;
using Core.Application.Interfaces;
using Domain.Orders.Entities;
using Protos;


namespace Infrastructure.Clients;

public class StockServiceClient : IStockServiceClient
{
    private readonly StockProtoService.StockProtoServiceClient _client;

    public StockServiceClient(StockProtoService.StockProtoServiceClient client)
    {
        _client = client;
    }

    public async Task<DomainContracts.ReserveStockResponse> ReserveStockAsync(Guid orderId, List<Item> items, CancellationToken ct)
    {
        var request = new ReserveStockRequest
        {
            OrderId = orderId.ToString(),
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow.ToUniversalTime())
        };

        foreach (var i in items)
        {
            var item = new StockItemModel
            {
                ProductGuid = i.ProductID.ToString(),
                Quantity = i.Quantity
            };

            // Create a ReservationItemModel wrapper if required
            var reservationItem = new ReservationItemModel();
            reservationItem.Items.Add(item);
            request.Stock.Add(reservationItem);
        }

        var response = await _client.ReserveStockAsync(request, cancellationToken: ct);
        return new()
        {
            ReservationId = Guid.TryParse(response.ReservationId, out var resId) ? resId : null,
            OutOfStockProducts = response.OutOfStockProducts.ToList(),
            CreatedAt = response.CreatedAt.ToDateTime(),
            ValidUntil = response.ValidUntil.ToDateTime()
        };
    }

    public async Task<DomainContracts.CommitReservationResponse> CommitReservationAsync(Guid orderId, Guid reservationId, CancellationToken ct)
    {
        var response = await _client.CommitReservationAsync(new CommitReservationRequest
        {
            OrderId = orderId.ToString(),
            ReservationId = reservationId.ToString()
        }, cancellationToken: ct);

        return new()
        {
            OrderId = Guid.Parse(response.OrderId),
            ReservationId = Guid.Parse(response.ReservationId),
            Success = response.Success
        };
    }

    public async Task<DomainContracts.CancelReservationResponse> CancelReservationAsync(Guid orderId, Guid reservationId, CancellationToken ct)
    {
        var response = await _client.CancelReservationAsync(new CancelReservationRequest
        {
            OrderId = orderId.ToString(),
            ReservationId = reservationId.ToString()
        }, cancellationToken: ct);

        return new()
        {
            OrderId = Guid.Parse(response.OrderId),
            ReservationId = Guid.Parse(response.ReservationId),
            Success = response.Success,
            Reason = response.Reason
        };
    }
}
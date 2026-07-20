using Domain.Common.Commands.Stock;
using Infrastructure.Data;
using Infrastructure.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stock.Messaging.Processor.Consumers;

public sealed class ReleaseStockReservationConsumer : IConsumer<ReleaseStockReservationCommand>
{
    private readonly IStockReservationRepository _reservationRepository;
    private readonly IStockRepository _stockRepository;
    private readonly StockMessagingClient _messagingClient;
    private readonly ILogger<ReleaseStockReservationConsumer> _logger;

    public ReleaseStockReservationConsumer(
        IStockReservationRepository reservationRepository,
        IStockRepository stockRepository,
        StockMessagingClient messagingClient)
        : this(reservationRepository, stockRepository, messagingClient, NullLogger<ReleaseStockReservationConsumer>.Instance)
    {
    }

    public ReleaseStockReservationConsumer(
        IStockReservationRepository reservationRepository,
        IStockRepository stockRepository,
        StockMessagingClient messagingClient,
        ILogger<ReleaseStockReservationConsumer> logger)
    {
        _reservationRepository = reservationRepository;
        _stockRepository = stockRepository;
        _messagingClient = messagingClient;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReleaseStockReservationCommand> context)
    {
        var command = context.Message;
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["OrderId"] = command.OrderId,
            ["ReservationId"] = command.ReservationId
        });
        var reservation = await _reservationRepository.GetByIdAsync(command.ReservationId, context.CancellationToken);

        if (reservation == null)
        {
            StockMetrics.RecordFailure("release", "not_found");
            return;
        }

        if (reservation.IsCommitted || reservation.CommittedAt.HasValue)
        {
            return;
        }

        if (reservation.ValidUntil < DateTime.UtcNow)
        {
            StockMetrics.RecordFailure("release", "expired");
            return;
        }

        foreach (var resItem in reservation.Items)
        {
            foreach (var item in resItem.Items)
            {
                var stock = await _stockRepository.GetByProductIdAsync(item.ProductID, context.CancellationToken);
                if (stock != null)
                {
                    stock.Quantity += item.Quantity;
                    await _stockRepository.AddOrUpdateAsync(stock, context.CancellationToken);
                }
            }
        }

        reservation.IsCommitted = false;
        reservation.CommittedAt = DateTime.UtcNow;
        await _reservationRepository.UpdateAsync(reservation, context.CancellationToken);
        await _messagingClient.SendStockReleased(reservation.OrderId, reservation.Id, command.Reason, context.CancellationToken);
    }
}

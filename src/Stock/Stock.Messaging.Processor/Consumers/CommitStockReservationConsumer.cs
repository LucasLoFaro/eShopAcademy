using Domain.Common.Commands.Stock;
using Domain.Common.Events.Stock;
using Domain.Stock.Entities;
using Domain.Stock.Contracts;
using Infrastructure.Services;
using Infrastructure.Data;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;


namespace Stock.Messaging.Processor.Consumers;

public sealed class CommitStockReservationConsumer : IConsumer<CommitStockReservationCommand>
{
    private readonly IStockReservationRepository _reservationRepository;
    private readonly StockMessagingClient _messagingClient;
    private readonly ILogger<CommitStockReservationConsumer> _logger;

    public CommitStockReservationConsumer(
        IStockReservationRepository reservationRepository,
        StockMessagingClient messagingClient)
        : this(reservationRepository, messagingClient, NullLogger<CommitStockReservationConsumer>.Instance)
    {
    }

    public CommitStockReservationConsumer(
        IStockReservationRepository reservationRepository,
        StockMessagingClient messagingClient,
        ILogger<CommitStockReservationConsumer> logger)
    {
        _reservationRepository = reservationRepository;
        _messagingClient = messagingClient;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CommitStockReservationCommand> context)
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
            StockMetrics.RecordFailure("commit", "not_found");
            await PublishCommitFailed(context, command, "Reservation not found.");
            return;
        }

        if (reservation.IsCommitted)
        {
            _logger.LogDebug("Ignoring duplicate stock reservation commit.");
            return;
        }

        if (reservation.CommittedAt.HasValue)
        {
            StockMetrics.RecordFailure("commit", "released");
            await PublishCommitFailed(context, command, "Reservation already released.");
            return;
        }

        if (reservation.ValidUntil < DateTime.UtcNow)
        {
            StockMetrics.RecordFailure("commit", "expired");
            await PublishCommitFailed(context, command, "Reservation expired.");
            return;
        }

        reservation.IsCommitted = true;
        reservation.CommittedAt = DateTime.UtcNow;
        await _reservationRepository.UpdateAsync(reservation, context.CancellationToken);

        var reserveRequest = new ReserveStockRequest
        {
            OrderId = command.OrderId,
            Stock = reservation.Items
                .SelectMany(i => i.Items.Select(item => new Domain.Stock.Entities.Stock
                {
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    Warehouse = i.Warehouse
                }))
                .ToList()
        };

        await _messagingClient.SendStockReservationCommitted(reserveRequest, reservation.Id, context.CancellationToken);
    }

    private static Task PublishCommitFailed(
        ConsumeContext<CommitStockReservationCommand> context,
        CommitStockReservationCommand command,
        string reason)
    {
        return context.Publish(new StockReservationCommitFailedEvent
        {
            OrderId = command.OrderId,
            ReservationId = command.ReservationId,
            Reason = reason
        }, context.CancellationToken);
    }
}

using Common.Domain.Commands.Stock;
using Common.Domain.Events.Stock;
using Domain.Stock.Entities;
using Domain.Stock.Contracts;
using Infrastructure.Services;
using Infrastructure.Data;
using MassTransit;


namespace Stock.Messaging.Processor.Consumers;

public sealed class CommitStockReservationConsumer : IConsumer<CommitStockReservationCommand>
{
    private readonly IStockReservationRepository _reservationRepository;
    private readonly StockMessagingClient _messagingClient;

    public CommitStockReservationConsumer(
        IStockReservationRepository reservationRepository,
        StockMessagingClient messagingClient)
    {
        _reservationRepository = reservationRepository;
        _messagingClient = messagingClient;
    }

    public async Task Consume(ConsumeContext<CommitStockReservationCommand> context)
    {
        var command = context.Message;
        var reservation = await _reservationRepository.GetByIdAsync(command.ReservationId, context.CancellationToken);

        if (reservation == null)
        {
            await PublishCommitFailed(context, command, "Reservation not found.");
            return;
        }

        if (reservation.IsCommitted)
        {
            await PublishCommitFailed(context, command, "Reservation already committed.");
            return;
        }

        if (reservation.ValidUntil < DateTime.UtcNow)
        {
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

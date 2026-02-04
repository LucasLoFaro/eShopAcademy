using Domain.Common.Commands.Stock;
using Infrastructure.Data;
using Infrastructure.Services;
using MassTransit;

namespace Stock.Messaging.Processor.Consumers;

public sealed class ReleaseStockReservationConsumer : IConsumer<ReleaseStockReservationCommand>
{
    private readonly IStockReservationRepository _reservationRepository;
    private readonly IStockRepository _stockRepository;
    private readonly StockMessagingClient _messagingClient;

    public ReleaseStockReservationConsumer(
        IStockReservationRepository reservationRepository,
        IStockRepository stockRepository,
        StockMessagingClient messagingClient)
    {
        _reservationRepository = reservationRepository;
        _stockRepository = stockRepository;
        _messagingClient = messagingClient;
    }

    public async Task Consume(ConsumeContext<ReleaseStockReservationCommand> context)
    {
        var command = context.Message;
        var reservation = await _reservationRepository.GetByIdAsync(command.ReservationId, context.CancellationToken);

        if (reservation == null)
        {
            return;
        }

        if (reservation.IsCommitted)
        {
            return;
        }

        if (reservation.ValidUntil < DateTime.UtcNow)
        {
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

        await _messagingClient.SendStockReleased(reservation.OrderId, reservation.Id, command.Reason, context.CancellationToken);

        reservation.IsCommitted = false;
        await _reservationRepository.UpdateAsync(reservation, context.CancellationToken);
    }
}

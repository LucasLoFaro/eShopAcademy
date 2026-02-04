using Common.Domain.Commands.Operations;
using MassTransit;

namespace Operations.Service.Consumers;

public class PreparePackageCommandConsumer : IConsumer<PreparePackageCommand>
{
    private readonly ILogger<PreparePackageCommandConsumer> _logger;

    public PreparePackageCommandConsumer(ILogger<PreparePackageCommandConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<PreparePackageCommand> context)
    {
        var message = context.Message;

        // TODO: Save to DB and publish a notification to the admin frontend.

        _logger.LogInformation(
            "[Operations] Prepare package requested for Order {OrderId}, Reservation {ReservationId}",
            message.OrderId,
            message.ReservationId);

        return Task.CompletedTask;
    }
}
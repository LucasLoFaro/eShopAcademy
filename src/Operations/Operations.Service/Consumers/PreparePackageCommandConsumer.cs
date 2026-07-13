using Operations.Application.Repositories;
using Domain.Common.Commands.Operations;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using MassTransit;


namespace Operations.Service.Consumers;

public class PreparePackageCommandConsumer : IConsumer<PreparePackageCommand>
{
    private readonly ILogger<PreparePackageCommandConsumer> _logger;
    private readonly IPackageRepository _repository;

    public PreparePackageCommandConsumer(
        ILogger<PreparePackageCommandConsumer> logger,
        IPackageRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task Consume(ConsumeContext<PreparePackageCommand> context)
    {
        var message = context.Message;

        var package = new Package
        {
            OrderId = message.OrderId,
            ReservationId = message.ReservationId,
            SellerId = message.SellerId,
            CustomerName = message.CustomerName,
            CustomerEmail = message.CustomerEmail,
            Status = PackageStatus.Pending,
            PreparedAt = DateTime.UtcNow,
            Items = message.Items.Select(i => new PackageItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity
            }).ToList()
        };

        await _repository.CreateOrUpdateAsync(package, context.CancellationToken);

        _logger.LogInformation(
            "[Operations] Prepare package requested for Order {OrderId}, Reservation {ReservationId}",
            message.OrderId,
            message.ReservationId);
    }
}

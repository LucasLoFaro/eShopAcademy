using Domain.Common.Events.Orders;
using Domain.Operations.Entities;
using MassTransit;
using Operations.Application.Repositories;

namespace Operations.Service.Consumers;

public class PackageSellerStampConsumer : IConsumer<OrderSellerSaleRegistrationRequestedEvent>
{
    private readonly ILogger<PackageSellerStampConsumer> _logger;
    private readonly IPackageRepository _repository;

    public PackageSellerStampConsumer(
        ILogger<PackageSellerStampConsumer> logger,
        IPackageRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task Consume(ConsumeContext<OrderSellerSaleRegistrationRequestedEvent> context)
    {
        var message = context.Message;

        var package = await _repository.GetByOrderIdAsync(message.OrderId, context.CancellationToken);
        if (package is null)
        {
            _logger.LogWarning(
                "[Operations] No package found for order {OrderId} when stamping seller {SellerId}.",
                message.OrderId, message.SellerId);
            return;
        }

        var updated = false;

        if (package.SellerId is null)
        {
            package.SellerId = message.SellerId;
            updated = true;
        }

        if (string.IsNullOrWhiteSpace(package.CustomerName) && !string.IsNullOrWhiteSpace(message.CustomerName))
        {
            package.CustomerName = message.CustomerName;
            updated = true;
        }

        if (string.IsNullOrWhiteSpace(package.CustomerEmail) && !string.IsNullOrWhiteSpace(message.CustomerEmail))
        {
            package.CustomerEmail = message.CustomerEmail;
            updated = true;
        }

        if (message.ProductId != Guid.Empty &&
            !package.Items.Any(i => i.ProductId == message.ProductId))
        {
            package.Items.Add(new PackageItem
            {
                ProductId = message.ProductId,
                ProductName = message.ProductName,
                Quantity = message.Quantity
            });
            updated = true;
        }

        if (!updated)
            return;

        await _repository.CreateOrUpdateAsync(package, context.CancellationToken);

        _logger.LogInformation(
            "[Operations] Stamped seller {SellerId} on package for order {OrderId}.",
            message.SellerId, message.OrderId);
    }
}

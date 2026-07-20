using Domain.Common.Events.Orders;
using Domain.Shipping.Contracts.Requests;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shipping.Application.Clients;
using Shipping.Application.Data;
using Shipping.Application.Services;
using Xunit;

namespace Shipping.Tests.Services;

public sealed class ShippingStatusServiceTests
{
    private readonly Mock<IShippingStatusHistoryRepository> _repository = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<IShippingProviderClient> _providerClient = new();

    [Theory]
    [InlineData("shipped")]
    [InlineData("out_for_delivery")]
    [InlineData(" OUT_FOR_DELIVERY ")]
    public async Task ProcessStatusUpdateAsync_WhenShipmentIsInTransit_PublishesOrderShippedEvent(string status)
    {
        var update = CreateUpdate(status);

        await CreateSut().ProcessStatusUpdateAsync(update);

        _publishEndpoint.Verify(p => p.Publish(
            It.Is<OrderShippedEvent>(e =>
                e.OrderId == update.OrderId &&
                e.TrackingNumber == update.TrackingNumber &&
                e.Carrier == update.Carrier &&
                e.ShippedAt == update.OccurredAt),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessStatusUpdateAsync_WhenDelivered_PublishesOrderDeliveredEventOnly()
    {
        var update = CreateUpdate("delivered");

        await CreateSut().ProcessStatusUpdateAsync(update);

        _publishEndpoint.Verify(p => p.Publish(
            It.Is<OrderDeliveredEvent>(e =>
                e.OrderId == update.OrderId &&
                e.TrackingNumber == update.TrackingNumber &&
                e.DeliveredAt == update.OccurredAt),
            It.IsAny<CancellationToken>()), Times.Once);
        _publishEndpoint.Verify(p => p.Publish(
            It.IsAny<OrderShippedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private ShippingStatusService CreateSut() => new(
        _repository.Object,
        _publishEndpoint.Object,
        _providerClient.Object,
        NullLogger<ShippingStatusService>.Instance);

    private static ShippingStatusUpdateRequest CreateUpdate(string status) => new()
    {
        ShippingId = Guid.NewGuid(),
        OrderId = Guid.NewGuid(),
        Status = status,
        TrackingNumber = "tracking-123",
        Carrier = "ShipSim",
        OccurredAt = DateTime.UtcNow
    };
}

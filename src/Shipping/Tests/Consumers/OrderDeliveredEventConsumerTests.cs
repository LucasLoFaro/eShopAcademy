using AutoFixture.Xunit2;
using Domain.Common.Events.Orders;
using Domain.Shipping.Entities;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shipping.Application.Data;
using Shipping.Service.Consumers;
using Xunit;

namespace Shipping.Tests.Consumers;

public class OrderDeliveredEventConsumerTests
{
    private readonly Mock<IShippingInfoRepository> _infoRepo = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();

    private OrderDeliveredEventConsumer CreateSut() =>
        new(_infoRepo.Object, _publishEndpoint.Object, NullLogger<OrderDeliveredEventConsumer>.Instance);

    [Theory]
    [AutoData]
    public async Task Consume_WhenEventAlreadyHasEmail_ReturnsEarlyWithoutPublishing(OrderDeliveredEvent evt)
    {
        // Arrange: incoming event has a non-empty email — no enrichment needed
        var enrichedEvt = evt with { CustomerEmail = "existing@example.com" };
        var context = new Mock<ConsumeContext<OrderDeliveredEvent>>();
        context.Setup(c => c.Message).Returns(enrichedEvt);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: no lookup and no re-publish
        _infoRepo.Verify(r => r.GetByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _publishEndpoint.Verify(p => p.Publish(It.IsAny<OrderDeliveredEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenEventLacksEmail_EnrichesFromRepoAndPublishes(OrderDeliveredEvent evt)
    {
        // Arrange: incoming event has no email, repo has it
        var missingEmailEvt = evt with { CustomerEmail = string.Empty };
        var shippingInfo = new ShippingInfo
        {
            OrderId = evt.OrderId,
            CustomerEmail = "found@example.com",
            CustomerName = "John Doe"
        };

        _infoRepo.Setup(r => r.GetByOrderIdAsync(evt.OrderId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(shippingInfo);
        _publishEndpoint.Setup(p => p.Publish(It.IsAny<OrderDeliveredEvent>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        var context = new Mock<ConsumeContext<OrderDeliveredEvent>>();
        context.Setup(c => c.Message).Returns(missingEmailEvt);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: enriched event published with email from repo
        _publishEndpoint.Verify(p => p.Publish(
            It.Is<OrderDeliveredEvent>(e =>
                e.OrderId == evt.OrderId &&
                e.CustomerEmail == shippingInfo.CustomerEmail),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenEventLacksEmailAndRepoReturnsNull_DoesNotPublish(OrderDeliveredEvent evt)
    {
        // Arrange: no email in event and no record in repo
        var missingEmailEvt = evt with { CustomerEmail = string.Empty };
        _infoRepo.Setup(r => r.GetByOrderIdAsync(evt.OrderId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((ShippingInfo?)null);

        var context = new Mock<ConsumeContext<OrderDeliveredEvent>>();
        context.Setup(c => c.Message).Returns(missingEmailEvt);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await CreateSut().Consume(context.Object);

        // Assert: nothing published
        _publishEndpoint.Verify(p => p.Publish(It.IsAny<OrderDeliveredEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

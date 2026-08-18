using System.Text.Json;
using Domain.Common.Events.Orders;
using FluentAssertions;
using Xunit;

namespace Orders.Tests.Messaging;

public class SellerAttributionContractTests
{
    private static readonly Guid OrderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrderItemId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProductId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid SellerId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void CreateOperationId_IsStableForAnOrderItem()
    {
        var first = SellerAttributionContract.CreateOperationId(OrderId, OrderItemId);
        var replay = SellerAttributionContract.CreateOperationId(OrderId, OrderItemId);
        var otherItem = SellerAttributionContract.CreateOperationId(OrderId, Guid.NewGuid());

        first.Should().Be(replay);
        first.Should().Be(Guid.Parse("b6f1cfa1-0333-4749-3547-4c6f040e6c23"));
        first.Should().NotBe(otherItem);
    }

    [Fact]
    public void Validate_RejectsMissingOrMismatchedAttribution()
    {
        var missingSeller = CreateSubmittedEvent(new OrderItemSellerAttribution
        {
            OrderItemId = OrderItemId,
            ProductId = ProductId,
            SellerSaleOperationId = SellerAttributionContract.CreateOperationId(OrderId, OrderItemId)
        });
        var mismatchedOperation = CreateSubmittedEvent(new OrderItemSellerAttribution
        {
            OrderItemId = OrderItemId,
            ProductId = ProductId,
            SellerId = SellerId,
            SellerSaleOperationId = Guid.NewGuid()
        });

        var missingSellerAction = () => SellerAttributionContract.Validate(missingSeller);
        var mismatchedOperationAction = () => SellerAttributionContract.Validate(mismatchedOperation);

        missingSellerAction.Should().Throw<ArgumentException>();
        mismatchedOperationAction.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OrderSubmittedEvent_SellerAttributionRoundTrips()
    {
        var operationId = SellerAttributionContract.CreateOperationId(OrderId, OrderItemId);
        var message = CreateSubmittedEvent(new OrderItemSellerAttribution
        {
            OrderItemId = OrderItemId,
            ProductId = ProductId,
            SellerId = SellerId,
            SellerSaleOperationId = operationId
        });

        var json = JsonSerializer.Serialize(message);
        var roundTrip = JsonSerializer.Deserialize<OrderSubmittedEvent>(json);

        roundTrip.Should().NotBeNull();
        roundTrip!.SellerAttributions.Should().ContainSingle().Which.Should().BeEquivalentTo(
            message.SellerAttributions.Single());
        SellerAttributionContract.Validate(roundTrip);
    }

    [Fact]
    public void OrderSubmittedEvent_OlderPayloadWithoutAttributionRemainsCompatible()
    {
        const string olderPayload =
            """
            {
              "OrderId": "11111111-1111-1111-1111-111111111111",
              "EventId": "55555555-5555-5555-5555-555555555555"
            }
            """;

        var message = JsonSerializer.Deserialize<OrderSubmittedEvent>(olderPayload);

        message.Should().NotBeNull();
        message!.SellerAttributions.Should().BeEmpty();
        SellerAttributionContract.Validate(message);
    }

    [Fact]
    public void SellerSaleRequest_RoundTripsStableOperationId()
    {
        var operationId = SellerAttributionContract.CreateOperationId(OrderId, OrderItemId);
        var message = new OrderSellerSaleRegistrationRequestedEvent
        {
            OrderId = OrderId,
            OrderItemId = OrderItemId,
            ProductId = ProductId,
            SellerId = SellerId,
            SellerSaleOperationId = operationId
        };

        var json = JsonSerializer.Serialize(message);
        var roundTrip = JsonSerializer.Deserialize<OrderSellerSaleRegistrationRequestedEvent>(json);

        roundTrip.Should().NotBeNull();
        roundTrip!.SellerSaleOperationId.Should().Be(operationId);
        SellerAttributionContract.Validate(roundTrip);
    }

    private static OrderSubmittedEvent CreateSubmittedEvent(OrderItemSellerAttribution attribution) =>
        new()
        {
            OrderId = OrderId,
            SellerAttributions = [attribution]
        };
}

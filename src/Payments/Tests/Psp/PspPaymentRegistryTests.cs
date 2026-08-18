using Domain.Payments.Contracts;
using FluentAssertions;
using Psp.Simulator;
using Xunit;

namespace Payments.Tests.Psp;

public sealed class PspPaymentRegistryTests
{
    [Fact]
    public void Register_DuplicateOrder_ReturnsSameProviderTransaction()
    {
        var registry = new PspPaymentRegistry();
        var request = Request(amount: 10);
        var notification = new Uri("https://payments.example/webhook");

        var first = registry.Register(request, notification);
        var duplicate = registry.Register(request, notification);

        duplicate.Conflict.Should().BeFalse();
        duplicate.Payment.Id.Should().Be(first.Payment.Id);
    }

    [Fact]
    public void Register_ReusedKeyWithDifferentFinancialPayload_IsRejected()
    {
        var registry = new PspPaymentRegistry();
        var request = Request(amount: 10);
        registry.Register(request, new Uri("https://payments.example/webhook"));

        var conflict = registry.Register(
            Request(amount: 20, externalId: request.ExternalId),
            new Uri("https://payments.example/webhook"));

        conflict.Conflict.Should().BeTrue();
    }

    private static PaymentRequest Request(double amount, string? externalId = null) => new()
    {
        ExternalId = externalId ?? Guid.NewGuid().ToString(),
        Amount = amount,
        Currency = "USD",
        NotificationUrl = "https://payments.example/webhook"
    };
}

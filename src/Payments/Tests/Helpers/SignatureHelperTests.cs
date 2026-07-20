using Domain.Payments.Contracts;
using FluentAssertions;
using Infrastructure.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Infrastructure.Configuration;
using Xunit;

namespace Payments.Tests.Helpers;

public class SignatureHelperTests
{
    private const string Secret = "test-secret-key-1234567890abcdef";

    private static SignatureHelper CreateSut(string secret = Secret)
    {
        return new SignatureHelper(Options.Create(new PaymentSecurityOptions
        {
            SignatureSecret = secret
        }));
    }

    private static PaymentNotification BuildNotification() => new()
    {
        ExternalId = Guid.NewGuid().ToString(),
        Status = "completed",
        Amount = 10.0,
        Currency = "USD"
    };

    [Fact]
    public void SignPaymentRequest_ReturnsDeterministicHexString()
    {
        // Arrange
        var sut = CreateSut();
        var request = new PaymentRequest { ExternalId = Guid.NewGuid().ToString(), Amount = 99.99, Currency = "USD", NotificationUrl = "http://example.com" };

        // Act
        var sig1 = sut.SignPaymentRequest(request);
        var sig2 = sut.SignPaymentRequest(request);

        // Assert: same input always produces same signature
        sig1.Should().Be(sig2);
        sig1.Should().MatchRegex("^[0-9a-f]+$");
    }

    [Fact]
    public void SignPaymentRequest_DifferentSecrets_ProduceDifferentSignatures()
    {
        // Arrange
        var request = new PaymentRequest { ExternalId = Guid.NewGuid().ToString(), Amount = 50.0, Currency = "USD", NotificationUrl = "http://example.com" };

        // Act
        var sig1 = CreateSut("secret-a").SignPaymentRequest(request);
        var sig2 = CreateSut("secret-b").SignPaymentRequest(request);

        // Assert
        sig1.Should().NotBe(sig2);
    }

    [Fact]
    public void SignNotificationRequest_ReturnsDeterministicHexString()
    {
        // Arrange
        var sut = CreateSut();
        var notification = BuildNotification();

        // Act
        var sig1 = sut.SignNotificationRequest(notification);
        var sig2 = sut.SignNotificationRequest(notification);

        // Assert
        sig1.Should().Be(sig2);
        sig1.Should().MatchRegex("^[0-9a-f]+$");
    }

    [Fact]
    public void VerifyWebhookSignature_WithMatchingSignatureHeader_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        var notification = BuildNotification();
        var signature = sut.SignNotificationRequest(notification);

        // Act
        var result = sut.VerifyWebhookSignature(notification, signature);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyWebhookSignature_WithWrongHeader_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var notification = BuildNotification();

        // Act
        var result = sut.VerifyWebhookSignature(notification, "wrong-sig");

        // Assert
        result.Should().BeFalse();
    }
}

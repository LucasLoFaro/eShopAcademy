using System.Net;
using System.Net.Http.Json;
using Domain.Payments.Contracts;
using FluentAssertions;
using Infrastructure.Configuration;
using Infrastructure.Helpers;
using Infrastructure.Psp;
using Microsoft.Extensions.Options;
using Xunit;

namespace Payments.Tests.Psp;

public sealed class PspPaymentClientTests
{
    [Fact]
    public async Task InitiateAsync_TransientFailure_IsNotAutomaticallyRetried()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = CreateClient(handler);

        var act = () => client.InitiateAsync(Request(), CancellationToken.None);

        await act.Should().ThrowAsync<PspTransientException>();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task InitiateAsync_PermanentFailure_IsNotAutomaticallyRetried()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var client = CreateClient(handler);

        var act = () => client.InitiateAsync(Request(), CancellationToken.None);

        await act.Should().ThrowAsync<PspPermanentException>();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task InitiateAsync_UsesOrderAsStableIdempotencyKey()
    {
        string? idempotencyKey = null;
        var request = Request();
        var handler = new StubHandler(message =>
        {
            idempotencyKey = message.Headers.GetValues("Idempotency-Key").Single();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new PaymentResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    ExternalId = request.ExternalId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Status = "Pending",
                    Url = "https://psp.example/payment"
                })
            };
        });

        await CreateClient(handler).InitiateAsync(request, CancellationToken.None);

        idempotencyKey.Should().Be(request.ExternalId);
    }

    private static PspPaymentClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://psp.example") };
        var signatures = new SignatureHelper(Options.Create(new PaymentSecurityOptions
        {
            SignatureSecret = "test-secret-key-at-least-thirty-two-characters"
        }));
        return new PspPaymentClient(httpClient, signatures);
    }

    private static PaymentRequest Request() => new()
    {
        ExternalId = Guid.NewGuid().ToString(),
        Amount = 10,
        Currency = "USD",
        NotificationUrl = "https://payments.example/webhook"
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(response(request));
        }
    }
}

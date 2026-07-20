using System.Net;
using Domain.Common.Events.Orders;
using Domain.Common.Events.Sellers;
using Domain.Sellers.Entities;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Moq;
using Sellers.Application.Repositories;
using Sellers.Application.Services;
using Sellers.EventsProcessor.Consumers;
using Sellers.Service.Consumers;
using Xunit;

namespace Sellers.Tests;

public class HealthAndReliabilityTests
{
    [Fact]
    public async Task DependencyFailure_FailsReadinessButNotLiveness()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
            .AddCheck("sellers-mongodb", () => HealthCheckResult.Unhealthy(), ["ready"]);
        await using var app = builder.Build();
        app.UseDefaultEndpoints();
        await app.StartAsync();

        (await app.GetTestClient().GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await app.GetTestClient().GetAsync("/alive")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void MissingSellerDatabase_FailsOptionsValidation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSellerStorage(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IOptions<SellerStorageOptions>>().Value;
        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public async Task DuplicateSaleDelivery_DoesNotPublishAgain()
    {
        var seller = new Seller();
        var repository = new Mock<ISellerRepository>();
        repository.Setup(r => r.TryRegisterSaleAsync(
                seller.Id, It.IsAny<SellerLedgerEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((seller, false));
        var publish = new Mock<IPublishEndpoint>();
        var service = new SellerService(repository.Object, publish.Object);

        var result = await service.RegisterSaleAsync(
            seller.Id, Guid.NewGuid(), Guid.NewGuid(), 10m, 1m, "duplicate", CancellationToken.None);

        result.Should().BeSameAs(seller);
        publish.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task SaleConsumer_WhenStorageTimesOut_PropagatesTransientFailure()
    {
        var service = new Mock<ISellerService>();
        service.Setup(x => x.RegisterSaleAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(),
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Mongo timeout"));
        var message = new OrderSellerSaleRegistrationRequestedEvent
        {
            SellerId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            OrderItemId = Guid.NewGuid()
        };
        var context = new Mock<ConsumeContext<OrderSellerSaleRegistrationRequestedEvent>>();
        context.SetupGet(x => x.Message).Returns(message);
        var consumer = new OrderSellerSaleRegistrationRequestedConsumer(
            new CapturingLogger<OrderSellerSaleRegistrationRequestedConsumer>(), service.Object);

        var act = () => consumer.Consume(context.Object);
        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task SaleConsumer_WhenBusinessIdentifiersAreMissing_ThrowsPermanentFailure()
    {
        var context = new Mock<ConsumeContext<OrderSellerSaleRegistrationRequestedEvent>>();
        context.SetupGet(x => x.Message).Returns(new OrderSellerSaleRegistrationRequestedEvent());
        var consumer = new OrderSellerSaleRegistrationRequestedConsumer(
            new CapturingLogger<OrderSellerSaleRegistrationRequestedConsumer>(), Mock.Of<ISellerService>());

        var act = () => consumer.Consume(context.Object);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task TaxConsumer_DoesNotLogTaxIdentifier()
    {
        const string secretTaxId = "secret-tax-identifier";
        var message = new SellerTaxVerificationRequestedEvent
        {
            SellerId = Guid.NewGuid(),
            TaxId = secretTaxId
        };
        var repository = new Mock<ISellerRepository>();
        repository.Setup(x => x.GetByIdAsync(message.SellerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Seller?)null);
        var logger = new CapturingLogger<SellerTaxBillingVerificationConsumer>();
        var context = new Mock<ConsumeContext<SellerTaxVerificationRequestedEvent>>();
        context.SetupGet(x => x.Message).Returns(message);

        await new SellerTaxBillingVerificationConsumer(repository.Object, logger).Consume(context.Object);

        logger.Messages.Should().NotContain(entry => entry.Contains(secretTaxId, StringComparison.Ordinal));
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}

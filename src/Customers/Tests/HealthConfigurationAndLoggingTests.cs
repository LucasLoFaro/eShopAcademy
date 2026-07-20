using System.Net;
using Customers.Infrastructure.Data;
using Customers.Messaging.Consumers;
using Domain.Common.Events.Customers;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Customers.Tests;

public class HealthConfigurationAndLoggingTests
{
    [Fact]
    public async Task DependencyFailure_FailsReadinessButNotLiveness()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
            .AddCheck("customers-mongodb", () => HealthCheckResult.Unhealthy(), ["ready"]);

        await using var app = builder.Build();
        app.UseDefaultEndpoints();
        await app.StartAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await app.GetTestClient().GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await app.GetTestClient().GetAsync("/alive")).StatusCode);
    }

    [Fact]
    public void MissingMongoConnection_FailsOptionsValidation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCustomerStorage(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<CustomerStorageOptions>>().Value);
    }

    [Fact]
    public async Task Consumer_DoesNotLogAddressBody()
    {
        const string secretStreet = "customer-secret-street";
        var message = new CustomerAddressUpdatedEvent
        {
            CustomerId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Street = secretStreet,
            Number = "10",
            ZipCode = "12345",
            City = "Private City"
        };
        var repository = new Mock<ICustomerRepository>();
        repository.Setup(x => x.GetByIdAsync(message.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Customers.Entities.Customer?)null);
        var logger = new CapturingLogger<CustomerAddressUpdatedEventConsumer>();
        var context = new Mock<ConsumeContext<CustomerAddressUpdatedEvent>>();
        context.SetupGet(x => x.Message).Returns(message);

        await new CustomerAddressUpdatedEventConsumer(repository.Object, logger).Consume(context.Object);

        Assert.DoesNotContain(logger.Messages, entry => entry.Contains(secretStreet, StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, entry => entry.Contains(message.CustomerId.ToString(), StringComparison.Ordinal));
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

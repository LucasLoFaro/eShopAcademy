using AutoFixture.Xunit2;
using Customers.Infrastructure.Data;
using Customers.Messaging.Consumers;
using Domain.Common.Events.Customers;
using Domain.Customers.Entities;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Customers.Tests.Consumers;

public class CustomerAddressUpdatedEventConsumerTests
{
    private readonly Mock<ICustomerRepository> _repository = new();

    private CustomerAddressUpdatedEventConsumer CreateSut() =>
        new(_repository.Object, NullLogger<CustomerAddressUpdatedEventConsumer>.Instance);

    private static ConsumeContext<CustomerAddressUpdatedEvent> BuildContext(CustomerAddressUpdatedEvent evt)
    {
        var context = new Mock<ConsumeContext<CustomerAddressUpdatedEvent>>();
        context.Setup(c => c.Message).Returns(evt);
        return context.Object;
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenCustomerNotFound_DoesNotUpdateOrAddAddress(CustomerAddressUpdatedEvent evt)
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(evt.CustomerId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Customer?)null);

        // Act
        await CreateSut().Consume(BuildContext(evt));

        // Assert: no writes made
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.AddAddressAsync(It.IsAny<Guid>(), It.IsAny<SavedAddress>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.AddAddressIfMissingAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<SavedAddress>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenAddressAlreadyExists_UpdatesLegacyFieldAndDoesNotAddNewAddress(
        CustomerAddressUpdatedEvent evt)
    {
        // Arrange: customer has an exact matching saved address
        var matchingAddress = new SavedAddress
        {
            Address = new Address
            {
                Street = evt.Street,
                Number = evt.Number,
                ZipCode = evt.ZipCode,
                City = evt.City
            }
        };
        var customer = new Customer
        {
            Name = "Alice",
            Mail = "alice@example.com",
            Phone = "555-0000",
            Address = new Address(),
            SavedAddresses = [matchingAddress]
        };

        _repository.Setup(r => r.GetByIdAsync(evt.CustomerId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(customer);
        _repository.Setup(r => r.UpdateAsync(evt.CustomerId, customer, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(customer);

        // Act
        await CreateSut().Consume(BuildContext(evt));

        // Assert: legacy field updated, no new address added
        _repository.Verify(r => r.UpdateAsync(evt.CustomerId, customer, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.AddAddressAsync(It.IsAny<Guid>(), It.IsAny<SavedAddress>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.AddAddressIfMissingAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<SavedAddress>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenAddressIsNew_AddsItAsSavedAddress(CustomerAddressUpdatedEvent evt)
    {
        // Arrange: customer has no saved addresses
        var customer = new Customer
        {
            Name = "Bob",
            Mail = "bob@example.com",
            Phone = "555-1111",
            Address = new Address(),
            SavedAddresses = []
        };

        _repository.Setup(r => r.GetByIdAsync(evt.CustomerId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(customer);
        _repository.Setup(r => r.AddAddressIfMissingAsync(
                evt.CustomerId,
                $"Order {evt.OrderId:D}",
                It.IsAny<SavedAddress>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await CreateSut().Consume(BuildContext(evt));

        // Assert: new address saved with the event values
        _repository.Verify(r => r.AddAddressIfMissingAsync(
            evt.CustomerId,
            $"Order {evt.OrderId:D}",
            It.Is<SavedAddress>(a =>
                a.Address.Street == evt.Street &&
                a.Address.ZipCode == evt.ZipCode &&
                a.Address.City == evt.City),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenDuplicateIsDelivered_DoesNotAddAnotherAddress(CustomerAddressUpdatedEvent evt)
    {
        var customer = new Customer
        {
            Name = "Existing",
            Mail = "existing@example.invalid",
            Phone = "unused",
            Address = new Address(),
            SavedAddresses =
            [
                new SavedAddress
                {
                    Address = new Address
                    {
                        Street = evt.Street,
                        Number = evt.Number,
                        ZipCode = evt.ZipCode,
                        City = evt.City
                    }
                }
            ]
        };
        _repository.Setup(r => r.GetByIdAsync(evt.CustomerId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _repository.Setup(r => r.UpdateAsync(evt.CustomerId, customer, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        var consumer = CreateSut();

        await consumer.Consume(BuildContext(evt));
        await consumer.Consume(BuildContext(evt));

        _repository.Verify(r => r.AddAddressAsync(It.IsAny<Guid>(), It.IsAny<SavedAddress>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.AddAddressIfMissingAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<SavedAddress>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenConcurrentDuplicateIsDelivered_UsesStableOrderDeduplicationKey(
        CustomerAddressUpdatedEvent evt)
    {
        var customer = new Customer
        {
            Name = "Existing",
            Mail = "existing@example.invalid",
            Phone = "unused",
            Address = new Address(),
            SavedAddresses = []
        };
        var operationId = $"Order {evt.OrderId:D}";
        _repository.Setup(r => r.GetByIdAsync(evt.CustomerId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _repository.SetupSequence(r => r.AddAddressIfMissingAsync(
                evt.CustomerId,
                operationId,
                It.IsAny<SavedAddress>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        var consumer = CreateSut();

        await consumer.Consume(BuildContext(evt));
        await consumer.Consume(BuildContext(evt));

        _repository.Verify(r => r.AddAddressIfMissingAsync(
            evt.CustomerId,
            operationId,
            It.IsAny<SavedAddress>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Consume_WhenIdentifiersAreMissing_ThrowsPermanentFailure()
    {
        var evt = new CustomerAddressUpdatedEvent { CustomerId = Guid.Empty, OrderId = Guid.Empty };
        var act = () => CreateSut().Consume(BuildContext(evt));
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenRepositoryTimesOut_PropagatesTransientFailure(CustomerAddressUpdatedEvent evt)
    {
        _repository.Setup(r => r.GetByIdAsync(evt.CustomerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Mongo timeout"));
        var act = () => CreateSut().Consume(BuildContext(evt));
        await act.Should().ThrowAsync<TimeoutException>();
    }
}

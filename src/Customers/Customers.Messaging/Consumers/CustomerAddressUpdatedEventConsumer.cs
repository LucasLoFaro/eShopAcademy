using Customers.Infrastructure.Data;
using Domain.Common.Events.Customers;
using Domain.Customers.Entities;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Customers.Messaging.Consumers;

public class CustomerAddressUpdatedEventConsumer : IConsumer<CustomerAddressUpdatedEvent>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<CustomerAddressUpdatedEventConsumer> _logger;

    public CustomerAddressUpdatedEventConsumer(
        ICustomerRepository customerRepository,
        ILogger<CustomerAddressUpdatedEventConsumer> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CustomerAddressUpdatedEvent> context)
    {
        var evt = context.Message;

        if (evt.CustomerId == Guid.Empty || evt.OrderId == Guid.Empty)
        {
            throw new ArgumentException("Customer and order identifiers are required.");
        }
        
        _logger.LogInformation(
            "[CustomerAddressUpdate] Processing address update from order {OrderId}",
            evt.OrderId);

        var customer = await _customerRepository.GetByIdAsync(evt.CustomerId, context.CancellationToken);
        if (customer == null)
        {
            _logger.LogWarning(
                "[CustomerAddressUpdate] Customer record was not found");
            return;
        }

        var newAddress = new Address
        {
            Street = evt.Street,
            Number = evt.Number,
            AdditionalInformation = evt.AdditionalInformation,
            ZipCode = evt.ZipCode,
            City = evt.City
        };

        // Check if this exact address already exists in saved addresses
        var existingAddress = customer.SavedAddresses.FirstOrDefault(a => 
            a.Address.Street.Equals(newAddress.Street, StringComparison.OrdinalIgnoreCase) &&
            a.Address.Number.Equals(newAddress.Number, StringComparison.OrdinalIgnoreCase) &&
            a.Address.ZipCode.Equals(newAddress.ZipCode, StringComparison.OrdinalIgnoreCase) &&
            a.Address.City.Equals(newAddress.City, StringComparison.OrdinalIgnoreCase));

        if (existingAddress != null)
        {
            _logger.LogInformation(
                "[CustomerAddressUpdate] Address already exists; skipping duplicate insert");
            
            // Update the legacy Address field for backward compatibility
            customer.Address = newAddress;
            await _customerRepository.UpdateAsync(evt.CustomerId, customer, context.CancellationToken);
            return;
        }

        // Address doesn't exist, add it as a new saved address
        var operationId = $"Order {evt.OrderId:D}";
        var savedAddress = new SavedAddress
        {
            Description = operationId,
            Address = newAddress,
            IsDefault = customer.SavedAddresses.Count == 0 // Set as default if it's the first address
        };

        try
        {
            var added = await _customerRepository.AddAddressIfMissingAsync(
                evt.CustomerId,
                operationId,
                savedAddress,
                context.CancellationToken);

            if (added)
            {
                _logger.LogInformation(
                    "[CustomerAddressUpdate] Added an address. Address count: {Count}",
                    customer.SavedAddresses.Count + 1);
            }
            else
            {
                _logger.LogInformation("[CustomerAddressUpdate] Duplicate delivery skipped");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[CustomerAddressUpdate] Failed to add an address");
            throw;
        }
    }
}


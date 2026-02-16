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
        
        _logger.LogInformation(
            "[CustomerAddressUpdate] Processing address update for customer {CustomerId} from order {OrderId}",
            evt.CustomerId, evt.OrderId);

        var customer = await _customerRepository.GetByIdAsync(evt.CustomerId);
        if (customer == null)
        {
            _logger.LogWarning(
                "[CustomerAddressUpdate] Customer {CustomerId} not found",
                evt.CustomerId);
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
                "[CustomerAddressUpdate] Address already exists for customer {CustomerId}, skipping",
                evt.CustomerId);
            
            // Update the legacy Address field for backward compatibility
            customer.Address = newAddress;
            await _customerRepository.UpdateAsync(evt.CustomerId, customer);
            return;
        }

        // Address doesn't exist, add it as a new saved address
        var savedAddress = new SavedAddress
        {
            Description = $"Order {evt.OrderId.ToString()[..8]}", // Use first 8 chars of order ID as description
            Address = newAddress,
            IsDefault = customer.SavedAddresses.Count == 0 // Set as default if it's the first address
        };

        try
        {
            await _customerRepository.AddAddressAsync(evt.CustomerId, savedAddress);
            
            _logger.LogInformation(
                "[CustomerAddressUpdate] Added new address '{Description}' for customer {CustomerId}. Total addresses: {Count}",
                savedAddress.Description, evt.CustomerId, customer.SavedAddresses.Count + 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[CustomerAddressUpdate] Failed to add address for customer {CustomerId}",
                evt.CustomerId);
            throw;
        }
    }
}


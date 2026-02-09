using Domain.Common.Events.Orders;
using Domain.Shipping.Contracts.Requests;
using Domain.Shipping.Contracts.Responses;
using Domain.Shipping.Entities;
using Domain.Shipping.Helpers;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shipping.Application.Clients;
using Shipping.Application.Data;

namespace Shipping.Application.Services;

public interface IShippingStatusService
{
    Task ProcessStatusUpdateAsync(ShippingStatusUpdateRequest update, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShippingStatusResponse>> GetHistoryAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<ShippingStatusResponse?> GetLatestStatusAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task ScheduleShippingAsync(Domain.Shipping.Entities.Shipping shipping, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShippingStatusResponse>> QueryProviderHistoryAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public sealed class ShippingStatusService : IShippingStatusService
{
    private readonly IShippingStatusHistoryRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IShippingProviderClient _providerClient;
    private readonly ILogger<ShippingStatusService> _logger;

    public ShippingStatusService(
        IShippingStatusHistoryRepository repository,
        IPublishEndpoint publishEndpoint,
        IShippingProviderClient providerClient,
        ILogger<ShippingStatusService> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _providerClient = providerClient;
        _logger = logger;
    }

    public async Task ProcessStatusUpdateAsync(ShippingStatusUpdateRequest update, CancellationToken cancellationToken = default)
    {
        if (update.ShippingId == Guid.Empty || update.OrderId == Guid.Empty)
        {
            _logger.LogWarning("Received shipping update with missing identifiers.");
            return;
        }

        var entry = new ShippingStatusHistoryEntry
        {
            ShipmentId = update.ShippingId,
            OrderId = update.OrderId,
            Status = update.Status,
            TrackingNumber = update.TrackingNumber,
            Carrier = update.Carrier,
            OccurredAt = update.OccurredAt
        };

        await _repository.AddAsync(entry, cancellationToken);

        var normalizedStatus = ProviderShippingStatus.Normalize(update.Status);

        if (normalizedStatus == ProviderShippingStatus.Shipped)
        {
            await PublishEventAsync(new OrderShippedEvent
            {
                OrderId = update.OrderId,
                TrackingNumber = update.TrackingNumber,
                Carrier = update.Carrier,
                ShippedAt = entry.OccurredAt
            }, cancellationToken);
        }
        else if (normalizedStatus == ProviderShippingStatus.Delivered)
        {
            await PublishEventAsync(new OrderDeliveredEvent
            {
                OrderId = update.OrderId,
                TrackingNumber = update.TrackingNumber,
                DeliveredAt = entry.OccurredAt
            }, cancellationToken);
        }

        _logger.LogInformation("Persisted shipping status {Status} for shipment {ShipmentId}.",
            update.Status, update.ShippingId);
    }

    public async Task<IReadOnlyList<ShippingStatusResponse>> GetHistoryAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var history = await _repository.GetHistoryAsync(orderId, cancellationToken);
        return history.Select(ToResponse).ToList();
    }

    public async Task<ShippingStatusResponse?> GetLatestStatusAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var latest = await _repository.GetLatestStatusAsync(orderId, cancellationToken);
        return latest is null ? null : ToResponse(latest);
    }

    public Task ScheduleShippingAsync(Domain.Shipping.Entities.Shipping shipping, CancellationToken cancellationToken = default)
        => _providerClient.ScheduleShippingAsync(shipping, cancellationToken);

    public Task<IReadOnlyList<ShippingStatusResponse>> QueryProviderHistoryAsync(Guid orderId, CancellationToken cancellationToken = default)
        => _providerClient.GetStatusHistoryAsync(orderId, cancellationToken);

    private Task PublishEventAsync<T>(T message, CancellationToken cancellationToken)
        where T : OrderEvent
    {
        _logger.LogInformation("Publishing {Event} for order {OrderId}.", message.EventType, message.OrderId);
        return _publishEndpoint.Publish(message, cancellationToken);
    }

    private static ShippingStatusResponse ToResponse(ShippingStatusHistoryEntry entry) =>
        new(entry.ShipmentId, entry.OrderId, entry.Status, entry.TrackingNumber, entry.Carrier, entry.OccurredAt);
}

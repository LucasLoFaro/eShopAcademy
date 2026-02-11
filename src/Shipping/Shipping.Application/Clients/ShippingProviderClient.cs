using System.Net.Http.Json;
using Domain.Shipping.Contracts.Requests;
using Domain.Shipping.Contracts.Responses;
using Microsoft.Extensions.Options;
using Shipping.Application.Constants;
using Shipping.Application.Options;

namespace Shipping.Application.Clients;

public sealed class ShippingProviderClient : IShippingProviderClient
{
    private readonly HttpClient _client;
    private readonly ShippingProviderOptions _options;

    public ShippingProviderClient(HttpClient client, IOptions<ShippingProviderOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<ScheduleShippingResponse> ScheduleShippingAsync(Domain.Shipping.Entities.Shipping shipping, CancellationToken cancellationToken = default)
    {
        EnsureBasePathConfigured();

        var response = await _client.PostAsJsonAsync(ShippingProviderConstants.SchedulePath, shipping, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ScheduleShippingResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Shipping provider returned an empty schedule response.");
    }

    public async Task ConfirmPickupAsync(Guid shippingId, Guid orderId, DateTime readyAt, CancellationToken cancellationToken = default)
    {
        EnsureBasePathConfigured();

        var request = new ShippingPickupConfirmationRequest
        {
            ShippingId = shippingId,
            OrderId = orderId,
            ReadyAt = readyAt
        };

        var response = await _client.PostAsJsonAsync(ShippingProviderConstants.ConfirmPickupPath, request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<ShippingStatusResponse>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        EnsureBasePathConfigured();

        var path = string.Format(ShippingProviderConstants.HistoryPathFormat, orderId);
        var response = await _client.GetAsync(path, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<ShippingStatusResponse>();
        }

        IReadOnlyList<ShippingStatusResponse>? history = await response.Content.ReadFromJsonAsync<List<ShippingStatusResponse>>(cancellationToken: cancellationToken);
        return history ?? Array.Empty<ShippingStatusResponse>();
    }

    private void EnsureBasePathConfigured()
    {
        if (_client.BaseAddress is null)
        {
            throw new InvalidOperationException("Shipping provider base address is not configured.");
        }
    }
}


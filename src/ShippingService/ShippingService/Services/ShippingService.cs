using Grpc.Core;

public class ShippingService : ShippingGrpc.ShippingGrpcBase
{
    private readonly ILogger<ShippingService> _logger;

    public ShippingService(ILogger<ShippingService> logger)
    {
        _logger = logger;
    }

    public override Task<ShippingResponse> ProcessShipping(ShippingRequest request, ServerCallContext context)
    {
        _logger.LogInformation($"Processing shipping for Order {request.OrderId} to {request.Address}");

        return Task.FromResult(new ShippingResponse
        {
            Success = true,
            TrackingNumber = $"TRACK-{Guid.NewGuid()}"
        });
    }
}

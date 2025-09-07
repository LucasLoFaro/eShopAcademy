using Grpc.Core;

namespace gRPC.Services;

public class PaymentService : PaymentGrpc.PaymentGrpcBase
{
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    public override Task<PaymentResponse> ProcessPayment(PaymentRequest request, ServerCallContext context)
    {
        _logger.LogInformation($"Processing payment for Order {request.OrderId}");

        // Mock. Always success at first.
        // Todo: Consume the external api mock (wiremock)
        return Task.FromResult(new PaymentResponse
        {
            Success = true,
            TransactionId = Guid.NewGuid().ToString()
        });
    }
}
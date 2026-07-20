using Domain.Payments.Contracts;
using Grpc.Core;
using Infrastructure.Messaging;
using Infrastructure.Observability;
using Infrastructure.Psp;
using Protos;

namespace Services;

public class PaymentService : PaymentGrpc.PaymentGrpcBase
{
    private readonly IPspPaymentClient _pspClient;
    private readonly IPaymentMessagingClient _messagingClient;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPspPaymentClient pspClient,
        IPaymentMessagingClient messagingClient,
        ILogger<PaymentService> logger)
    {
        _pspClient = pspClient;
        _messagingClient = messagingClient;
        _logger = logger;
    }

    public override async Task<InitiatePaymentResponse> Initiate(
        InitiatePaymentRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.ExternalId, out var orderId) ||
            request.Amount <= 0 ||
            string.IsNullOrWhiteSpace(request.Currency) ||
            !Uri.TryCreate(request.NotificationUrl, UriKind.Absolute, out _))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid payment request."));
        }

        var paymentRequest = new PaymentRequest
        {
            ExternalId = request.ExternalId,
            Amount = request.Amount,
            Currency = request.Currency,
            NotificationUrl = request.NotificationUrl
        };

        PaymentResponse paymentResponse;
        try
        {
            paymentResponse = await _pspClient.InitiateAsync(paymentRequest, context.CancellationToken);
        }
        catch (PspTransientException)
        {
            _logger.LogWarning("Payment provider was temporarily unavailable for order {OrderId}", orderId);
            throw new RpcException(new Status(StatusCode.Unavailable, "Payment provider temporarily unavailable."));
        }
        catch (PspPermanentException)
        {
            _logger.LogWarning("Payment provider rejected initiation for order {OrderId}", orderId);
            await _messagingClient.SendPaymentFailed(
                orderId,
                request.ExternalId,
                "Payment provider rejected the request",
                context.CancellationToken);
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Payment request rejected."));
        }

        await _messagingClient.SendPaymentCreated(
            orderId,
            paymentResponse.Id,
            context.CancellationToken);

        PaymentTelemetry.RecordResult("grpc", "accepted");
        return new InitiatePaymentResponse
        {
            Id = paymentResponse.Id,
            ExternalId = paymentResponse.ExternalId,
            Amount = paymentResponse.Amount,
            Currency = paymentResponse.Currency,
            Status = paymentResponse.Status,
            Url = paymentResponse.Url
        };
    }
}

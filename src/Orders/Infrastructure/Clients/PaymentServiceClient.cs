using Core.Application.Interfaces;
using Domain.Orders.Contracts;
using Protos;


namespace Infrastructure.Clients;

public class PaymentServiceClient : IPaymentServiceClient
{
    private readonly PaymentGrpc.PaymentGrpcClient _paymentGrpcClient;

    public PaymentServiceClient(PaymentGrpc.PaymentGrpcClient client)
    {
        _paymentGrpcClient = client;
    }

    public async Task<InitPaymentResponse> InitPaymentAsync(double amount, string currency, string notificationUrl, Guid orderId, CancellationToken ct = default)
    {
        var request = new InitiatePaymentRequest
        {
            ExternalId = orderId.ToString(),
            Amount = amount,
            Currency = currency,
            NotificationUrl = notificationUrl
        };
        var response = await _paymentGrpcClient.InitiateAsync(request, cancellationToken: ct);
        if (response != null && response.Status.Equals("Pending")) {
            return new()
            {
                Id = new Guid(response.Id),
                Amount = response.Amount,
                ProviderTransactionId = response.Id,
                PaymentUrl = response.Url
            };
        }
        else
            throw new Exception("There was an error creating the payment");
    }
}
    
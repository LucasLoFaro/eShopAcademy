using Core.Application.Interfaces;
using Core.Domain.Entities;
using System.Globalization;
using Protos;


namespace Infrastructure.Clients;

public class PaymentServiceClient : IPaymentServiceClient
{
    private readonly PaymentGrpc.PaymentGrpcClient _paymentGrpcClient;

    public PaymentServiceClient(PaymentGrpc.PaymentGrpcClient client)
    {
        _paymentGrpcClient = client;
    }

    public async Task<Payment> InitPaymentAsync(double amount, string currency, string notificationUrl, Guid orderId, CancellationToken ct = default)
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
                OrderId = new Guid(response.ExternalId),
                Status = Core.Domain.Enums.PaymentStatus.Pending,
                PaymentURL = response.Url,
                ProviderTransactionId = response.Id
            };
        }
        else
            throw new Exception("There was an error creating the payment");
    }
}
    
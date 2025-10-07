using Domain.Contracts;
using Grpc.Core;
using Infrastructure.Helpers;
using Infrastructure.Messaging;
using Newtonsoft.Json;
using Protos;
using System.Text;


namespace Services;

public class PaymentService : PaymentGrpc.PaymentGrpcBase
{
    private readonly HttpClient _httpClient;
    private readonly IPaymentMessagingClient _messagingClient;
    private readonly ISignatureHelper _signatureHelper;

    public PaymentService(HttpClient httpClient, IPaymentMessagingClient messagingClient, ISignatureHelper signatureHelper)
    {
        _httpClient = httpClient;
        _messagingClient = messagingClient;
        _signatureHelper = signatureHelper;
    }

    public override async Task<InitiatePaymentResponse> Initiate(InitiatePaymentRequest request, ServerCallContext context)
    {
        var paymentRequest = new PaymentRequest
        {
            ExternalId = request.ExternalId,
            Amount = request.Amount,
            Currency = request.Currency,
            NotificationUrl = request.NotificationUrl
        };

        var signature = _signatureHelper.SignPaymentRequest(paymentRequest);

        string json = JsonConvert.SerializeObject(paymentRequest);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/psp/make-payment")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("X-Signature", signature);


        using var response = await _httpClient.SendAsync(httpRequest, context.CancellationToken);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>(cancellationToken: context.CancellationToken);

        if (!response.IsSuccessStatusCode)
            _messagingClient.SendPaymentFailed(paymentResponse!.ExternalId, paymentResponse.Id, paymentResponse.FailureReason!, context.CancellationToken).GetAwaiter().GetResult();

        _messagingClient.SendPaymentCreated(paymentResponse!.ExternalId, paymentResponse.Id, context.CancellationToken).GetAwaiter().GetResult();

        // TODO: Add automapper.
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

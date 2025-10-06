using Infrastructure.Helpers;
using Domain.Contracts;
using Newtonsoft.Json;
using System.Text;
using Grpc.Core;
using Protos;


namespace Services;

public class PaymentService : PaymentGrpc.PaymentGrpcBase
{
    private readonly HttpClient _httpClient;
    private readonly ISignatureHelper _signatureHelper;

    public PaymentService(HttpClient httpClient, ISignatureHelper signatureHelper)
    {
        _httpClient = httpClient;
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
        response.EnsureSuccessStatusCode();

        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>(cancellationToken: context.CancellationToken);
        if (paymentResponse is null)
            throw new RpcException(new Status(StatusCode.Internal, "Invalid PSP response"));

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

using System.Net;
using System.Net.Http.Json;
using Domain.Payments.Contracts;
using Infrastructure.Helpers;
using Infrastructure.Observability;

namespace Infrastructure.Psp;

public interface IPspPaymentClient
{
    Task<PaymentResponse> InitiateAsync(PaymentRequest request, CancellationToken cancellationToken);
}

public sealed class PspPaymentClient : IPspPaymentClient
{
    private readonly HttpClient _httpClient;
    private readonly ISignatureHelper _signatureHelper;

    public PspPaymentClient(HttpClient httpClient, ISignatureHelper signatureHelper)
    {
        _httpClient = httpClient;
        _signatureHelper = signatureHelper;
    }

    public async Task<PaymentResponse> InitiateAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/psp/make-payment")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Signature", _signatureHelper.SignPaymentRequest(request));
        httpRequest.Headers.Add("Idempotency-Key", request.ExternalId);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            PaymentTelemetry.RecordResult("initiate", "transient_failure");
            throw new PspTransientException("The payment provider request timed out.");
        }
        catch (HttpRequestException exception)
        {
            PaymentTelemetry.RecordResult("initiate", "transient_failure");
            throw new PspTransientException("The payment provider is unavailable.", exception);
        }

        using (response)
        {
            if (IsTransient(response.StatusCode))
            {
                PaymentTelemetry.RecordResult("initiate", "transient_failure");
                throw new PspTransientException($"The payment provider returned {(int)response.StatusCode}.");
            }

            if (!response.IsSuccessStatusCode)
            {
                PaymentTelemetry.RecordResult("initiate", "permanent_failure");
                throw new PspPermanentException($"The payment provider rejected the request with {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<PaymentResponse>(cancellationToken)
                ?? throw new PspPermanentException("The payment provider returned an empty response.");

            PaymentTelemetry.RecordResult("initiate", "accepted");
            return result;
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
        (int)statusCode >= 500;
}

public sealed class PspTransientException : Exception
{
    public PspTransientException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

public sealed class PspPermanentException : Exception
{
    public PspPermanentException(string message)
        : base(message) { }
}

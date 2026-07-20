using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.Payments.Contracts;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Helpers;

public class SignatureHelper : ISignatureHelper
{
    private readonly string _signatureSecret;

    public SignatureHelper(IOptions<PaymentSecurityOptions> options)
    {
        _signatureSecret = options.Value.SignatureSecret;
    }

    public bool VerifyWebhookSignature(PaymentNotification payload, string headerSignature)
        => Verify(JsonSerializer.Serialize(payload), headerSignature);

    public bool VerifyPaymentRequest(PaymentRequest payload, string headerSignature)
        => Verify(JsonSerializer.Serialize(payload), headerSignature);

    private bool Verify(string payload, string headerSignature)
    {
        if (string.IsNullOrWhiteSpace(headerSignature))
            return false;

        var computed = ComputeHmacHex(payload);
        var suppliedBytes = Encoding.ASCII.GetBytes(headerSignature.Trim().ToLowerInvariant());
        var computedBytes = Encoding.ASCII.GetBytes(computed);
        return suppliedBytes.Length == computedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(computedBytes, suppliedBytes);
    }

    public string SignNotificationRequest(PaymentNotification request)
    {
        var json = JsonSerializer.Serialize(request);
        return ComputeHmacHex(json);
    }

    public string SignPaymentRequest(PaymentRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        return ComputeHmacHex(json);
    }

    private string ComputeHmacHex(string json)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_signatureSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public interface ISignatureHelper
{
    string SignNotificationRequest(PaymentNotification request);
    bool VerifyWebhookSignature(PaymentNotification payload, string headerSignature);
    bool VerifyPaymentRequest(PaymentRequest payload, string headerSignature);
    string SignPaymentRequest(PaymentRequest request);
}

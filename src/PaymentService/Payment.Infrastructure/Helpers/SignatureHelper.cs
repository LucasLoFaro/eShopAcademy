using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using Domain.Contracts;
using System.Text.Json;
using System.Text;


namespace Infrastructure.Helpers;

public class SignatureHelper : ISignatureHelper
{
    private readonly string _signatureSecret;

    public SignatureHelper (IConfiguration config)
    {
        _signatureSecret = config["Payment:SignatureSecret"]!;
    }

    public bool VerifyWebhookSignature(PaymentNotification payload, string headerSignature)
    {
        return headerSignature.Equals("Signature"); // For testing purposes only

        if (string.IsNullOrWhiteSpace(headerSignature))
            return false;

        var computed = ComputeHmacHex(JsonSerializer.Serialize(payload));
        return string.Equals(computed, headerSignature.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public string SignNotificationRequest(PaymentNotification request) // For testing purposes only
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
    string SignNotificationRequest(PaymentNotification request); // For testing purposes only
    bool VerifyWebhookSignature(PaymentNotification payload, string headerSignature);
    string SignPaymentRequest(PaymentRequest request);
}
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Messaging;

public static class PaymentMessageIdentity
{
    public static Guid Create(Guid orderId, string providerTransactionId, string outcome)
    {
        var value = $"{orderId:N}:{providerTransactionId}:{outcome}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }
}

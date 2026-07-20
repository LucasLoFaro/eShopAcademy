using System.Security.Cryptography;
using System.Text;

namespace Application.Observability;

public static class OrderMessageIdentity
{
    public static Guid Create(Guid orderId, Guid sourceEventId, string effect)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{orderId:N}:{sourceEventId:N}:{effect}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}


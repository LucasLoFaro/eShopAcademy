using System.Collections.Concurrent;

namespace Infrastructure.Idempotency;

public interface IPaymentOperationRegistry
{
    bool TryBegin(string operationKey);
    void Abandon(string operationKey);
}

public sealed class PaymentOperationRegistry : IPaymentOperationRegistry
{
    private readonly ConcurrentDictionary<string, byte> _operations = new(StringComparer.Ordinal);

    public bool TryBegin(string operationKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKey);
        return _operations.TryAdd(operationKey, 0);
    }

    public void Abandon(string operationKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKey);
        _operations.TryRemove(operationKey, out _);
    }
}


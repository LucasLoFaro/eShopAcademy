using System.Collections.Concurrent;
using Domain.Payments.Contracts;

namespace Psp.Simulator;

public sealed class PspPaymentRegistry
{
    private readonly ConcurrentDictionary<string, PendingPayment> _byId = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PendingPayment> _byExternalId = new(StringComparer.Ordinal);

    public PaymentRegistration Register(PaymentRequest request, Uri notificationUrl)
    {
        var payment = _byExternalId.GetOrAdd(request.ExternalId, _ => new PendingPayment
        {
            Id = Guid.NewGuid().ToString(),
            ExternalId = request.ExternalId,
            Amount = request.Amount,
            Currency = request.Currency,
            NotificationUrl = notificationUrl
        });

        var conflict = payment.Amount != request.Amount ||
            !string.Equals(payment.Currency, request.Currency, StringComparison.OrdinalIgnoreCase) ||
            payment.NotificationUrl != notificationUrl;
        if (!conflict)
            _byId.TryAdd(payment.Id, payment);

        return new PaymentRegistration(payment, conflict);
    }

    public bool TryGet(string id, out PendingPayment payment) => _byId.TryGetValue(id, out payment!);
}

public sealed record PaymentRegistration(PendingPayment Payment, bool Conflict);

public sealed class PendingPayment
{
    public string Id { get; init; } = default!;
    public string ExternalId { get; init; } = default!;
    public double Amount { get; init; }
    public string Currency { get; init; } = default!;
    public Uri NotificationUrl { get; init; } = default!;
    public string Status { get; set; } = "Pending";
}

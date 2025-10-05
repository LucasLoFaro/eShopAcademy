using Core.Domain.Entities;

namespace Core.Application.Interfaces;

public interface IPaymentServiceClient
{
    Task<Payment> InitPaymentAsync(double amount, string currency, string notificationUrl, Guid orderId, CancellationToken ct = default);
}
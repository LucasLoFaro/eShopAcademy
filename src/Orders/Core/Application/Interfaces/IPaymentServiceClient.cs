using Domain.Orders.Contracts;

namespace Core.Application.Interfaces;

public interface IPaymentServiceClient
{
    Task<InitPaymentResponse> InitPaymentAsync(double amount, string currency, string notificationUrl, Guid orderId, CancellationToken ct = default);
}
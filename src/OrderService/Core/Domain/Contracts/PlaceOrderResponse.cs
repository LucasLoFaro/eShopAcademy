using Core.Domain.Enums;

namespace Domain.Contracts;

public class PlaceOrderResponse
{
    public Guid OrderId { get; set; }
    public Uri PaymentUrl { get; set; }
    public OrderStatus Status { get; set; }
}

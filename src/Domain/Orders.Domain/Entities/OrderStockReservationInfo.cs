namespace Domain.Orders.Entities;

public class OrderStockReservationInfo
{
    public Guid ReservationId { get; set; }
    public DateTime? CommittedAt { get; set; }
}

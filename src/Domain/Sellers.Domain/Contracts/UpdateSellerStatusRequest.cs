using Domain.Sellers.Enums;

namespace Domain.Sellers.Contracts;

public class UpdateSellerStatusRequest
{
    public SellerStatus Status { get; set; }
}

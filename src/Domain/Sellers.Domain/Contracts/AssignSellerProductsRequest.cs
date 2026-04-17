namespace Domain.Sellers.Contracts;

public class AssignSellerProductsRequest
{
    public List<Guid> ProductIds { get; set; } = [];
}

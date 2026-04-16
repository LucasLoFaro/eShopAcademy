using Domain.Sellers.Entities;

namespace Domain.Sellers.Contracts;

public class CreateSellerRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public SellerAddress Address { get; set; } = new();
}

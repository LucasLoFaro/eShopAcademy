using Domain.Sellers.Entities;

namespace Domain.Sellers.Contracts;

public class RegisterSellerRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public SellerAddress Address { get; set; } = new();
    public string DocumentUrl { get; set; } = string.Empty;
}

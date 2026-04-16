namespace Domain.Sellers.Entities;

public class SellerAddress
{
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string AdditionalInformation { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

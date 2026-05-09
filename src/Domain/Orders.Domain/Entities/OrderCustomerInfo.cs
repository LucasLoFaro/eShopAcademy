using System.Text.Json.Serialization;

namespace Domain.Orders.Entities;

public class OrderCustomerInfo
{
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("mail")]
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public OrderAddressInfo Address { get; set; }
}

public class OrderAddressInfo
{
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string AdditionalInformation { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

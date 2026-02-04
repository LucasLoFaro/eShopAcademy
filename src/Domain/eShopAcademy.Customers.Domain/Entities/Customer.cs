using Domain.Customers.Enums;


namespace Domain.Customers.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; }
    public string Mail { get; set; }
    public string Phone { get; set; }
    public Address Address { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
}

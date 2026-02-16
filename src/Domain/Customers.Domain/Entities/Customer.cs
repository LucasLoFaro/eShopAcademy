using Domain.Customers.Enums;


namespace Domain.Customers.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; }
    public string Mail { get; set; }
    public string Phone { get; set; }
    public Address Address { get; set; } // Legacy - kept for backward compatibility
    public List<SavedAddress> SavedAddresses { get; set; } = new();
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
}

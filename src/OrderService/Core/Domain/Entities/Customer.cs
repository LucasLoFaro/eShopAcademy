namespace Core.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; }
    public string Mail { get; set; }
    public string Phone { get; set; }
    public Address Address { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string Number { get; set; }
    public string AdditionalInformation { get; set; }
    public string ZipCode { get; set; }
    public string City { get; set; }
}

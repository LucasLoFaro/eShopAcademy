namespace Domain.Stock.Entities;

public class Warehouse : BaseEntity
{
    public String Name { get; set; }
    public Location Location { get; set; }
}

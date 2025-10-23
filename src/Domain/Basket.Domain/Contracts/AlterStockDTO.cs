namespace Domain.Basket.Contracts;

public class AlterStockDTO
{
    public Guid ProductGuid { get; set; }
    public int Quantity { get; set; }
    public String Warehouse { get; set; }
    //public Warehouse Warehouse { get; set; }
}

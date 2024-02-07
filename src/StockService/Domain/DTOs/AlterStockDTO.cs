namespace Domain.DTOs
{
    public class AlterStockDTO
    {
        public string ProductGuid { get; set; }
        public int Quantity { get; set; }
        public string Warehouse { get; set; }
    }
}

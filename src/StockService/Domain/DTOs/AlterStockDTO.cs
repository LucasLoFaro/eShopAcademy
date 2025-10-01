using Core.Domain.Entities;

namespace Core.Domain.DTOs
{
    public class AlterStockDTO
    {
        public Guid ProductGuid { get; set; }
        public int Quantity { get; set; }
        public String Warehouse { get; set; }
        //public Warehouse Warehouse { get; set; }

        public AlterStockDTO(){ }
        public AlterStockDTO(Stock stock)
        {
            ProductGuid = stock.ProductID;
            Quantity = stock.Quantity;
            Warehouse = stock.Warehouse;
        }
    }
}

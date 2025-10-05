using Core.Domain.Entities;

namespace Core.Domain.Contracts
{
    public class AlterStockRequest
    {
        public Guid ProductGuid { get; set; }
        public int Quantity { get; set; }
        public String Warehouse { get; set; }
        //public Warehouse Warehouse { get; set; }

        public AlterStockRequest(){ }
        public AlterStockRequest(Stock stock)
        {
            ProductGuid = stock.ProductID;
            Quantity = stock.Quantity;
            Warehouse = stock.Warehouse;
        }
    }
}

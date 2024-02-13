using Domain.Entities;

namespace Domain.DTOs
{
    public class AlterStockDTO
    {
        public Guid ProductGuid { get; set; }
        public int Quantity { get; set; }
        public Warehouse Warehouse { get; set; }
    }
}

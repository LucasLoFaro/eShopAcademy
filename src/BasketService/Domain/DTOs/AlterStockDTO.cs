using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs
{
    public class AlterStockDTO
    {
        public Guid ProductGuid { get; set; }
        public int Quantity { get; set; }
        public String Warehouse { get; set; }
        //public Warehouse Warehouse { get; set; }
    }
}

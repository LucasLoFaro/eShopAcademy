using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs
{
    public class ProductDTO
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }

    }
}

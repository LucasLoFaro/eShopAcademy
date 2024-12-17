using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Domain.Entities
{
    public class Item
    {
        public Product Product { get; set; }
        public int Stock { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Domain.Entities
{
    public class Basket
    {
        public Guid ClientID { get; set; }
        public List<Item> Items { get; set; }
    }
}

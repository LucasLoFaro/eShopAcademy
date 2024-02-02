using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Stock
    {
        //!! Puede pasar a dato tipo GUID
        string ProductGuid { get; set; }
        int Quantity { get; set; }

        //!! Puede pasar a tener su propia collection
        string Warehouse { get; set; }
    }
}

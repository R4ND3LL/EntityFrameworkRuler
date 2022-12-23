using System;
using System.Collections.Generic;

namespace NorthwindModel.Models
{
    public partial class Shipper
    {
        public Shipper()
        {
            OrdersCustoms = new HashSet<Order>();
        }

        public int ShipperID { get; set; }
        public string CompanyName { get; set; }
        public string Phone { get; set; }

        public virtual ICollection<Order> OrdersCustoms { get; set; }
    }
}

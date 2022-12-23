using System;
using System.Collections.Generic;

namespace NorthwindModel.Models
{
    public partial class Order
    {
        public Order()
        {
            OrderDetailsCustoms = new HashSet<Order_Detail>();
        }

        public int OrderID { get; set; }
        public string CustomerID { get; set; }
        public int? EmployeeID { get; set; }
        public DateTime? OrderDateCustom { get; set; }
        public DateTime? RequiredDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public int? ShipViaCustom { get; set; }
        public decimal? Freight { get; set; }
        public string ShipName { get; set; }
        public string ShipAddress { get; set; }
        public string ShipCity { get; set; }
        public string ShipRegion { get; set; }
        public string ShipPostalCode { get; set; }
        public string ShipCountry { get; set; }

        public virtual Employee CustomEmployee { get; set; }
        public virtual Shipper CustomShipper { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual ICollection<Order_Detail> OrderDetailsCustoms { get; set; }
    }
}

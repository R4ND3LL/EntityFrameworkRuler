using System;
using System.Collections.Generic;

namespace FirebirdTestDesign.Models
{
    public partial class Sale
    {
        public string PoNumber { get; set; }
        public int CustNo { get; set; }
        public short? SalesRep { get; set; }
        public string OrderStatus { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ShipDate { get; set; }
        public DateTime? DateNeeded { get; set; }
        public string Paid { get; set; }
        public int QtyOrdered { get; set; }
        public decimal TotalValue { get; set; }
        public float Discount { get; set; }
        public string ItemType { get; set; }
        public decimal? Aged { get; set; }

        public virtual Customer CustNoNavigation { get; set; }
        public virtual Employee SalesRepNavigation { get; set; }
    }
}

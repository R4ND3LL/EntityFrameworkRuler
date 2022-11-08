namespace NorthwindTestProject.Models;

using System;
using System.Collections.Generic;

public partial class Order_Detail {
    public int OrderID { get; set; }
    public int ProductID { get; set; }
    public decimal UnitPrice { get; set; }
    public short Quantity { get; set; }
    public float Discount { get; set; }

    public virtual Order OrderIDNavigation { get; set; }
    public virtual Products ProductIDNavigation { get; set; }
}
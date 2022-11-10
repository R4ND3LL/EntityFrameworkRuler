namespace NorthwindTestProject.Models;

using System;
using System.Collections.Generic;

public partial class Order {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
        "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public Order() {
        Order_DetailsNavigation = new HashSet<Order_Detail>();
    }

    public int OrderID { get; set; }
    public string CustomerID { get; set; }
    public Nullable<int> EmployeeID { get; set; }
    public Nullable<DateTime> OrderDate { get; set; }
    public Nullable<DateTime> RequiredDate { get; set; }
    public Nullable<DateTime> ShippedDate { get; set; }

    /// <summary> should rename to ShipViaFk via PRIMITIVE rules only </summary>
    public Nullable<int> ShipVia { get; set; }

    /// <summary> should reference enum </summary>
    public byte Freight { get; set; }

    public string ShipName { get; set; }
    public string ShipAddress { get; set; }
    public string ShipCity { get; set; }
    public string ShipRegion { get; set; }
    public string ShipPostalCode { get; set; }
    public string ShipCountry { get; set; }

    /// <summary> Should rename to Customer </summary>
    public virtual Customers CustomerNavigation { get; set; }

    public virtual Employees EmployeeNavigation { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
    public virtual ICollection<Order_Detail> Order_DetailsNavigation { get; set; }

    public virtual Shipper Shippers { get; set; }
}
namespace NorthwindTestProject.Models;

using System;
using System.Collections.Generic;

public partial class Product {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
        "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public Product() {
        Order_Detail = new HashSet<Order_Detail>();
    }

    public int ProductID { get; set; }
    public string ProductName { get; set; }
    public Nullable<int> SupplierID { get; set; }
    public Nullable<int> CategoryID { get; set; }
    public string QuantityPerUnit { get; set; }
    public Nullable<decimal> UnitPrice { get; set; }
    public Nullable<short> UnitsInStock { get; set; }
    public Nullable<short> UnitsOnOrder { get; set; }

    /// <summary> should reference enum </summary>
    // ReSharper disable once ConvertNullableToShortForm
    public Nullable<short> ReorderLevel { get; set; }

    public bool Discontinued { get; set; }

    public virtual Category CategoryIDNavigation { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
    public virtual ICollection<Order_Detail> Order_Detail { get; set; }

    public virtual Supplier SupplierIDNavigation { get; set; }
}
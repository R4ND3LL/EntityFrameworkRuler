using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace NorthwindTestProject.Models;

using System;
using System.Collections.Generic;
using System.Linq;

public partial class Products {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
        "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public Products() {
        Order_Details = new HashSet<Order_Detail>();
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

    public virtual Categories CategoryNavigation { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
    public virtual ICollection<Order_Detail> Order_Details { get; set; }

    public virtual Supplier SupplierNavigation { get; set; }

    public void SomeMethod() {
        var list = new List<Categories>();
        list[0].ProductsNavigation.Clear(); // generic rename challenge
        var list2 = new List<Products>();
        list2[0].CategoryNavigation = null; // generic rename challenge

        var productWrapper = new ModelWrapper<Products>(this);
        var products = productWrapper.Model.CategoryNavigation.ProductsNavigation;
        var cat1= products[0].CategoryNavigation;
        var cat2= products.First().CategoryNavigation;
        
        var categoryWrapper = new ModelWrapper<Categories>(list2[0].CategoryNavigation);
        var category = categoryWrapper.Model.ProductsNavigation[0].CategoryNavigation;
    }
}

public class ModelWrapper<T> {
    public ModelWrapper(T m) { Model = m; }
    public T Model { get; }
}
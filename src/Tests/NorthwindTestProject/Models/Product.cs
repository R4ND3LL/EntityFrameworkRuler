using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace NorthwindTestProject.Models;

using System;
using System.Collections.Generic;
using System.Linq;

public partial class Products {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
        "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public Products() {
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

    public virtual Categories CategoryIDNavigation { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
    public virtual ICollection<Order_Detail> Order_Detail { get; set; }

    public virtual Supplier SupplierIDNavigation { get; set; }

    public void SomeMethod() {
        var list = new List<Categories>();
        list[0].ProductCategoryIDNavigations.Clear(); // generic rename challenge
        var list2 = new List<Products>();
        list2[0].CategoryIDNavigation = null; // generic rename challenge

        var productWrapper = new ModelWrapper<Products>(this);
        var products = productWrapper.Model.CategoryIDNavigation.ProductCategoryIDNavigations;
        var cat1= products[0].CategoryIDNavigation;
        var cat2= products.First().CategoryIDNavigation;
        
        var categoryWrapper = new ModelWrapper<Categories>(list2[0].CategoryIDNavigation);
        var category = categoryWrapper.Model.ProductCategoryIDNavigations[0].CategoryIDNavigation;
    }
}

public class ModelWrapper<T> {
    public ModelWrapper(T m) { Model = m; }
    public T Model { get; }
}
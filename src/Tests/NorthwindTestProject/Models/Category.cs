namespace NorthwindTestProject.Models;

using System;
using System.Collections.Generic;

public partial class Categories {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
        "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public Categories() {
        ProductCategoryIDNavigations = new List<Products>();
    }

    public int CategoryID { get; set; }
    public string CategoryName { get; set; }
    public string Description { get; set; }
    public byte[] Picture { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
    public virtual IList<Products> ProductCategoryIDNavigations { get; set; }
}
namespace NorthwindTestProject.Models;

using System;
using System.Collections.Generic;

public partial class CustomerDemographic {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
        "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public CustomerDemographic() {
        Customers = new HashSet<Customers>();
    }

    public string CustomerTypeID { get; set; }
    public string CustomerDesc { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
    public virtual ICollection<Customers> Customers { get; set; }
}
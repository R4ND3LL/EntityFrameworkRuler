namespace NorthwindTestProject.Models;

using System;
using System.Collections.Generic;

public partial class Territory {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
        "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public Territory() {
        Employees = new HashSet<Employees>();
    }

    public string TerritoryID { get; set; }
    public string TerritoryDescription { get; set; }
    public int RegionID { get; set; }

    public virtual Region Region { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
    public virtual ICollection<Employees> Employees { get; set; }
}
namespace NorthwindTestProject.Models;

using System;
using System.Collections.Generic;

public partial class Region {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
        "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public Region() {
        TerritoriesRegionIDNavigations = new HashSet<Territory>();
    }

    public int RegionID { get; set; }
    public string RegionDescription { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
    public virtual ICollection<Territory> TerritoriesRegionIDNavigations { get; set; }
}
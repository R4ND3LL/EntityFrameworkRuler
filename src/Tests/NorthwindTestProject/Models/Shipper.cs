namespace NorthwindTestProject.Models;

using System;
using System.Collections.Generic;

public partial class Shipper {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
        "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public Shipper() {
        OrderShipViaFkNavigations = new HashSet<Order>();
    }

    public int ShipperID { get; set; }
    public string CompanyName { get; set; }
    public string Phone { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
    public virtual ICollection<Order> OrderShipViaFkNavigations { get; set; }
}
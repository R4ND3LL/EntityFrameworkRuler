namespace NorthwindTestProject.Models;

using System;
using System.Collections.Generic;

public partial class Employee {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
        "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public Employee() {
        ReportsToFkNavigations = new HashSet<Employee>();
        OrderEmployeeIDNavigations = new HashSet<Order>();
        Territories = new HashSet<Territory>();
    }

    public int EmployeeID { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string Title { get; set; }
    public string TitleOfCourtesy { get; set; }
    public Nullable<DateTime> BirthDate { get; set; }
    public Nullable<DateTime> HireDate { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string Region { get; set; }
    public string PostalCode { get; set; }
    public string Country { get; set; }
    public string HomePhone { get; set; }
    public string Extension { get; set; }
    public byte[] Photo { get; set; }
    public string Notes { get; set; }

    /// <summary> should rename to ReportsToFk </summary>
    public Nullable<int> ReportsTo { get; set; }

    public string PhotoPath { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
    public virtual ICollection<Employee> ReportsToFkNavigations { get; set; }

    public virtual Employee ReportsToFkNavigation { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
    public virtual ICollection<Order> OrderEmployeeIDNavigations { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
    public virtual ICollection<Territory> Territories { get; set; }
}
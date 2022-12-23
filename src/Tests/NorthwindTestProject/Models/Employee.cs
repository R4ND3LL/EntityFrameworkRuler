using System;
using System.Collections.Generic;

namespace NorthwindModel.Models
{
    public partial class Employee
    {
        public Employee()
        {
            InverseEmployeeOneCustom = new HashSet<Employee>();
            Orders = new HashSet<Order>();
            Territories = new HashSet<Territory>();
        }

        public int EmployeeID { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Title { get; set; }
        public string TitleOfCourtesy { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime? HireDate { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string HomePhone { get; set; }
        public string Extension { get; set; }
        public byte[] Photo { get; set; }
        public string Notes { get; set; }
        public int? ReportsToCustom { get; set; }
        public string PhotoPath { get; set; }

        public virtual Employee EmployeeOneCustom { get; set; }
        public virtual ICollection<Employee> InverseEmployeeOneCustom { get; set; }
        public virtual ICollection<Order> Orders { get; set; }

        public virtual ICollection<Territory> Territories { get; set; }
    }
}

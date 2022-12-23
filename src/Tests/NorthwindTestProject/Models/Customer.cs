using System;
using System.Collections.Generic;

namespace NorthwindModel.Models
{
    public partial class Customer
    {
        public Customer()
        {
            OrdersCustoms = new HashSet<Order>();
            CustomerTypes = new HashSet<CustomerDemographic>();
        }

        public string CustomerID { get; set; }
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public string ContactTitle { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }

        public virtual ICollection<Order> OrdersCustoms { get; set; }

        public virtual ICollection<CustomerDemographic> CustomerTypes { get; set; }
    }
}

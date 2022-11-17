using System;
using System.Collections.Generic;

namespace NorthwindTestProject.Models;

public partial class CustomerDemographic
{
    public string CustomerTypeId { get; set; }

    public string CustomerDesc { get; set; }

    public virtual ICollection<Customer> Customers { get; } = new List<Customer>();
}

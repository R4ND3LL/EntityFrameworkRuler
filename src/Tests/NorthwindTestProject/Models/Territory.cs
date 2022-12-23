using System;
using System.Collections.Generic;

namespace NorthwindModel.Models
{
    public partial class Territory
    {
        public Territory()
        {
            Employees = new HashSet<Employee>();
        }

        public string TerritoryID { get; set; }
        public string TerritoryDescription { get; set; }
        public int RegionID { get; set; }

        public virtual RegionCustom RegionCustom { get; set; }

        public virtual ICollection<Employee> Employees { get; set; }
    }
}

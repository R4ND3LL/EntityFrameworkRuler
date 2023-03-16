using System;
using System.Collections.Generic;

namespace FirebirdTestDesign.Models
{
    public partial class SalaryHistory
    {
        public short EmpNo { get; set; }
        public DateTime ChangeDate { get; set; }
        public string UpdaterId { get; set; }
        public decimal OldSalary { get; set; }
        public double PercentChange { get; set; }
        public double? NewSalary { get; set; }

        public virtual Employee EmpNoNavigation { get; set; }
    }
}

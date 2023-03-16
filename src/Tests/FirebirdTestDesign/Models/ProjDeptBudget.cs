using System;
using System.Collections.Generic;

namespace FirebirdTestDesign.Models
{
    public partial class ProjDeptBudget
    {
        public int FiscalYear { get; set; }
        public string ProjId { get; set; }
        public string DeptNo { get; set; }
        public int? QuartHeadCnt { get; set; }
        public decimal? ProjectedBudget { get; set; }

        public virtual Department DeptNoNavigation { get; set; }
        public virtual Project Proj { get; set; }
    }
}

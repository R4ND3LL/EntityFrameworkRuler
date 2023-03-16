using System;
using System.Collections.Generic;

namespace FirebirdTestDesign.Models
{
    public partial class PROJECT
    {
        public PROJECT()
        {
            PROJ_DEPT_BUDGET = new HashSet<PROJ_DEPT_BUDGET>();
            EmpNos = new HashSet<EMPLOYEE>();
        }

        public string PROJ_ID { get; set; }
        public string PROJ_NAME { get; set; }
        public string PROJ_DESC { get; set; }
        public short? TEAM_LEADER { get; set; }
        public string PRODUCT { get; set; }

        public virtual EMPLOYEE EMPLOYEE { get; set; }
        public virtual ICollection<PROJ_DEPT_BUDGET> PROJ_DEPT_BUDGET { get; set; }

        public virtual ICollection<EMPLOYEE> EmpNos { get; set; }
    }
}

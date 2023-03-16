using System;
using System.Collections.Generic;

namespace FirebirdTestDesign.Models
{
    public partial class DEPARTMENT
    {
        public DEPARTMENT()
        {
            DEPARTMENT11 = new HashSet<DEPARTMENT>();
            EMPLOYEE = new HashSet<EMPLOYEE>();
            PROJ_DEPT_BUDGET = new HashSet<PROJ_DEPT_BUDGET>();
        }

        public string DEPT_NO { get; set; }
        public string DEPARTMENT1 { get; set; }
        public string HEAD_DEPT { get; set; }
        public short? MNGR_NO { get; set; }
        public decimal? BUDGET { get; set; }
        public string LOCATION { get; set; }
        public string PHONE_NO { get; set; }

        public virtual DEPARTMENT DEPARTMENT2 { get; set; }
        public virtual EMPLOYEE EMPLOYEE1 { get; set; }
        public virtual ICollection<DEPARTMENT> DEPARTMENT11 { get; set; }
        public virtual ICollection<EMPLOYEE> EMPLOYEE { get; set; }
        public virtual ICollection<PROJ_DEPT_BUDGET> PROJ_DEPT_BUDGET { get; set; }
    }
}

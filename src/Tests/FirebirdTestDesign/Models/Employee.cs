using System;
using System.Collections.Generic;

namespace FirebirdTestDesign.Models
{
    public partial class EMPLOYEE
    {
        public EMPLOYEE()
        {
            DEPARTMENT1 = new HashSet<DEPARTMENT>();
            PROJECT = new HashSet<PROJECT>();
            SALARY_HISTORY = new HashSet<SALARY_HISTORY>();
            SALES = new HashSet<SALES>();
            Projs = new HashSet<PROJECT>();
        }

        public short EMP_NO { get; set; }
        public string FIRST_NAME { get; set; }
        public string LAST_NAME { get; set; }
        public string PHONE_EXT { get; set; }
        public DateTime HIRE_DATE { get; set; }
        public string DEPT_NO { get; set; }
        public string JOB_CODE { get; set; }
        public short JOB_GRADE { get; set; }
        public string JOB_COUNTRY { get; set; }
        public decimal SALARY { get; set; }
        public string FULL_NAME { get; set; }

        public virtual DEPARTMENT DEPARTMENT { get; set; }
        public virtual JOB JOB { get; set; }
        public virtual ICollection<DEPARTMENT> DEPARTMENT1 { get; set; }
        public virtual ICollection<PROJECT> PROJECT { get; set; }
        public virtual ICollection<SALARY_HISTORY> SALARY_HISTORY { get; set; }
        public virtual ICollection<SALES> SALES { get; set; }

        public virtual ICollection<PROJECT> Projs { get; set; }
    }
}

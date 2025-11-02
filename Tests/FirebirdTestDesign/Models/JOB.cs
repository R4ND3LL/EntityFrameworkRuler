using System;
using System.Collections.Generic;

namespace FirebirdTestDesign.Models
{
    public partial class JOB
    {
        public JOB()
        {
            EMPLOYEE = new HashSet<EMPLOYEE>();
        }

        public string JOB_CODE { get; set; }
        public short JOB_GRADE { get; set; }
        public string JOB_COUNTRY { get; set; }
        public string JOB_TITLE { get; set; }
        public decimal MIN_SALARY { get; set; }
        public decimal MAX_SALARY { get; set; }
        public string JOB_REQUIREMENT { get; set; }
        public string LANGUAGE_REQ { get; set; }

        public virtual COUNTRY COUNTRY { get; set; }
        public virtual ICollection<EMPLOYEE> EMPLOYEE { get; set; }
    }
}

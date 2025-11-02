using System;
using System.Collections.Generic;

namespace FirebirdTestDesign.Models
{
    public partial class SALARY_HISTORY
    {
        public short EMP_NO { get; set; }
        public DateTime CHANGE_DATE { get; set; }
        public string UPDATER_ID { get; set; }
        public decimal OLD_SALARY { get; set; }
        public double PERCENT_CHANGE { get; set; }
        public double? NEW_SALARY { get; set; }

        public virtual EMPLOYEE EMPLOYEE { get; set; }
    }
}

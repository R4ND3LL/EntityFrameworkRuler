using System;
using System.Collections.Generic;

namespace FirebirdTestDesign.Models
{
    public partial class PROJ_DEPT_BUDGET
    {
        public int FISCAL_YEAR { get; set; }
        public string PROJ_ID { get; set; }
        public string DEPT_NO { get; set; }
        public int? QUART_HEAD_CNT { get; set; }
        public decimal? PROJECTED_BUDGET { get; set; }

        public virtual DEPARTMENT DEPARTMENT { get; set; }
        public virtual PROJECT PROJECT { get; set; }
    }
}

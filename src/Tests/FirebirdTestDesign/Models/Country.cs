using System;
using System.Collections.Generic;

namespace FirebirdTestDesign.Models
{
    public partial class COUNTRY
    {
        public COUNTRY()
        {
            CUSTOMER = new HashSet<CUSTOMER>();
            JOB = new HashSet<JOB>();
        }

        public string COUNTRY1 { get; set; }
        public string CURRENCY { get; set; }

        public virtual ICollection<CUSTOMER> CUSTOMER { get; set; }
        public virtual ICollection<JOB> JOB { get; set; }
    }
}

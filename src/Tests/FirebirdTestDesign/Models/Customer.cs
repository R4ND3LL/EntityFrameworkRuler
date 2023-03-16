using System;
using System.Collections.Generic;

namespace FirebirdTestDesign.Models
{
    public partial class CUSTOMER
    {
        public CUSTOMER()
        {
            SALES = new HashSet<SALES>();
        }

        public int CUST_NO { get; set; }
        public string CUSTOMER1 { get; set; }
        public string CONTACT_FIRST { get; set; }
        public string CONTACT_LAST { get; set; }
        public string PHONE_NO { get; set; }
        public string ADDRESS_LINE1 { get; set; }
        public string ADDRESS_LINE2 { get; set; }
        public string CITY { get; set; }
        public string STATE_PROVINCE { get; set; }
        public string COUNTRY { get; set; }
        public string POSTAL_CODE { get; set; }
        public string ON_HOLD { get; set; }

        public virtual COUNTRY COUNTRY1 { get; set; }
        public virtual ICollection<SALES> SALES { get; set; }
    }
}

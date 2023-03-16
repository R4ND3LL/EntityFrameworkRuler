using System;
using System.Collections.Generic;

namespace FirebirdTestDesign.Models
{
    public partial class SALES
    {
        public string PO_NUMBER { get; set; }
        public int CUST_NO { get; set; }
        public short? SALES_REP { get; set; }
        public string ORDER_STATUS { get; set; }
        public DateTime ORDER_DATE { get; set; }
        public DateTime? SHIP_DATE { get; set; }
        public DateTime? DATE_NEEDED { get; set; }
        public string PAID { get; set; }
        public int QTY_ORDERED { get; set; }
        public decimal TOTAL_VALUE { get; set; }
        public float DISCOUNT { get; set; }
        public string ITEM_TYPE { get; set; }
        public decimal? AGED { get; set; }

        public virtual CUSTOMER CUSTOMER { get; set; }
        public virtual EMPLOYEE EMPLOYEE { get; set; }
    }
}

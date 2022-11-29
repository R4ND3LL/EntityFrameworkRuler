using System;
using System.Collections.Generic;

namespace NorthwindModel.Models;

public partial class ProductsAboveAveragePrice
{
    public string ProductName { get; set; }

    public decimal? UnitPrice { get; set; }
}

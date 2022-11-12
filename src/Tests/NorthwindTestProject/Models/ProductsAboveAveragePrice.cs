using System;
using System.Collections.Generic;

namespace NorthwindTestProject.Models;

public partial class ProductsAboveAveragePrice
{
    public string ProductName { get; set; }

    public decimal? UnitPrice { get; set; }
}

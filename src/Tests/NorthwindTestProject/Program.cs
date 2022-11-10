using System;
using Microsoft.EntityFrameworkCore;
using NorthwindTestProject.Data;
using NorthwindTestProject.Models;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable CollectionNeverUpdated.Local
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException

namespace NorthwindTestProject;

internal class Program {
    private static void Main() {
        /***************************************************************************************/
        /********* Do not run this project.  It is a target for rule application only. *********/
        /***************************************************************************************/

        using var dbContext = new NorthwindDbContext(new DbContextOptions<NorthwindDbContext>());

        // ReSharper disable once SuggestVarOrType_SimpleTypes
        Order order = dbContext.Orders.FirstOrDefault();
        order.ShipVia = 1;
        Products product = new Products();
        Categories category = product.CategoryNavigation;
        var allCategoryProducts = category.ProductsNavigation;

        Console.WriteLine($"Some comment {nameof(Products.CategoryNavigation)}");
        Console.WriteLine($"Some comment {nameof(Categories.ProductsNavigation)}");

        if (allCategoryProducts?.Count > 0) {
            var list = new List<Categories>();
            list[0].ProductsNavigation.Clear();
        }

        Console.WriteLine("This is a fake test project to illustrate rule application only!");
    }
}
using System;
using Microsoft.EntityFrameworkCore;
using NorthwindTestProject.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Design;

[assembly: DesignTimeServicesReference("EntityFrameworkRuler.EfRulerDesignTimeServices, EfRuler")]
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
        Product product = new Product();
        var category = product.Category;
        var allCategoryProducts = category.Products;

        Console.WriteLine($"Some comment {nameof(Product.Category)}");
        Console.WriteLine($"Some comment {nameof(Category.Products)}");

        if (allCategoryProducts?.Count > 0) {
            var list = new List<Category>();
            list[0].Products.Clear();
        }

        Console.WriteLine("This is a fake test project to illustrate rule application only!");
    }
}
using Microsoft.EntityFrameworkCore;
using NorthwindTestProject.Data;
using NorthwindTestProject.Models;

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
        Category category = product.CategoryIDNavigation;
        var allCategoryProducts = category.ProductCategoryIDNavigations;

        Console.WriteLine("This is a fake test project to illustrate rule application only!");
    }
}
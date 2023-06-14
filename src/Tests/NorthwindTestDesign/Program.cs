using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NorthwindModel.Context;
using NorthwindModel.Models;

[assembly: DesignTimeServicesReference("EntityFrameworkRuler.Design.RuledDesignTimeServices, EntityFrameworkRuler.Design")]
// ReSharper disable CollectionNeverUpdated.Local
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException

namespace NorthwindModel;

internal class Program {
    private static async Task Main() {
        /***************************************************************************************/
        /********* Do not run this project.  It is a target for scaffolding only. *********/
        /***************************************************************************************/

        Console.WriteLine("This is a fake test project to illustrate rule application only!");

#if DEBUGNORTHWIND||false
        using var dbContext = new NorthwindEntities(new DbContextOptions<NorthwindEntities>());

        // ReSharper disable once SuggestVarOrType_SimpleTypes
        Order order = dbContext.Orders.FirstOrDefault();

        var animals = await dbContext.Animals.ToListAsync();
        var cats = animals.OfType<Cat>().ToList();
        var dogs = animals.OfType<Dog>().ToList();
        Debug.Assert(animals.Count > 0);
        Debug.Assert(cats.Count > 0);
        Debug.Assert(dogs.Count > 0);

        var categories = await dbContext.Categories.ToListAsync();
        Debug.Assert(categories.Count > 0);

        var res = await dbContext.Functions.ReturnNumberOneNamed(45);
        Debug.Assert(res.Count == 1 && res[0].Num == 45);
        var res2 = await dbContext.Functions.ReturnNumberOne();
        Debug.Assert(res2.Count == 1 && res2[0].Value0 == 1);
        var formattedNumber = await dbContext.Functions.FormatNumber(5);
        Debug.Assert(formattedNumber == "000-000-005");

        res = await dbContext.Functions.ReturnNumberOneNamed(45);
        Debug.Assert(res.Count == 1 && res[0].Num == 45);

        var TenMostExpensiveProducts = await dbContext.Functions.TenMostExpensiveProducts();
        Debug.Assert(TenMostExpensiveProducts.Count == 10 && TenMostExpensiveProducts[0].UnitPrice > 0);

        var SalesByCategory = await dbContext.Functions.SalesByCategory("Beverages", "1996");
        Debug.Assert(SalesByCategory.Count > 10 && SalesByCategory[0].TotalPurchase > 1000);

        //var FnTableValued = await dbContext.Functions.FnTableValued(7);
        //Debug.Assert(FnTableValued == "5");


        var customers = await dbContext.Customers.ToListAsync();
        var greens = customers.OfType<CustomerGreen>().ToList();
        var reds = customers.OfType<CustomerRed>().ToList();
        Debug.Assert(customers.Count > 0);
        Debug.Assert(greens.Count > 0);
        Debug.Assert(reds.Count > 0);
        Debug.Assert((reds.Count + greens.Count )== customers.Count);

#endif
    }
}
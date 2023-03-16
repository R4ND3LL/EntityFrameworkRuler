using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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

#if false
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

        var customers = await dbContext.Customers.ToListAsync();
        var greens = customers.OfType<CustomerGreen>().ToList();
        var reds = customers.OfType<CustomerRed>().ToList();
        Debug.Assert(customers.Count > 0);
        Debug.Assert(greens.Count > 0);
        Debug.Assert(reds.Count > 0);


#endif
    }
}
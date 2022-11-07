using Microsoft.EntityFrameworkCore;
using NorthwindTestProject.Data;
using NorthwindTestProject.Models;

namespace NorthwindTestProject;

internal class Program {
    private static void Main() {
        using var dbContext = new NorthwindDbContext(new DbContextOptions<NorthwindDbContext>());

        // ReSharper disable once SuggestVarOrType_SimpleTypes
        Order order = dbContext.Orders.FirstOrDefault();
        order.ShipVia = 1;

        Console.WriteLine("This is a fake test project to illustrate rule application only!");
    }
}
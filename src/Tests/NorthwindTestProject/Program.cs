using Microsoft.EntityFrameworkCore;
using NorthwindTestProject.Data;

namespace NorthwindTestProject; 

internal class Program {
    private static void Main() {
        using var dbContext = new NorthwindDbContext(new DbContextOptions<NorthwindDbContext>());

        Console.WriteLine("This is a fake test project to illustrate rule application only!");
    }
}
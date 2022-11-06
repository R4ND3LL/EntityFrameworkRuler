using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NorthwindTestProject.Models;

namespace NorthwindTestProject.Data;

public sealed class NorthwindDbContext : DbContext {
    public DbSet<Category> Categories { get; set; }
    public DbSet<Customer> Customer { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Order_Detail> OrderDetails { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Region> Regions { get; set; }
    public DbSet<Shipper> Shippers { get; set; }
    public DbSet<Territory> Territories { get; set; }

    public NorthwindDbContext(DbContextOptions options) : base(options) {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
#if DEBUG
        optionsBuilder.LogTo(Console.WriteLine).EnableSensitiveDataLogging().EnableDetailedErrors();
#endif
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new()) {
        //var user = sessionManager?.UserSession?.User?.Username;
        foreach (var entry in ChangeTracker.Entries<Employee>())
            if (entry.Entity.ReportsTo == null)
                entry.Entity.ReportsTo = 1;

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        // tap into the Configurations installed in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
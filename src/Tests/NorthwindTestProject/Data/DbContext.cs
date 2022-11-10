using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NorthwindTestProject.Models;

namespace NorthwindTestProject.Data;

public sealed class NorthwindDbContext : DbContext {
    public DbSet<Categories> Categories { get; set; }
    public DbSet<Customers> Customer { get; set; }
    public DbSet<Employees> Employees { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Order_Detail> OrderDetails { get; set; }
    public DbSet<Products> Products { get; set; }
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
        foreach (var entry in ChangeTracker.Entries<Employees>()) {
            if (entry.Entity.OrdersNavigation.Count == 0)
                throw new Exception(nameof(Models.Employees.OrdersNavigation));
            if (entry.Entity.ReportsToNavigations.Count == 0)
                throw new Exception(nameof(Models.Employees.ReportsToNavigations));
            if (entry.Entity.ReportsTo == null)
                entry.Entity.ReportsTo = 1; // this should rename with primitive rules
        }

        foreach (var entry in ChangeTracker.Entries<Order>()) {
            if (entry.Entity.EmployeeNavigation == null) throw new Exception(nameof(Order.EmployeeNavigation));
            if (entry.Entity.ShipVia == null)
                entry.Entity.ShipVia = 1; // this should rename with primitive rules
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        // tap into the Configurations installed in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
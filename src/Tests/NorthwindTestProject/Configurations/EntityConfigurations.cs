using NorthwindTestProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NorthwindTestProject.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Categories> {
    public void Configure(EntityTypeBuilder<Categories> builder) {
        builder.ToTable("Categories");
        builder.Property(o => o.CategoryID).IsRequired();
        builder.HasMany(o => o.ProductsNavigation).WithOne(o => o.CategoryNavigation);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employees> {
    public void Configure(EntityTypeBuilder<Employees> builder) {
        builder.ToTable("Employees");
        builder.HasIndex(o => o.FirstName).IsUnique();
        builder.HasIndex(o => o.LastName).IsUnique();
        builder.Property(o => o.ReportsTo).IsRequired();
        builder.OwnsMany(o => o.OrdersNavigation).HasOne(o => o.EmployeeNavigation);
        builder.OwnsMany(o => o.ReportsToNavigations).HasOne(o => o.InverseReportsToFk);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order> {
    public void Configure(EntityTypeBuilder<Order> builder) {
        builder.ToTable("Orders");
        builder.Property(o => o.ShipViaCustom).IsRequired();
        builder.HasOne(o => o.CustomerNavigation).WithMany(o => o.OrdersNavigation);
        builder.HasOne(o => o.EmployeeNavigation).WithMany(o => o.OrdersNavigation);
        builder.HasMany(o => o.Order_DetailsNavigation).WithOne(o => o.OrderNavigation);
    }
}

public class SuppliersConfiguration : IEntityTypeConfiguration<Supplier> {
    public void Configure(EntityTypeBuilder<Supplier> builder) {
        builder.ToTable("Suppliers");
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customers> {
    public void Configure(EntityTypeBuilder<Customers> builder) {
        builder.ToTable("Customers");
        builder.OwnsMany(o => o.OrdersNavigation).HasOne(o => o.CustomerNavigation);
    }
}

public class Order_DetailConfiguration : IEntityTypeConfiguration<Order_Detail> {
    public void Configure(EntityTypeBuilder<Order_Detail> builder) {
        builder.ToTable("OrderDetails");
    }
}

public class RegionConfiguration : IEntityTypeConfiguration<RegionCustom> {
    public void Configure(EntityTypeBuilder<RegionCustom> builder) {
        builder.ToTable("Regions");
        builder.OwnsMany(o => o.TerritoriesNavigation).HasOne(o => o.Region);
    }
}

public class ShipperConfiguration : IEntityTypeConfiguration<Shipper> {
    public void Configure(EntityTypeBuilder<Shipper> builder) {
        builder.ToTable("Shippers");
        builder.OwnsMany(o => o.OrdersNavigation).HasOne(o => o.Shippers);
    }
}
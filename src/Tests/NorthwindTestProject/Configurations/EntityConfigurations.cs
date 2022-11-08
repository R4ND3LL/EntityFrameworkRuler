using NorthwindTestProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NorthwindTestProject.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Categories> {
    public void Configure(EntityTypeBuilder<Categories> builder) {
        builder.ToTable("Categories");
        builder.Property(o => o.CategoryID).IsRequired();
        builder.HasMany(o => o.ProductCategoryIDNavigations).WithOne(o => o.CategoryIDNavigation);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employees> {
    public void Configure(EntityTypeBuilder<Employees> builder) {
        builder.ToTable("Employees");
        builder.HasIndex(o => o.FirstName).IsUnique();
        builder.HasIndex(o => o.LastName).IsUnique();
        builder.Property(o => o.ReportsTo).IsRequired();
        builder.OwnsMany(o => o.OrderEmployeeIDNavigations).HasOne(o => o.EmployeeIDNavigation);
        builder.OwnsMany(o => o.ReportsToFkNavigations).HasOne(o => o.ReportsToFkNavigation);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order> {
    public void Configure(EntityTypeBuilder<Order> builder) {
        builder.ToTable("Orders");
        builder.Property(o => o.ShipVia).IsRequired();
        builder.HasOne(o => o.CustomerIDNavigation).WithMany(o => o.OrderCustomerIDNavigations);
        builder.HasOne(o => o.EmployeeIDNavigation).WithMany(o => o.OrderEmployeeIDNavigations);
        builder.HasMany(o => o.Order_Detail).WithOne(o => o.OrderIDNavigation);
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
        builder.OwnsMany(o => o.OrderCustomerIDNavigations).HasOne(o => o.CustomerIDNavigation);
    }
}

public class Order_DetailConfiguration : IEntityTypeConfiguration<Order_Detail> {
    public void Configure(EntityTypeBuilder<Order_Detail> builder) {
        builder.ToTable("OrderDetails");
    }
}

public class RegionConfiguration : IEntityTypeConfiguration<Region> {
    public void Configure(EntityTypeBuilder<Region> builder) {
        builder.ToTable("Regions");
        builder.OwnsMany(o => o.TerritoryRegionIDNavigations).HasOne(o => o.Region);
    }
}

public class ShipperConfiguration : IEntityTypeConfiguration<Shipper> {
    public void Configure(EntityTypeBuilder<Shipper> builder) {
        builder.ToTable("Shippers");
        builder.OwnsMany(o => o.OrderShipViaFkNavigations).HasOne(o => o.ShipViaFkNavigation);
    }
}
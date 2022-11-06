using NorthwindTestProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NorthwindTestProject.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category> {
    public void Configure(EntityTypeBuilder<Category> builder) {
        builder.ToTable("Categories");
        builder.Property(o => o.CategoryID).IsRequired();
        builder.HasMany(o => o.ProductCategoryIDNavigations).WithOne(o => o.Category);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee> {
    public void Configure(EntityTypeBuilder<Employee> builder) {
        builder.ToTable("Employees");
        builder.HasIndex(o => o.FirstName).IsUnique();
        builder.HasIndex(o => o.LastName).IsUnique();
        builder.Property(o => o.ReportsTo).IsRequired();
        builder.OwnsMany(o => o.Orders).HasOne(o => o.Employee);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order> {
    public void Configure(EntityTypeBuilder<Order> builder) {
        builder.ToTable("Orders");
        builder.HasOne(o => o.CustomerIDNavigation).WithMany(o => o.Orders);
    }
}

public class SuppliersConfiguration : IEntityTypeConfiguration<Supplier> {
    public void Configure(EntityTypeBuilder<Supplier> builder) {
        builder.ToTable("Suppliers");
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer> {
    public void Configure(EntityTypeBuilder<Customer> builder) {
        builder.ToTable("Customers");
    }
}

public class Order_DetailConfiguration : IEntityTypeConfiguration<Order_Detail> {
    public void Configure(EntityTypeBuilder<Order_Detail> builder) {
        builder.ToTable("OrderDetails");
    }
}
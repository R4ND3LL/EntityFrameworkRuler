using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NorthwindTestProject.Models;

namespace NorthwindTestProject.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Products> {
    public void Configure(EntityTypeBuilder<Products> builder) {
        builder.ToTable("Product");
        builder.Property(o => o.ReorderLevel).IsRequired();
        builder.HasOne(o => o.CategoryNavigation).WithMany(o => o.ProductsNavigation);
        builder.HasMany(o => o.Order_Details).WithOne(o => o.ProductNavigation);
        builder.HasOne(o => o.SupplierNavigation).WithMany(o => o.Products);

        // random code to test for reference updates:
        Products p = new Products();
        if (p.CategoryNavigation == null) throw new Exception(nameof(Products.CategoryNavigation));
        if (p.CategoryNavigation.ProductsNavigation[0].Order_Details.First().ProductNavigation == null)
            throw new Exception(nameof(Order_Detail.ProductNavigation));
    }
}
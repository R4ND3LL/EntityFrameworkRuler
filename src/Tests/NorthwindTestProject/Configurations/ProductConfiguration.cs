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
        builder.HasOne(o => o.CategoryIDNavigation).WithMany(o => o.ProductCategoryIDNavigations);
        builder.HasMany(o => o.Order_Detail).WithOne(o => o.ProductIDNavigation);
        builder.HasOne(o => o.SupplierIDNavigation).WithMany(o => o.Products);

        // random code to test for reference updates:
        Products p = new Products();
        if (p.CategoryIDNavigation == null) throw new Exception(nameof(Products.CategoryIDNavigation));
        if (p.CategoryIDNavigation.ProductCategoryIDNavigations[0].Order_Detail.First().ProductIDNavigation == null)
            throw new Exception(nameof(Order_Detail.ProductIDNavigation));
    }
}
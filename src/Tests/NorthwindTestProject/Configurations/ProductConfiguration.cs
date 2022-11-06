using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NorthwindTestProject.Models;

namespace NorthwindTestProject.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product> {
    public void Configure(EntityTypeBuilder<Product> builder) {
        builder.ToTable("Product");
        builder.Property(o => o.ReorderLevel).IsRequired();
    }
}
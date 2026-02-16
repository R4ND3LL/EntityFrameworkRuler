using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace NorthwindModel.Context;

public abstract class DataServiceBase {
    protected abstract void OnConfiguring(DbContextOptionsBuilder optionsBuilder);
    protected abstract void OnModelCreating(ModelBuilder modelBuilder);
}
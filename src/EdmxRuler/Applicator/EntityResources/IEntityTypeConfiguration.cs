using Microsoft.EntityFrameworkCore.Metadata.Builders;

public interface IEntityTypeConfiguration<TEntity> where TEntity : class {
    /// <summary>
    ///     Configures the entity of type <typeparamref name="TEntity" />.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    void Configure(EntityTypeBuilder<TEntity> builder);
}
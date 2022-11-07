using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Metadata.Builders; 

public interface IMutableProperty {
}

public class PropertyBuilder<TProperty> : PropertyBuilder {
    public PropertyBuilder(IMutableProperty property) : base(property) { }

    public virtual PropertyBuilder<TProperty> HasAnnotation(string annotation, object? value) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> IsRequired(bool required = true) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasMaxLength(int maxLength) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasPrecision(int precision, int scale) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasPrecision(int precision) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> IsUnicode(bool unicode = true) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> IsRowVersion() {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasValueGenerator<TGenerator>() where TGenerator : ValueGenerator {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasValueGenerator(
        Type? valueGeneratorType) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasValueGenerator(
        Func<IProperty, IEntityType, ValueGenerator> factory) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasValueGeneratorFactory<TFactory>()
        where TFactory : ValueGeneratorFactory {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasValueGeneratorFactory(
        Type? valueGeneratorFactoryType) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> IsConcurrencyToken(
        bool concurrencyToken = true) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> ValueGeneratedNever() {
        return this;
    }

    public virtual PropertyBuilder<TProperty> ValueGeneratedOnAdd() {
        return this;
    }

    public virtual PropertyBuilder<TProperty> ValueGeneratedOnAddOrUpdate() {
        return this;
    }

    public virtual PropertyBuilder<TProperty> ValueGeneratedOnUpdate() {
        return this;
    }

    public virtual PropertyBuilder<TProperty> ValueGeneratedOnUpdateSometimes() {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasField(string fieldName) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> UsePropertyAccessMode(
        PropertyAccessMode propertyAccessMode) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasConversion<TConversion>() {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasConversion(Type? providerClrType) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasConversion<TProvider>(
        Expression<Func<TProperty, TProvider>> convertToProviderExpression,
        Expression<Func<TProvider, TProperty>> convertFromProviderExpression) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasConversion<TProvider>(
        ValueConverter<TProperty, TProvider>? converter) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasConversion(ValueConverter? converter) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasConversion<TConversion>(
        ValueComparer? valueComparer) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasConversion(
        Type conversionType,
        ValueComparer? valueComparer) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasConversion<TProvider>(
        Expression<Func<TProperty, TProvider>> convertToProviderExpression,
        Expression<Func<TProvider, TProperty>> convertFromProviderExpression,
        ValueComparer? valueComparer) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasConversion<TProvider>(
        ValueConverter<TProperty, TProvider>? converter,
        ValueComparer? valueComparer) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasConversion(
        ValueConverter? converter,
        ValueComparer? valueComparer) {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasConversion<TConversion, TComparer>()
        where TComparer : ValueComparer {
        return this;
    }

    public virtual PropertyBuilder<TProperty> HasConversion(
        Type conversionType,
        Type? comparerType) {
        return this;
    }
}

public class PropertyBuilder {
    public PropertyBuilder(IMutableProperty property) { }
    public virtual IMutableProperty Metadata => default;

    public virtual PropertyBuilder HasAnnotation(string annotation, object? value) {
        return default;
    }

    public virtual PropertyBuilder IsRequired(bool required = true) {
        return default;
    }

    public virtual PropertyBuilder HasMaxLength(int maxLength) {
        return default;
    }

    public virtual PropertyBuilder HasPrecision(int precision, int scale) {
        return default;
    }

    public virtual PropertyBuilder HasPrecision(int precision) {
        return default;
    }

    public virtual PropertyBuilder IsUnicode(bool unicode = true) {
        return default;
    }

    public virtual PropertyBuilder IsRowVersion() {
        return default;
    }

    public virtual PropertyBuilder HasValueGenerator<TGenerator>() where TGenerator : ValueGenerator {
        return default;
    }

    public virtual PropertyBuilder HasValueGenerator(Type? valueGeneratorType) {
        return default;
    }

    public virtual PropertyBuilder HasValueGenerator(
        Func<IProperty, IEntityType, ValueGenerator> factory) {
        return default;
    }

    public virtual PropertyBuilder HasValueGeneratorFactory<TFactory>() where TFactory : ValueGeneratorFactory {
        return default;
    }

    public virtual PropertyBuilder HasValueGeneratorFactory(
        Type? valueGeneratorFactoryType) {
        return default;
    }

    public virtual PropertyBuilder IsConcurrencyToken(bool concurrencyToken = true) {
        return default;
    }

    /// </remarks>
    public virtual PropertyBuilder ValueGeneratedNever() {
        return default;
    }

    public virtual PropertyBuilder ValueGeneratedOnAdd() {
        return default;
    }

    public virtual PropertyBuilder ValueGeneratedOnAddOrUpdate() {
        return default;
    }

    public virtual PropertyBuilder ValueGeneratedOnUpdate() {
        return default;
    }

    public virtual PropertyBuilder ValueGeneratedOnUpdateSometimes() {
        return default;
    }

    public virtual PropertyBuilder HasField(string fieldName) {
        return default;
    }

    public virtual PropertyBuilder UsePropertyAccessMode(
        PropertyAccessMode propertyAccessMode) {
        return default;
    }

    public virtual PropertyBuilder HasConversion<TConversion>() {
        return default;
    }

    public virtual PropertyBuilder HasConversion(Type? conversionType) {
        return default;
    }

    public virtual PropertyBuilder HasConversion(ValueConverter? converter) {
        return default;
    }

    public virtual PropertyBuilder HasConversion<TConversion>(
        ValueComparer? valueComparer) {
        return default;
    }

    public virtual PropertyBuilder HasConversion(
        Type conversionType,
        ValueComparer? valueComparer) {
        return default;
    }

    public virtual PropertyBuilder HasConversion(
        ValueConverter? converter,
        ValueComparer? valueComparer) {
        return default;
    }

    public virtual PropertyBuilder HasConversion<TConversion, TComparer>() where TComparer : ValueComparer {
        return default;
    }

    public virtual PropertyBuilder HasConversion(
        Type conversionType,
        Type? comparerType) {
        return default;
    }
}
using System;
using System.Linq.Expressions;

// ReSharper disable CheckNamespace
namespace Microsoft.EntityFrameworkCore.Storage.ValueConversion {
    public class ValueConverter<TModel, TProvider> : ValueConverter {
        public Func<object?, object?> ConvertToProvider
            => default;

        public Func<object?, object?> ConvertFromProvider => default;
        public new virtual Expression<Func<TModel, TProvider>> ConvertToProviderExpression => default;
        public new virtual Expression<Func<TProvider, TModel>> ConvertFromProviderExpression => default;

        public Type ModelClrType
            => typeof(TModel);

        public Type ProviderClrType
            => typeof(TProvider);
    }

    public class ConverterMappingHints {
    }

    public class ValueConverter {
    }
}
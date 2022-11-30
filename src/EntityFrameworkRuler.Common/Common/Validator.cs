using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Common;

/// <summary> A simple fluent validator. </summary>
public class Validator<T> : IValidator where T : class {
    private readonly List<PropertyValidatorBase> propertyValidators = new();

    /// <summary> Create a validation rule for the given property. </summary>
    public PropertyValidator<TProperty> For<TProperty>(Expression<Func<T, TProperty>> propertyPath) {
        if (propertyPath is null) throw new ArgumentNullException(nameof(propertyPath));

        if (!TryGetPropertyMetadata(propertyPath.Body, out var path1))
            throw new ArgumentException("Invalid property path expression", nameof(propertyPath));

        var propertyValidator = new Validator<T>.PropertyValidator<TProperty>(this, propertyPath, path1);
        propertyValidators.Add(propertyValidator);
        return propertyValidator;
    }
    IEnumerable<EvaluationFailure> IValidator.Evaluate(object instance) => Evaluate((T)instance);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IEnumerable<EvaluationFailure> Evaluate(T instance) {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        foreach (var pv in propertyValidators) {
            foreach (var error in pv.Evaluate(instance)) {
                yield return error;
            }
        }
    }

    private static bool TryGetPropertyMetadata(Expression expression, out string prop) {
        var path = GetPropertyNameFast(expression);
        if (path is null) {
            prop = null;
            return false;
        }

        prop = path;
        return path.HasNonWhiteSpace();
    }

    private static string GetPropertyNameFast(Expression expression) {
        if (expression is LambdaExpression l) expression = l.Body;
        if (expression is not MemberExpression memberExpression)
            throw new ArgumentException("MemberExpression is expected", nameof(expression));

        var member = memberExpression.Member;
        Debug.Assert(member.MemberType.In(MemberTypes.Property, MemberTypes.Field));
        return member.Name;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public sealed class PropertyValidator<TProperty> : PropertyValidatorBase {
        private readonly Validator<T> owner;

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public PropertyValidator(Validator<T> owner, Expression<Func<T, TProperty>> expression, string property) {
            this.owner = owner;
            Expression = expression;
            Property = property;
        }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public Expression<Func<T, TProperty>> Expression { get; }

        private readonly List<(Func<T, TProperty, bool>, string)> evaluators = new();
        private Func<T, TProperty> compiled;

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public PropertyValidator<TProperty> Assert(Func<TProperty, bool> evaluator, string message = null) {
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            evaluators.Add(((_, property) => evaluator(property), message));
            return this;
        }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public PropertyValidator<TProperty> Assert(Func<T, TProperty, bool> evaluator, string message = null) {
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            evaluators.Add((evaluator, message));
            return this;
        }

        /// <summary> Create a validation rule for the given property. </summary>
        public PropertyValidator<TP> For<TP>(Expression<Func<T, TP>> propertyPath) => owner.For(propertyPath);

        internal override IEnumerable<EvaluationFailure> Evaluate(T instance) {
            compiled ??= Expression.Compile();
            TProperty value;
            string readError;
            try {
                value = compiled(instance);
                readError = null;
            } catch (Exception ex) {
                readError = $"{Property} read error: {ex.Message}";
                value = default;
            }

            if (readError != null) {
                yield return new EvaluationFailure(instance, Property, value, readError);
                yield break;
            }


            foreach (var (evaluator, message) in evaluators) {
                string msg;
                try {
                    if (evaluator(instance, value)) continue;
                    // failed!
                    if (message.IsNullOrWhiteSpace()) {
                        msg = $"{Property} value '{value}' is invalid";
                    } else {
                        msg = $"{Property} error: {message}";
                    }
                } catch (Exception ex) {
                    msg = $"{Property} error: {ex.Message}";
                }

                yield return new EvaluationFailure(instance, Property, value, msg);
                break; // break on first
            }
        }

        /// <summary> Implicitly convert property validator to validator </summary>
        public static implicit operator Validator<T>(PropertyValidator<TProperty> pv) => pv.owner;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public abstract class PropertyValidatorBase {
        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public string Property { get; protected set; }

        internal abstract IEnumerable<EvaluationFailure> Evaluate(T instance);
    }

}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public interface IValidator {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    IEnumerable<EvaluationFailure> Evaluate(object instance);
}

/// <summary> Validator error. </summary>
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public sealed class EvaluationFailure {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public object Instance { get; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string Property { get; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public object Value { get; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string Message { get; }

    internal EvaluationFailure(object instance, string property, object value, string message) {
        Instance = instance;
        Property = property;
        Value = value;
        Message = message;
    }
}
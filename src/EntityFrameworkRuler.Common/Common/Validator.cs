using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable ClassCanBeSealed.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Common;

/// <summary> A simple fluent validator. </summary>
public class Validator<T> : IValidator where T : class {
    private readonly List<PropertyValidatorBase> propertyValidators = new();

    /// <summary> Create a validation rule for the given property. </summary>
    public PropertyValidator<TProperty> For<TProperty>(Expression<Func<T, TProperty>> propertyPath, Func<T, string> label = null) {
        if (propertyPath is null) throw new ArgumentNullException(nameof(propertyPath));

        if (!TryGetPropertyMetadata(propertyPath.Body, out var path1))
            throw new ArgumentException("Invalid property path expression", nameof(propertyPath));

        var propertyValidator = new Validator<T>.PropertyValidator<TProperty>(this, propertyPath, path1, label);
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
        if (expression == null) throw new ArgumentNullException(nameof(expression));
        if (expression is LambdaExpression l) expression = l.Body;
        if (expression is not MemberExpression memberExpression) {
            // throw new ArgumentException("MemberExpression is expected", nameof(expression));
            return expression.ToString();
        }

        var member = memberExpression.Member;
        Debug.Assert(member.MemberType.In(MemberTypes.Property, MemberTypes.Field));
        return member.Name;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public sealed class PropertyValidator<TProperty> : PropertyValidatorBase {
        private readonly Validator<T> owner;

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public PropertyValidator(Validator<T> owner, Expression<Func<T, TProperty>> expression, string property, Func<T, string> label = null) {
            this.owner = owner;
            Expression = expression;
            Property = property;
            LabelGetter = label;
        }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public Expression<Func<T, TProperty>> Expression { get; }

        private readonly List<Func<T, TProperty, EvaluatorResponse>> evaluators = new();
        private Func<T, TProperty> compiled;

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public PropertyValidator<TProperty> Assert(Func<TProperty, bool> evaluator, string message = null) {
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            evaluators.Add((_, property) => new(evaluator(property), message));
            return this;
        }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public PropertyValidator<TProperty> Assert(Func<TProperty, EvaluatorResponse> evaluator) {
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            evaluators.Add((_, property) => evaluator(property));
            return this;
        }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public PropertyValidator<TProperty> Assert(Func<T, TProperty, bool> evaluator, string message = null) {
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            evaluators.Add((t, property) => new(evaluator(t, property), message));
            return this;
        }

        /// <summary> Create a validation rule for the given property. </summary>
        public PropertyValidator<TP> For<TP>(Expression<Func<T, TP>> propertyPath, Func<T, string> label = null) => owner.For(propertyPath, label);

        internal override IEnumerable<EvaluationFailure> Evaluate(T instance) {
            compiled ??= Expression.Compile();
            TProperty value;
            string readError;
            try {
                value = compiled(instance);
                readError = null;
            } catch (Exception ex) {
                readError = $"{GetPropertyLabel(instance)} read error: {ex.Message}";
                value = default;
            }

            if (readError != null) {
                yield return new EvaluationFailure(instance, GetPropertyLabel(instance), value, readError);
                yield break;
            }


            foreach (var evaluator in evaluators) {
                string msg;
                try {
                    var evaluatorResponse = evaluator(instance, value);
                    if (evaluatorResponse.Success) continue;
                    // failed!
                    if (evaluatorResponse.Message.IsNullOrWhiteSpace()) {
                        msg = $"{GetPropertyLabel(instance)} value '{value}' is invalid";
                    } else {
                        msg = $"{GetPropertyLabel(instance)} error: {evaluatorResponse.Message}";
                    }
                } catch (Exception ex) {
                    msg = $"{GetPropertyLabel(instance)} error: {ex.Message}";
                }

                yield return new EvaluationFailure(instance, GetPropertyLabel(instance), value, msg);
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

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public Func<T, string> LabelGetter { get; protected set; }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public string GetPropertyLabel(T instance) => instance is not null ? LabelGetter?.Invoke(instance) ?? Property : Property;

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

/// <summary>
/// Represents the response from an evaluator in the validation process.
/// </summary>
/// <param name="Success">A boolean indicating whether the evaluation was successful.</param>
/// <param name="Message">A string containing any relevant message from the evaluation. This could be an error message or additional information about the evaluation.</param>
public record struct EvaluatorResponse(bool Success, string Message) {
    /// <summary>
    /// Creates a successful evaluation response with no diagnostic message.
    /// </summary>
    /// <returns>An <see cref="EvaluatorResponse"/> flagged as successful.</returns>
    public static EvaluatorResponse SuccessResponse() => new(true, null);

    /// <summary>
    /// Creates a failed evaluation response with the provided diagnostic message.
    /// </summary>
    /// <param name="message">Details about why the evaluation failed.</param>
    /// <returns>An <see cref="EvaluatorResponse"/> flagged as failed.</returns>
    public static EvaluatorResponse FailResponse(string message) => new(false, message);
}

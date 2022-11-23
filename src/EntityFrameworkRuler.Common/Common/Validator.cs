using System.Linq.Expressions;
using System.Reflection;

namespace EntityFrameworkRuler.Editor.Models;

public class Validator<T> where T : class {
    private readonly List<PropertyValidatorBase> propertyValidators = new();

    /// <summary> Gets the end value for a related object by using the specified property metadata. </summary>
    public PropertyValidator<TProperty> For<TProperty>(Expression<Func<T, TProperty>> propertyPath) {
        if (propertyPath is null) throw new ArgumentNullException(nameof(propertyPath));

        if (!TryGetPropertyMetadata(propertyPath.Body, out var path1))
            throw new ArgumentException("Invalid property path expression", nameof(propertyPath));

        var propertyValidator = new Validator<T>.PropertyValidator<TProperty>(propertyPath, path1);
        propertyValidators.Add(propertyValidator);
        return propertyValidator;
    }

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
        Debug.Assert(member.MemberType == MemberTypes.Property);
        return member.Name;
    }

    public sealed class PropertyValidator<TProperty> : PropertyValidatorBase {
        public PropertyValidator(Expression<Func<T, TProperty>> expression, string property) {
            Expression = expression;
            Property = property;
        }

        public Expression<Func<T, TProperty>> Expression { get; }

        private readonly List<(Func<T, TProperty, bool>, string)> evaluators = new();
        private Func<T, TProperty> compiled;

        public PropertyValidator<TProperty> Must(Func<TProperty, bool> evaluator, string message = null) {
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            evaluators.Add(((_, property) => evaluator(property), message));
            return this;
        }

        public PropertyValidator<TProperty> Must(Func<T, TProperty, bool> evaluator, string message = null) {
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            evaluators.Add((evaluator, message));
            return this;
        }

        internal override IEnumerable<EvaluationFailure> Evaluate(T instance) {
            compiled ??= Expression.Compile();
            var value = compiled(instance);

            foreach (var (evaluator, message) in evaluators) {
                var msg = message;
                try {
                    if (evaluator(instance, value)) continue;
                    // failed!
                    if (msg.IsNullOrWhiteSpace()) {
                        msg = $"{Property} value {value} is invalid";
                    }
                } catch (Exception ex) {
                    msg = ex.Message;
                }

                yield return new EvaluationFailure(instance, Property, value, msg);
            }
        }
    }

    public abstract class PropertyValidatorBase {
        public string Property { get; protected set; }
        internal abstract IEnumerable<EvaluationFailure> Evaluate(T instance);
    }
}

public sealed class EvaluationFailure {
    public object Instance { get; }
    public string Property { get; }
    public object Value { get; }
    public string Msg { get; }

    internal EvaluationFailure(object instance, string property, object value, string msg) {
        Instance = instance;
        Property = property;
        Value = value;
        Msg = msg;
    }
}
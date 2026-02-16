using System.Reflection;
using EntityFrameworkRuler.Common.Annotations;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using EntityFrameworkRuler.Extension;

namespace EntityFrameworkRuler.Design.Services;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class RuledAnnotationCodeGenerator : AnnotationCodeGenerator {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuledAnnotationCodeGenerator(AnnotationCodeGeneratorDependencies dependencies) : base(dependencies) {
    }


    /// <inheritdoc />
    protected override MethodCallCodeFragment GenerateFluentApi(IEntityType entityType, IAnnotation annotation) {
        switch (annotation.Name) {
            case RulerAnnotations.Abstract: {
                return null;
            }
            case RulerAnnotations.DiscriminatorConfig: {
                var s = annotation.Value as string;
                if (!s.HasNonWhiteSpace()) return null;
                // format the code so that it compile when placed in the entity config segment.
                s = s!.Trim(new char[] { '\n', '\r', ' ', '\t', ';' });
                if (s.StartsWith("entity.")) s = s["entity.".Length..];
                if (s.EndsWith(";")) s = s[..^1];
                if (s.EndsWith("(true)")) s = s[..^"(true)".Length];
                if (s.EndsWith("()")) s = s[..^2];
#pragma warning disable CS0618
                var m = new LastMethodCallCodeFragment(s);
#pragma warning restore CS0618
                return m;
            }
            default:
                return base.GenerateFluentApi(entityType, annotation);
        }
    }

    /// <inheritdoc />
    protected override MethodCallCodeFragment GenerateFluentApi(IProperty property, IAnnotation annotation) {
        switch (annotation.Name) {
            case RulerAnnotations.ForceColumnName: {
                var columnName = annotation.Value as string;
                if (!columnName.HasNonWhiteSpace()) return null;
#pragma warning disable CS0618
                return new MethodCallCodeFragment("HasColumnName", columnName);
#pragma warning restore CS0618
            }
            default:
                return base.GenerateFluentApi(property, annotation);
        }
    }

    /// <inheritdoc />
    public override IEnumerable<IAnnotation> FilterIgnoredAnnotations(IEnumerable<IAnnotation> annotations) {
        foreach (var annotation in base.FilterIgnoredAnnotations(annotations)) {
            if (annotation.Name == RulerAnnotations.Abstract) continue;
            yield return annotation;
        }
    }

    /// <inheritdoc />
    public override IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(IEntityType entityType,
        IDictionary<string, IAnnotation> annotations) {
        if (annotations.ContainsKey(RulerAnnotations.DiscriminatorConfig)) {
        }

        var list = base.GenerateFluentApiCalls(entityType, annotations);
        var i = list.IndexOf(o => o is LastMethodCallCodeFragment);
        if (i < 0 || i >= (list.Count - 1)) return list;

        // move item to the end
        var l = list as IList<MethodCallCodeFragment> ?? new List<MethodCallCodeFragment>(list);
        var item = l[i];
        l.RemoveAt(i);
        l.Add(item);
        list = (IReadOnlyList<MethodCallCodeFragment>)l;
        return list;
    }

    private sealed class LastMethodCallCodeFragment : MethodCallCodeFragment {
        public LastMethodCallCodeFragment(MethodInfo methodInfo, params object[] arguments) : base(methodInfo, arguments) { }

        [Obsolete("Obsolete according to base constructor")]
        public LastMethodCallCodeFragment(string method, params object[] arguments) : base(method, arguments) { }

        [Obsolete("Obsolete according to base constructor")]
        public LastMethodCallCodeFragment(string method, object[] arguments, MethodCallCodeFragment chainedCall) : base(method, arguments,
            chainedCall) {
        }
    }
}

// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Common.Annotations;

/// <summary> Ruler annotations </summary>
public static class RulerAnnotations {
    /// <summary>     The prefix used for all Ruler annotations. </summary>
    public const string Prefix = "Ruler:";

    /// <summary> Type is Abstract. </summary>
    public const string Abstract = Prefix + nameof(Abstract);

    /// <summary> Code for discriminator configuration. </summary>
    public const string DiscriminatorConfig = Prefix + nameof(DiscriminatorConfig);

    /// <summary> ParameterOrder </summary>
    public const string ParameterOrder = Prefix + nameof(ParameterOrder);

    /// <summary> Nullable </summary>
    public const string Nullable = Prefix + nameof(Nullable);

    /// <summary> Ordinal </summary>
    public const string Ordinal = Prefix + nameof(Ordinal);

    /// <summary> Function </summary>
    public const string Function = Prefix + nameof(Function);

    /// <summary>The database generates a value when a row is inserted or updated.</summary>
    public const string ComputedGenerationPattern = Prefix + "ComputedGenerationPattern";

    /// <summary>The database generates a value when a row is inserted.</summary>
    public const string IdentityGenerationPattern = Prefix + "IdentityGenerationPattern";
}
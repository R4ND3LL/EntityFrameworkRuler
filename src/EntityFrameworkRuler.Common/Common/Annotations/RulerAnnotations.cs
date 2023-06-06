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

    /// <summary> List of Database Functions </summary>
    public const string Functions = Prefix + nameof(Functions);

    /// <summary> ModelBuilderExtension </summary>
    public const string ModelBuilderExtension = Prefix + nameof(ModelBuilderExtension);

    /// <summary> ModelExtension </summary>
    public const string ModelExtension = Prefix + nameof(ModelExtension);

    /// <summary> ParameterOrder </summary>
    public const string ParameterOrder = Prefix + nameof(ParameterOrder);
}
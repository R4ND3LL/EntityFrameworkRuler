using EntityFrameworkRuler.Rules.NavigationNaming;

// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Rules;

/// <summary> Root node of the rule model </summary>
public interface IRuleModelRoot {
    /// <summary> Get rule model kind </summary>
    RuleModelKind Kind { get; }

    /// <summary> Get class rules </summary>
    IEnumerable<IClassRule> GetClasses();
}

/// <summary> Rule for a class/table </summary>
public interface IClassRule {
    /// <summary> Get old name </summary>
    string GetOldName();

    /// <summary> Get new name </summary>
    string GetNewName();

    /// <summary> Get property rules </summary>
    IEnumerable<IPropertyRule> GetProperties();
}

/// <summary> Rule for a property/column </summary>
public interface IPropertyRule {
    /// <summary> Get name(s) to look for when making changes.  Assume Roslyn stage, after EF model generation. </summary>
    IEnumerable<string> GetCurrentNameOptions();

    /// <summary> Get new name in event of name change.  Can return null if not changing.  Assume Roslyn stage, after EF model generation. </summary>
    string GetNewName();

    /// <summary> Get new type in event of type change.  Can return null if not changing.  Assume Roslyn stage, after EF model generation. </summary>
    string GetNewTypeName();

    /// <summary> Get extra metadata about this navigation, if in fact it is a navigation. </summary>
    NavigationMetadata GetNavigationMetadata();
}

/// <summary> Extra information about the navigation property </summary>
public struct NavigationMetadata {
    internal NavigationMetadata(string fkName, string toEntity, bool isPrincipal, Multiplicity multiplicity) {
        FkName = fkName;
        ToEntity = toEntity;
        IsPrincipal = isPrincipal;
        Multiplicity = multiplicity;
    }

    /// <summary> The foreign key name for this relationship (if any) </summary>
    public string FkName { get; set; }

    /// <summary> The name of the inverse navigation entity </summary>
    public string ToEntity { get; }

    /// <summary> True if this is the principal end of the navigation.  False if this is the dependent end. </summary>
    public bool IsPrincipal { get; }

    /// <summary> True if this is the dependent end of the navigation.  False if this is the principal end. </summary>
    public bool IsDependent => !IsPrincipal;

    /// <summary> The multiplicity of this end of the relationship. Valid values include "1", "0..1", "*" </summary>
    public Multiplicity Multiplicity { get; set; }
}
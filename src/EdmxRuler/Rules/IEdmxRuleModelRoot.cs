using System.Collections.Generic;
using EdmxRuler.Generator.EdmxModel;

// ReSharper disable MemberCanBePrivate.Global

namespace EdmxRuler.Rules;

public interface IEdmxRuleModelRoot {
    EdmxRuleModelKind Kind { get; }
    IEnumerable<IEdmxRuleClassModel> GetClasses();
}

public interface IEdmxRuleClassModel {
    string GetOldName();
    string GetNewName();
    IEnumerable<IEdmxRulePropertyModel> GetProperties();
}

public interface IEdmxRulePropertyModel {
    /// <summary> Get name(s) to look for when making changes.  Assume Roslyn stage, after EF model generation. </summary>
    IEnumerable<string> GetCurrentNameOptions();

    /// <summary> Get new name in event of name change.  Can return null if not changing.  Assume Roslyn stage, after EF model generation. </summary>
    string GetNewName();

    /// <summary> Get new type in event of type change.  Can return null if not changing.  Assume Roslyn stage, after EF model generation. </summary>
    string GetNewTypeName();

    /// <summary> Get extra metadata about this navigation, if in fact it is a navigation. </summary>
    NavigationMetadata GetNavigationMetadata();
}

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
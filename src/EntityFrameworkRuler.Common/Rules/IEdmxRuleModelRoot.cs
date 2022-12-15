// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Rules;

/// <summary> Root node of the rule model </summary>
public interface IRuleModelRoot : IRuleItem {
    /// <summary> Get rule model kind </summary>
    RuleModelKind Kind { get; }

    /// <summary> Get schema rules </summary>
    IEnumerable<ISchemaRule> GetSchemas();

    /// <summary> Get class rules </summary>
    IEnumerable<IEntityRule> GetClasses();

    /// <summary> Get the file path that this file was loaded from </summary>
    string GetFilePath();
}

/// <summary> Rule for a schema </summary>
public interface ISchemaRule : IRuleItem {
    /// <summary> Get class rules </summary>
    IEnumerable<IEntityRule> GetClasses();
}

/// <summary> Rule for an entity/table </summary>
public interface IEntityRule : IRuleItem {
    /// <summary> Get property rules </summary>
    IEnumerable<IPropertyRule> GetProperties();
}

/// <summary> Rule for a property or navigation </summary>
public interface IPropertyRule : IRuleItem {
    /// <summary> Get name(s) to look for when making changes.  Assume Roslyn stage, after EF model generation. </summary>
    IEnumerable<string> GetCurrentNameOptions();

    /// <summary> Get new type in event of type change.  Can return null if not changing.  Assume Roslyn stage, after EF model generation. </summary>
    string GetNewTypeName();

    /// <summary> Get extra metadata about this navigation, if in fact it is a navigation. </summary>
    NavigationMetadata GetNavigationMetadata();
}

/// <summary> Base interface for rule model items </summary>
public interface IRuleItem {
    /// <summary> Get the name that we expect EF will generate for this item. </summary>
    string GetExpectedEntityFrameworkName();

    /// <summary> Gets the new name to give this element. </summary>
    string GetNewName();

    /// <summary> Gets the conceptual name of the model. That is, the name that this element should have after the reverse engineer. </summary>
    string GetFinalName();

    /// <summary> Sets the conceptual name of the model. That is, the name that this element should have after the reverse engineer. </summary>
    void SetFinalName(string value);

    /// <summary> If true, omit this item and all containing elements during the scaffolding process. Default is false. </summary>
    bool NotMapped { get; }
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
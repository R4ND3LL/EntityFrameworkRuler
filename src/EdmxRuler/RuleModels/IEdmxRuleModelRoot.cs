using System.Collections.Generic;
using EdmxRuler.Generator.EdmxModel;

namespace EdmxRuler.RuleModels;

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
    /// <summary> Get name(s) to look for when making changes </summary>
    IEnumerable<string> GetCurrentNameOptions();

    /// <summary> Get new name in event of name change.  Can return null if not changing name. </summary>
    string GetNewName();

    string GetNewTypeName();
    NavigationMetadata GetNavigationMetadata();
}

public struct NavigationMetadata {
    public NavigationMetadata(string fkName, Multiplicity multiplicity) {
        FkName = fkName;
        Multiplicity = multiplicity;
    }

    /// <summary> The foreign key name for this relationship (if any) </summary>
    public string FkName { get; set; }

    /// <summary> The multiplicity of this end of the relationship. Valid values include "1", "0..1", "*" </summary>
    public Multiplicity Multiplicity { get; set; }
}
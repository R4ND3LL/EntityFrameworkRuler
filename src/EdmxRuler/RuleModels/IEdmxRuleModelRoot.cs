using System.Collections.Generic;

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
}
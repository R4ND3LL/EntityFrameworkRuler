using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <summary> Base class for rule model items </summary>
[SuppressMessage("ReSharper", "VirtualMemberNeverOverridden.Global")]
[DataContract]
public abstract class RuleBase : IRuleItem {
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    // ReSharper disable once ConvertToConstant.Global
    internal static bool Observable = false;
    /// <summary> Get the name that we expect EF will generate for this item. </summary>
    protected abstract string GetExpectedEntityFrameworkName();
    /// <summary> Gets the new name to give this element. </summary>
    protected abstract string GetNewName();

    /// <summary> Sets the conceptual name of the model. That is, the name that this element should have in the final reverse engineered model. </summary>
    protected abstract void SetFinalName(string value);
    /// <summary> If true, omit this column during the scaffolding process. </summary>
    public abstract bool NotMapped { get; set; }
    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    internal bool Mapped => !NotMapped;
    string IRuleItem.GetExpectedEntityFrameworkName() => GetExpectedEntityFrameworkName();
    string IRuleItem.GetNewName() => GetNewName();
    string IRuleItem.GetFinalName() => GetNewName().NullIfWhitespace() ?? GetExpectedEntityFrameworkName();
    void IRuleItem.SetFinalName(string value) => SetFinalName(value);
}
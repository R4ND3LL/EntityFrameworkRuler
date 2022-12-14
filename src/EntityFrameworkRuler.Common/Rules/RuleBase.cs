using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <summary> Base class for rule model items </summary>
[SuppressMessage("ReSharper", "VirtualMemberNeverOverridden.Global")]
[DataContract]
public abstract class RuleBase : IRuleItem {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static bool Observable = false;

    /// <summary> Get the name that we expect EF will generate for this item. </summary>
    protected abstract string GetExpectedEntityFrameworkName();

    /// <summary> Gets the new name to give this element. </summary>
    protected abstract string GetNewName();

    /// <summary> Sets the conceptual name of the model. That is, the name that this element should have in the final reverse engineered model. </summary>
    protected abstract void SetFinalName(string value);

    /// <summary> If true, omit this item and all containing elements during the scaffolding process. Default is false. </summary>
    protected abstract bool GetNotMapped();

    /// <summary> If true, omit this column during the scaffolding process. </summary>
    bool IRuleItem.NotMapped => GetNotMapped();

    /// <summary> If false, omit this column during the scaffolding process. </summary>
    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public bool Mapped => !GetNotMapped();

    string IRuleItem.GetExpectedEntityFrameworkName() => GetExpectedEntityFrameworkName();
    string IRuleItem.GetNewName() => GetNewName();
    string IRuleItem.GetFinalName() => GetNewName().NullIfWhitespace() ?? GetExpectedEntityFrameworkName();
    void IRuleItem.SetFinalName(string value) => SetFinalName(value);
}
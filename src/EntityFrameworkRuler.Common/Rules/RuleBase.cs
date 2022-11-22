using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <summary> Base class for rule model items </summary>
[SuppressMessage("ReSharper", "VirtualMemberNeverOverridden.Global")]
[DataContract]
public abstract class RuleBase : IRuleItem, INotifyPropertyChanged {
    ///// <summary> Gets the final conceptual name of the model. That is, the name that this element should have in the final reverse engineered model. </summary>
    //protected abstract string GetFinalName();
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
    string IRuleItem.GetFinalName() => GetNewName().NullIfWhitespace() ?? GetExpectedEntityFrameworkName();
    string IRuleItem.GetNewName() => GetNewName();
    void IRuleItem.SetFinalName(string value) => SetFinalName(value);

    private event PropertyChangedEventHandler PropertyChanged;

    event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged {
        add => PropertyChanged += value;
        remove => PropertyChanged -= value;
    }

    /// <summary> raise property changed event </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary> raise property changed event for all properties </summary>
    internal virtual void OnPropertiesChanged() {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    /// <summary> Set field. Raise changed event. </summary>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
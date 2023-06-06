using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <summary> Table rule </summary>
[DebuggerDisplay("Function {NewName} (from DB function {Name})")]
[DataContract]
public sealed class FunctionRule : RuleBase, IFunctionRule {
    /// <summary> Creates a function rule </summary>
    public FunctionRule() {
        properties = Observable ? new ObservableCollection<FunctionParameterRule>() : new List<FunctionParameterRule>();
    }

    /// <summary> The raw database name of the function.  Used to locate the property during scaffolding phase.  Required. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 1)]
    [DisplayName("DB Name"), Category("Mapping"), Description("The storage name of the function."), Required]
    public string Name { get; set; }


    /// <summary> The new name to give the entity (if any). </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
    [DisplayName("New Name"), Category("Mapping"), Description("The new name to give the entity.")]
    public string NewName { get; set; }

    /// <summary> If true, omit this function during the scaffolding process. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 7)]
    [DisplayName("Not Mapped"), Category("Mapping"), Description("If true, omit this function during the scaffolding process.")]
    public bool NotMapped { get; set; }


    private IList<FunctionParameterRule> properties;

    /// <summary> The primitive property rules to apply to this entity. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 8)]
    [DisplayName("Parameters"), Category("Parameters|Parameters"), Description("The primitive property rules to apply to this entity.")]
    public IList<FunctionParameterRule> Parameters {
        get => properties;
        set => UpdateCollection(ref properties, value);
    }
 
    /// <summary> The return value type name.  Used to customize the name of a complex type. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 9)]
    [DisplayName("Result Type Name"), Category("Mapping"), Description("The return value type name.  Used to customize the name of a complex type.")]
    public string ResultTypeName { get; set; }


    /// <inheritdoc />
    protected override string GetDbName() => Name;

    /// <inheritdoc />
    protected override string GetNewName() => NewName.NullIfEmpty();

    /// <inheritdoc /> 
    protected override string GetExpectedEntityFrameworkName() => null;

    /// <inheritdoc />
    protected override void SetFinalName(string value) { NewName = value; }

    /// <inheritdoc />
    protected override bool GetNotMapped() => NotMapped;

    IEnumerable<IFunctionParameterRule> IFunctionRule.GetParameters() {
        if (!Parameters.IsNullOrEmpty())
            foreach (var rule in Parameters)
                yield return rule;
    }
}
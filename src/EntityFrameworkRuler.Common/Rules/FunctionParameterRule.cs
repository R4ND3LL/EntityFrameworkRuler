using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <summary> Parameter rule </summary>
[DataContract]
public sealed class FunctionParameterRule : RuleBase, IFunctionParameterRule {
    /// <inheritdoc />
    public FunctionParameterRule() { 
    }

    /// <summary> The raw database name of the column.  Used to locate the property during scaffolding phase.  Required. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 1)]
    [DisplayName("DB Name"), Category("Mapping"),
     Description("The raw database name of the parameter.  Used to locate the property during scaffolding phase.  Required."), Required]
    public string Name { get; set; }
 
    /// <summary> The new name to give the property. Optional. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
    [DisplayName("New Name"), Category("Mapping"), Description("The new name to give the property. Optional.")]
    public string NewName { get; set; }

    /// <summary> The type of the property. Optional. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 4)]
    [DisplayName("Type Name"), Category("Mapping"), Description("The type of the property. Optional.")]
    public string TypeName { get; set; }
 
    /// <summary> If true, omit this parameter during the scaffolding process. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 6)]
    [DisplayName("Not Mapped"), Category("Mapping"), Description("If true, omit this parameter during the scaffolding process.")]
    public bool NotMapped { get; set; }
  
    /// <inheritdoc />
    protected override string GetDbName() => Name;

    /// <inheritdoc />
    protected override string GetNewName() => NewName.NullIfEmpty();

    /// <inheritdoc />
    protected override string GetExpectedEntityFrameworkName() => null;

    /// <inheritdoc />
    protected override void SetFinalName(string value) {
        NewName = value;
    }

    /// <inheritdoc />
    protected override bool GetNotMapped() => NotMapped;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public override bool ShouldMap() => base.ShouldMap()  ;

    string IFunctionParameterRule.GetNewTypeName() => TypeName;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public override string ToString() {
        if (NewName != null) return $"Param {Name} to Prop {NewName}";
        return $"Param {Name}";
    }
}
 
using Microsoft.EntityFrameworkCore.Scaffolding;
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace EntityFrameworkRuler.Design.Scaffolding;

/// <summary>
///     Represents the options to use while generating code for a model.
/// </summary>
public class ModelCodeGenerationOptionsEx : ModelCodeGenerationOptions {
    /// <summary>     Gets or sets a value indicating whether to split entity type configurations into separate files. </summary> 
    public virtual bool SplitEntityTypeConfigurations { get; set; }
}
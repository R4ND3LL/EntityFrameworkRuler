using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace EntityFrameworkRuler.Generator.EdmxModel;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
[SuppressMessage("ReSharper", "MemberCanBeInternal", Justification = "Exported type")]
public sealed class EdmxParsed {
    internal EdmxParsed(string filePath) {
        FilePath = filePath;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string ContextName { get; set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string FilePath { get; }

    // /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    // public Dictionary<string, AssociationBase> AssociationsByName { get; internal set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public Dictionary<string, EnumType> EnumsByName { get; internal set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public Dictionary<string, EnumType> EnumsByConceptualSchemaName { get; internal set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public Dictionary<string, EnumType> EnumsByExternalTypeName { get; internal set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public ObservableCollection<Schema> Schemas { get; } = new();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public ObservableCollection<EntityType> Entities { get; } = new();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public ObservableCollection<NavigationProperty> NavProps { get; } = new();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public ObservableCollection<EntityProperty> Props { get; } = new();
}
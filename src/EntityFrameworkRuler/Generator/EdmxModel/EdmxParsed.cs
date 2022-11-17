using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace EntityFrameworkRuler.Generator.EdmxModel;

[SuppressMessage("ReSharper", "MemberCanBeInternal", Justification = "Exported type")]
public sealed class EdmxParsed {
    internal EdmxParsed(string filePath) {
        FilePath = filePath;
    }

    public string ContextName { get; set; }
    public string FilePath { get; }
    public Dictionary<string, AssociationBase> AssociationsByName { get; internal set; }
    public Dictionary<string, EnumType> EnumsByName { get; internal set; }
    public Dictionary<string, EnumType> EnumsByConceptualSchemaName { get; internal set; }
    public Dictionary<string, EnumType> EnumsByExternalTypeName { get; internal set; }

    public ObservableCollection<Schema> Schemas { get; } = new();

    public ObservableCollection<EntityType> Entities { get; } = new();

    public ObservableCollection<NavigationProperty> NavProps { get; } = new();

    public ObservableCollection<EntityProperty> Props { get; } = new();

}
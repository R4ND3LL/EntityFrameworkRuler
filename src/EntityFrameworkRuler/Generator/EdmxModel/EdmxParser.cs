#define DEBUGPARSER2
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml;

namespace EntityFrameworkRuler.Generator.EdmxModel;

public sealed class EdmxParser : NotifyPropertyChanged {
    public static EdmxParsed Parse(string filePath) {
        return new EdmxParser(filePath).State;
    }

    private EdmxParser(string fileInstancePath) {
        State = new(fileInstancePath);
        if (!File.Exists(fileInstancePath)) throw new InvalidDataException($"Could not find file {fileInstancePath}");

        var edmxModel = EdmxSerializer.Deserialize(File.ReadAllText(fileInstancePath));
        var edmx = edmxModel.Runtime;

        var schema = new Schema(edmx.ConceptualModels.Schema, edmx.StorageModels.Schema);
        Schemas.Add(schema);

        State.ContextName = schema.ConceptualSchema?.EntityContainer?.Name?.Trim();

        // weave the model data together starting with enums, then on to the conceptual entities
        var enums = edmx.ConceptualModels.Schema.EnumTypes
            .Select(enumItem => new EnumType(enumItem, schema)).ToArray();
        State.EnumsByName = enums.ToDictionary(o => o.Name);
        State.EnumsByConceptualSchemaName = enums.Where(o => o.Schema?.Namespace.HasNonWhiteSpace() == true)
            .ToDictionary(o => o.Schema.Namespace + "." + o.Name);
        State.EnumsByExternalTypeName = enums.Where(o => o.ExternalTypeName.HasNonWhiteSpace())
            .ToDictionary(o => o.ExternalTypeName);

        var conceptualAssociationsByName =
            edmx.ConceptualModels.Schema.Associations
                .Where(o => o.Name?.Length > 0)
                .ToDictionary(o =>
                    o.Name.Split('.').Last() ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        foreach (var conceptualEntityType in edmx.ConceptualModels.Schema.EntityTypes) {
            var entity = new EntityType(conceptualEntityType, schema);
            Entities.Add(entity);
            //if (entity.Name == "CustomerDemographic") Debugger.Break();

            var fullName = entity.FullName;
            var selfName = entity.SelfName;

            entity.EntitySetMapping = edmx.Mappings.Mapping.EntityContainerMapping.EntitySetMappings?
                .FirstOrDefault(o => o.EntityTypeMappings.Any(etm => etm.TypeName.EqualsIgnoreCase(fullName)));

            entity.MappingFragments = entity.EntitySetMapping?.EntityTypeMappings?
                .FirstOrDefault(etm => etm.TypeName.EqualsIgnoreCase(fullName))
                ?.MappingFragments ?? new List<MappingFragment>();

            if (entity.MappingFragments?.Count > 0) {
                entity.StoreEntitySetNames = entity.MappingFragments.Select(o => o.StoreEntitySet).Distinct().ToArray();

                entity.StorageContainer = edmx.StorageModels.Schema.EntityContainers.FirstOrDefault(o =>
                    o.EntitySets.Any(es => entity.StoreEntitySetNames.Contains(es.Name)));
                entity.StorageEntitySet = entity.StorageContainer?.EntitySets
                    .FirstOrDefault(o => entity.StoreEntitySetNames.Contains(o.Name));
                if (entity.StorageEntitySet?.Name != null)
                    entity.StorageEntity =
                        edmx.StorageModels.Schema.EntityTypes.FirstOrDefault(
                            o => o.Name == entity.StorageEntitySet.Name);
            }

            // populate associations that hit this entity
            entity.Associations =
                edmx.ConceptualModels.Schema.Associations.Where(conceptualAssociation =>
                    conceptualAssociation.Ends.Any(conceptualEnd =>
                        conceptualEnd.Type == fullName || conceptualEnd.Type == selfName ||
                        conceptualEnd.Role == entity.Name)).ToList();

            // link properties to storage elements
            foreach (var conceptualProperty in conceptualEntityType.Properties) {
                var property = new EntityProperty(entity, conceptualProperty);
                entity.Properties.Add(property);
                if (entity.MappingFragments?.Count > 0) {
                    property.Mapping = entity.MappingFragments.SelectMany(o => o.ScalarProperties)
                        .Single(o => o.Name == property.Name);
                    if (property.Mapping?.ColumnName != null && entity.StorageEntity?.Properties?.Count > 0)
                        property.StorageProperty =
                            entity.StorageEntity.Properties.Single(o => o.Name == property.Mapping.ColumnName);
                }

                Debug.Assert(property.StorageProperty != null);
                Props.Add(property);

                if (conceptualEntityType.Key?.PropertyRefs?.Count > 0) {
                    var key = conceptualEntityType.Key.PropertyRefs.FirstOrDefault(o => o.Name == property.Name);
                    property.IsConceptualKey = key != null;
                    if (property.IsConceptualKey)
                        entity.ConceptualKey.Add(property);
                }

                if (entity.StorageEntity?.Key?.PropertyRefs?.Count > 0) {
                    var key = entity.StorageEntity.Key.PropertyRefs.FirstOrDefault(o => o.Name == property.Name);
                    property.IsStorageKey = key != null;
                    if (property.IsConceptualKey)
                        entity.StorageKey.Add(property);
                }

                // link up enums:
                var typeNameParts = property.TypeName.SplitNamespaceAndName();
                EnumType enumType;
                if (typeNameParts.namespaceName.HasNonWhiteSpace()) {
                    // namespace itself it not reliable. just use the type name to see if we hit an enum
                    if (EnumsByName.TryGetValue(typeNameParts.name, out enumType)) {
                        property.EnumType = enumType;
                        enumType.Properties.Add(property);
                    }
                }

                // hail mary:
                if (property.EnumType is null && EnumsByExternalTypeName.TryGetValue(property.TypeName, out enumType)) {
                    property.EnumType = enumType;
                    enumType.Properties.Add(property);
                }
            }

            // link navigations to association elements
            foreach (var conceptualProperty in conceptualEntityType.NavigationProperties) {
                var property = new NavigationProperty(entity, conceptualProperty);
                entity.NavigationProperties.Add(property);
                NavProps.Add(property);

                if (conceptualProperty.Relationship.IsNullOrEmpty()) continue;

                var relationship = conceptualProperty.Relationship;
                relationship = relationship.Split('.').Last() ?? string.Empty; // remove namespace
                property.ConceptualAssociation = entity.Associations?
                    .FirstOrDefault(o => o.Name.EqualsIgnoreCase(relationship));
                if (property.ConceptualAssociation == null) {
                    // take a broader look. suspicious end Type in here
                    var match = conceptualAssociationsByName.TryGetValue(relationship, out var v) ? v : null;
                    // var match = edmx.ConceptualModels.Schema.Associations.FirstOrDefault(o =>
                    //     string.Equals(o.Name, relationship, StringComparison.OrdinalIgnoreCase));
                    if (match != null) property.ConceptualAssociation = match;
                }

                Debug.Assert(property.ConceptualAssociation != null);
            }
        }

        var associationSetMappings = edmx.Mappings.Mapping.EntityContainerMapping?.AssociationSetMapping;

        // with all entities loaded, perform association linking
        foreach (var entity in Entities)
        foreach (var navigation in entity.NavigationProperties) {
            if (navigation.Association != null) continue; // wired up from the opposite end!
            var ass = navigation.ConceptualAssociation;
            if (ass == null) continue;

            var endRoles = ass.Ends.Select(end => CreateEndRole(ass, end)).ToArray();
            Debug.Assert(ass.Ends.Count == 0 || endRoles.Length > 0);
            Debug.Assert(endRoles.Length == 2);

            var toEndRole = endRoles.FirstOrDefault(o => o.Role == navigation.ToRoleName);
            var fromEndRole = endRoles.FirstOrDefault(o => o.Role == navigation.FromRoleName);

            if (toEndRole == null || fromEndRole == null) {
#if DEBUGPARSER
                if (Debugger.IsAttached) Debugger.Break(); // invalid association!?
#endif
                continue;
            }

            if (ass.ReferentialConstraint == null) {
                // constraint missing probably because this is a many-to-many relationship with a suppressed junction.
                ResolveDesignAssociation(navigation, ass, endRoles, toEndRole, associationSetMappings);
            } else {
                // normal fk association
                ResolveFkAssociation(navigation, ass, endRoles, toEndRole);
            }
#if DEBUGPARSER
            if (navigation.Association == null && Debugger.IsAttached)
                Debugger.Break(); // invalid association. May be design time only?
#endif
        }
#if DEBUGPARSER
        // ensure associations are properly linked.
        var navsWithNoAssociation = Entities.SelectMany(o => o.NavigationProperties).Where(o => o.Association == null)
            .ToHashSet();
        var associationsLinked = Entities.SelectMany(o => o.Associations).ToHashSet();
        var allAssociations = edmx.ConceptualModels.Schema.Associations.ToHashSet();
        var associationsNotLinked = allAssociations.Where(o => !associationsLinked.Contains(o))
            .OrderBy(o => o.ReferentialConstraint.Principal.Role).ToArray();
        Debug.Assert(navsWithNoAssociation.Count == 0);
        Debug.Assert(associationsNotLinked.Length == 0);
#endif

        State.AssociationsByName = Entities.SelectMany(o => o.NavigationProperties)
            .Where(o => o.Association != null)
            .Select(o => o.Association)
            .GroupBy(o => o.Name)
            .ToDictionary(o => o.Key, o => o.First(), StringComparer.InvariantCultureIgnoreCase);
    }

    private void ResolveDesignAssociation(NavigationProperty navigation, ConceptualAssociation ass,
        EndRole[] endRoles, EndRole toEndRole,
        List<AssociationSetMapping> associationSetMappings) {
        if (associationSetMappings.IsNullOrEmpty()) return;
        var associationSetMapping = associationSetMappings.FirstOrDefault(o =>
            o.Name == navigation.Relationship.SplitNamespaceAndName().name);
        if (associationSetMapping?.EndProperties?.Count != 2) return;
        var endProperty =
            associationSetMapping.EndProperties.FirstOrDefault(o => o.Name == navigation.Name);
        if (endProperty == null) return;
        var inverseEndProperty = associationSetMapping.EndProperties?.FirstOrDefault(o => o != endProperty);
        if (inverseEndProperty == null) return;
        var inverseEntity = toEndRole?.Entity;
        var inverseNavigation =
            inverseEntity?.NavigationProperties?.FirstOrDefault(o => o.Name == inverseEndProperty.Name);
        if (inverseNavigation == null || navigation == inverseNavigation) return;
        // Note, for a many-to-many, there are no dependents in the end entities. Rather, the FKs are all in the junction,
        // which has no conceptual representation.  Therefore, the ScalarProperties in the mapping ends refer to the junction only.
        var a = new DesignAssociation(ass, endRoles, navigation, inverseNavigation);
        if (navigation.Association != a) throw new InvalidConstraintException("Association not wired to navigation");
    }

    private void ResolveFkAssociation(NavigationProperty navigation, ConceptualAssociation ass,
        EndRole[] endRoles, EndRole toEndRole) {
        var constraint = navigation?.ConceptualAssociation?.ReferentialConstraint;
        if (constraint == null) return;
        var principalEndRole = endRoles.FirstOrDefault(o => o.Role == constraint.Principal.Role);
        var dependentEndRole = endRoles.FirstOrDefault(o => o.Role == constraint.Dependent.Role);
        if (principalEndRole?.Entity == null || dependentEndRole?.Entity == null) return;
        var pProps = GetProperties(principalEndRole.Entity,
            constraint.Principal.PropertyRefs.Select(o => o.Name).ToArray());
        var dProps = GetProperties(dependentEndRole.Entity,
            constraint.Dependent.PropertyRefs.Select(o => o.Name).ToArray());
        if (pProps.IsNullOrEmpty() || dProps.IsNullOrEmpty()) return;
        var inverseNavigation =
            toEndRole.Entity.NavigationProperties.SingleOrDefault(o =>
                o.Relationship == navigation.Relationship && o != navigation);
        if (inverseNavigation == null || navigation == inverseNavigation) return;
        var a = new FkAssociation(ass, endRoles, navigation, inverseNavigation,
            new(constraint, pProps, dProps)
        );
        if (navigation.Association != a) throw new InvalidConstraintException("Association not wired to navigation");
    }

    private Schema GetSchema(string schemaNamespace) {
        if (schemaNamespace.IsNullOrEmpty() || schemaNamespace == "Self") return Schemas[0];

        var s = Schemas.FirstOrDefault(o => o.ConceptualSchema.Namespace == schemaNamespace)
                ?? Schemas.FirstOrDefault(o => o.StorageSchema.Namespace == schemaNamespace);
        // ReSharper disable once UseNullPropagation
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (s != null) return s;
        return null;
    }

    private EntityType GetEntity(string fullName) {
        if (fullName.IsNullOrEmpty()) throw new ArgumentNullException();
        var parts = fullName.SplitNamespaceAndName();
        return GetEntity(parts.namespaceName, parts.name);
    }


    private EntityType GetEntity(string schemaNamespace, string entityName) {
        var s = schemaNamespace.HasCharacters() || Schemas.Count == 1 ? GetSchema(schemaNamespace) : null;
        if (s == null) {
            var entList = Entities.Where(o => o.Name == entityName).ToArray();
            if (entList.Length != 1) throw new InvalidDataException("Ambiguous entity name: " + entityName);
        }

        var e = Entities.FirstOrDefault(o => o.Name == entityName);
        return e;
    }

    private EntityProperty[] GetProperties(EntityType entity, string[] propNames) {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        var props = propNames.Select(n => entity.Properties
                .FirstOrDefault(o => o.Name.EqualsIgnoreCase(n)))
            .Where(o => o != null).ToArray();
        return props;
    }


    private EndRole CreateEndRole(ConceptualAssociation association, ConceptualEnd end) {
        if (end?.Type.IsNullOrEmpty() != false) return null;
        var entityType = GetEntity(end?.Type);
        if (entityType == null) return null;
        var e = new EndRole(association, end, entityType);
        return e;
    }

    #region edmx editing

    public void SetNavPropName(string entityname, string propname, string newNavPropName) {
        ReplaceXmlAttributeValueByIndex(FilePath,
            $"descendant::edm:Schema/edm:EntityType[@Name='{entityname}']/edm:NavigationProperty[@Name='{propname}']",
            "Name", newNavPropName);
    }

    private static void ReplaceXmlAttributeValueByIndex(string fullFilePath, string nodeName, string attrName,
        string valueToAdd) {
        var fileInfo = new FileInfo(fullFilePath) { IsReadOnly = false };
        fileInfo.Refresh();

        var xmldoc = new XmlDocument();
        xmldoc.Load(fullFilePath);
        var nsm = new XmlNamespaceManager(xmldoc.NameTable);
        nsm.AddNamespace("edmx", "http://schemas.microsoft.com/ado/2009/11/edmx");
        nsm.AddNamespace("ssdl", "http://schemas.microsoft.com/ado/2009/11/edm/ssdl");
        nsm.AddNamespace("edm", "http://schemas.microsoft.com/ado/2009/11/edm");
        try {
            var tnode = xmldoc.SelectSingleNode(nodeName, nsm);
            var nodes = xmldoc.SelectNodes(nodeName, nsm);
            var node = tnode as XmlElement;
            //node.Attributes[index].Value = valueToAdd;
            node.SetAttribute(attrName, valueToAdd); // Set to new value
        } catch (Exception) {
            //add code to see the error
        } //"descendant::edm:Schema/edm:EntityType"

        xmldoc.Save(fullFilePath);
    }

    #endregion

    #region properties

    private EdmxParsed State { get; }
    private string FilePath => State.FilePath;
    private Dictionary<string, AssociationBase> AssociationsByName => State.AssociationsByName;
    private Dictionary<string, EnumType> EnumsByName => State.EnumsByName;
    private Dictionary<string, EnumType> EnumsByConceptualSchemaName => State.EnumsByConceptualSchemaName;
    private Dictionary<string, EnumType> EnumsByExternalTypeName => State.EnumsByExternalTypeName;
    private ObservableCollection<Schema> Schemas => State.Schemas;
    private ObservableCollection<EntityType> Entities => State.Entities;
    private ObservableCollection<NavigationProperty> NavProps => State.NavProps;
    private ObservableCollection<EntityProperty> Props => State.Props;

    #endregion
}
#define DEBUGPARSER2
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Generator.EdmxModel;

// ReSharper disable RemoveRedundantBraces

namespace EntityFrameworkRuler.Generator.Services;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
public class EdmxParser : NotifyPropertyChanged, IEdmxParser {
    private IMessageLogger logger;

    private static Regex ver2Regex =
        new Regex(@"\<\w+:Edmx\s+Version=""(?<ver>2\.\d)""\s+\w+:edmx=""http:\/\/schemas.microsoft.com\/ado\/2008\/10\/edmx""\s*\>");

    /// <inheritdoc />
    public EdmxParsed Parse(string fileInstancePath, IMessageLogger logger) {
        State = new(fileInstancePath); // FYI, this is not thread safe
        if (!File.Exists(fileInstancePath)) throw new InvalidDataException($"Could not find file {fileInstancePath}");
        this.logger = logger ?? NullMessageLogger.Instance;

        var edmxContent = File.ReadAllText(fileInstancePath);
        var m = ver2Regex.Match(edmxContent);

        if (m.Success) {
            var verStr = m.Groups["ver"]?.Value;
            if (verStr.HasNonWhiteSpace() && double.TryParse(verStr, out var ver) && ver < 3) {
                this.logger.WriteWarning($"EDMX ver {ver} detected.  This version is not fully supported and may not yield accurate model results.");
                TryUpgrade(ref edmxContent);
            }
        }

        var edmxModel = EdmxSerializer.Deserialize(edmxContent);
        var edmx = edmxModel.Runtime;

        var schema = new Schema(edmx.ConceptualModels.Schema, edmx.StorageModels.Schema);
        Schemas.Add(schema);

        State.ContextName = schema.ConceptualSchema?.EntityContainer?.Name?.Trim();

        // weave the model data together starting with enums, then on to the conceptual entities
        foreach (var enumType in edmx.ConceptualModels.Schema.EnumTypes)
            enumType.ExternalTypeName = enumType.ExternalTypeName.ToFriendlyTypeName();

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
                    o.Name.Split('.').Last(), StringComparer.OrdinalIgnoreCase);
        var storageAssociationsByName =
            edmx.StorageModels.Schema.Associations
                .Where(o => o.Name?.Length > 0)
                .ToDictionary(o =>
                    o.Name.Split('.').Last(), StringComparer.OrdinalIgnoreCase);

        var entitiesByTableMapping = new Dictionary<string, List<EntityType>>();

        foreach (var conceptualEntityType in edmx.ConceptualModels.Schema.EntityTypes) {
            var entity = new EntityType(conceptualEntityType, schema);
            Entities.Add(entity);
            //if (entity.Name == "CustomerDemographic") Debugger.Break();

            var fullName = entity.FullName;
            var selfName = entity.SelfName;
            var isOfTypeName = $"IsTypeOf({fullName})";

            entity.EntitySetMapping =
                edmx.Mappings.Mapping.EntityContainerMapping.EntitySetMappings?
                    .FirstOrDefault(o =>
                        o.EntityTypeMappings.Any(etm => etm.TypeName.EqualsIgnoreCase(fullName)))
                ?? edmx.Mappings.Mapping.EntityContainerMapping.EntitySetMappings?
                    .FirstOrDefault(o =>
                        o.EntityTypeMappings.Any(etm => etm.TypeName.EqualsIgnoreCase(isOfTypeName)));

            entity.MappingFragments =
                entity.EntitySetMapping?.EntityTypeMappings?
                    .FirstOrDefault(etm => etm.TypeName.EqualsIgnoreCase(fullName))
                    ?.MappingFragments
                ?? entity.EntitySetMapping?.EntityTypeMappings?
                    .FirstOrDefault(etm => etm.TypeName.EqualsIgnoreCase(isOfTypeName))
                    ?.MappingFragments
                ?? new List<MappingFragment>();

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


                // track entities-table mapping, so that we can easily capture TPH and table splitting behavior
                entitiesByTableMapping.GetOrAddNew(entity.StorageFullName ?? string.Empty, _ => new())
                    .Add(entity);
            }

            // populate associations that hit this entity
            var storageSelfName = $"Self.{entity.StorageName}";
            var storageRole = entity.StorageName;
            entity.Associations =
                edmx.ConceptualModels.Schema.Associations.Where(association =>
                    association.Ends.Any(end =>
                        end.Type == fullName || end.Type == selfName ||
                        end.Role == entity.Name)).ToList();

            if (entity.StorageEntity != null) {
                entity.StorageAssociations =
                    edmx.StorageModels.Schema.Associations.Where(association =>
                        association.Ends.Any(end =>
                            end.Type == storageSelfName ||
                            end.Role == storageRole)).ToList();
                Debug.Assert(entity.Associations.Count == 0 ||
                             entity.StorageEntitySet?.Type == "Views" ||
                             entity.StorageAssociations.Count > 0);
            }

            // link properties to storage elements
            foreach (var conceptualProperty in conceptualEntityType.Properties) {
                StorageProperty storageProperty = null;
                ScalarPropertyMapping mapping = null;
                if (entity.MappingFragments?.Count > 0) {
                    mapping = entity.MappingFragments.SelectMany(o => o.ScalarProperties)
                        .SingleOrDefault(o => o.Name == conceptualProperty.Name);
                    if (mapping?.ColumnName != null && entity.StorageEntity?.Properties?.Count > 0) {
                        storageProperty =
                            entity.StorageEntity.Properties.Single(o => o.Name == mapping.ColumnName);
                    }
                }

                var property = new EntityProperty(entity, conceptualProperty, storageProperty) {
                    Mapping = mapping
                };

                Debug.Assert(storageProperty == null || property.ColumnName != null);

                IdentifyKeys(entity, property);

                // link up enums:
                var typeNameParts = property.ClrTypeName.ToFriendlyTypeName().SplitNamespaceAndName();
                EnumType enumType;
                if (typeNameParts.namespaceName.HasNonWhiteSpace()) {
                    // namespace itself it not reliable. just use the type name to see if we hit an enum
                    if (EnumsByName.TryGetValue(typeNameParts.name, out enumType)) {
                        property.EnumType = enumType;
                        enumType.Properties.Add(property);
                    }
                }

                // hail mary:
                if (property.EnumType is null && EnumsByExternalTypeName.TryGetValue(property.ClrTypeName, out enumType)) {
                    property.EnumType = enumType;
                    enumType.Properties.Add(property);
                }

                entity.Properties.Add(property);
            }

            if (entity.StorageEntity?.Properties?.Count > 0)
                foreach (var storageProperty in entity.StorageEntity.Properties) {
                    if (entity.Properties.Any(o => o.StorageProperty == storageProperty)) continue;
                    var property = new EntityProperty(entity, null, storageProperty);
                    IdentifyKeys(entity, property);

                    if (property.IsStorageKey && entity.Properties.Count > 0) entity.Properties.Insert(0, property);
                    else entity.Properties.Add(property);
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
                    if (match != null) property.ConceptualAssociation = match;
                }

                Debug.Assert(property.ConceptualAssociation != null);

                /* still need to resolve the storage association in order to get the real name
                    /Edmx/Runtime/ConceptualModels/Schema/EntityType/NavigationProperty/@Relationship = Conceptual association name with Namespace. prefix
                    /Edmx/Runtime/ConceptualModels/Schema/Association/@Name = Conceptual association name and contains role and constraint details.

                    There is no named mapping from conceptual to storage association as per the following:

                    /Edmx/Runtime/StorageModels/Schema/EntityContainer/AssociationSet/@Association = DB association name with Self. prefix
                    /Edmx/Runtime/StorageModels/Schema/EntityContainer/AssociationSet/@Name = DB association name
                    /Edmx/Runtime/StorageModels/Schema/Association/@Name = DB association name and details the storage RefConstraint

                    Best to compare constraints to determine equality.
                */
                if (property.ConceptualAssociation?.ReferentialConstraint != null) {
                    var crc = property.ConceptualAssociation.ReferentialConstraint;
                    property.StorageAssociation = entity.StorageAssociations?
                        .FirstOrDefault(o =>
                            o.Name.EqualsIgnoreCase(property.ConceptualAssociation.Name) || crc.Equals(o.ReferentialConstraint));
                    if (property.StorageAssociation == null) {
                        // take a broader look. suspicious end Type in here
                        var match = storageAssociationsByName.TryGetValue(relationship, out var v) ? v : null;
                        if (match != null) property.StorageAssociation = match;
                    }
#if DEBUGPARSER
                    Debug.Assert(property.StorageAssociation != null);
#endif
                }
            }
        }

        var associationSetMappings = edmx.Mappings.Mapping.EntityContainerMapping?.AssociationSetMapping;

        var entitiesWithBaseTypes = new List<EntityType>();

        // with all entities loaded, perform base type linking
        foreach (var entity in Entities) {
            if (!entity.ConceptualEntity.BaseType.HasNonWhiteSpace()) continue;

            // resolve entity base type
            EntityType baseType;
            if (entity.ConceptualEntity.BaseType.Contains("."))
                baseType = Entities.FirstOrDefault(o => o.FullName == entity.ConceptualEntity.BaseType);
            else
                baseType = Entities.FirstOrDefault(o => o.Name == entity.ConceptualEntity.BaseType);
            Debug.Assert(baseType != null);
            entity.BaseType = baseType;
            entitiesWithBaseTypes.Add(entity);
        }

        // with all entities loaded, perform association linking
        foreach (var entity in Entities) {
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
                    ResolveFkAssociation(navigation, ass, endRoles, toEndRole, edmxModel);
                }
#if DEBUGPARSER
                if (navigation.Association == null && Debugger.IsAttached)
                    Debugger.Break(); // invalid association. May be design time only?
#endif
            }
        }

        var hierarchicalRoots = new HashSet<EntityType>();
        foreach (var grp in entitiesWithBaseTypes.GroupBy(o => o.BaseType)) {
            var baseType = grp.Key;
            Debug.Assert(baseType.DerivedTypes.Count > 0);
            var hierarchyRoot = baseType.GetHierarchyRoot();
            hierarchicalRoots.Add(hierarchyRoot);

            var allBaseProperties = baseType.GetProperties().Where(o => o.StorageProperty?.Name != null)
                .DistinctBy(o => o.ColumnName).ToDictionary(o => o.ColumnName);
            var allBaseNavigations = baseType.GetNavigations().ToHashSetNew();
            var baseTable = baseType.StorageEntity != null ? baseType.StorageFullName : baseType.GetHierarchyRoot()?.StorageFullName;
            Debug.Assert(baseTable.HasNonWhiteSpace());
            foreach (var derivedType in grp) {
                Debug.Assert(derivedType.BaseType == baseType);
                Debug.Assert(baseType.DerivedTypes.Contains(derivedType));

                var derivedTable = derivedType.StorageEntity != null ? baseType.StorageFullName : null;
                if (derivedTable != null && baseTable != derivedTable) continue;

                // remove properties from derived types that are already mapped in base types
                ConsolidateInheritedProperties(derivedType, allBaseProperties, allBaseNavigations);
            }
        }

        foreach (var hierarchicalRoot in hierarchicalRoots) {
            DetermineInheritanceStrategy(hierarchicalRoot);
        }

#if DEBUGPARSER
        // ensure associations are properly linked.
        var navsWithNoAssociation = Entities.SelectMany(o => o.NavigationProperties).Where(o => o.Association == null)
            .ToHashSetNew();
        var associationsLinked = Entities.SelectMany(o => o.Associations).ToHashSetNew();
        var allAssociations = edmx.ConceptualModels.Schema.Associations.ToHashSetNew();
        var associationsNotLinked = allAssociations.Where(o => !associationsLinked.Contains(o))
            .OrderBy(o => o.ReferentialConstraint.Principal.Role).ToArray();
        Debug.Assert(navsWithNoAssociation.Count == 0);
        Debug.Assert(associationsNotLinked.Length == 0);
#endif

        // State.AssociationsByName = Entities.SelectMany(o => o.NavigationProperties)
        //     .Where(o => o.Association != null)
        //     .Select(o => o.Association)
        //     .GroupBy(o => o.Name)
        //     .ToDictionary(o => o.Key, o => o.First(), StringComparer.InvariantCultureIgnoreCase);
        return State;
    }

    private void TryUpgrade(ref string edmxContent) {
        var nsPairs = new string[][] {
            new[] { @"""http://schemas.microsoft.com/ado/2008/10/edmx""", @"""http://schemas.microsoft.com/ado/2009/11/edmx""" },
            new[] { @"""http://schemas.microsoft.com/ado/2008/09/mapping/cs""", @"""http://schemas.microsoft.com/ado/2009/11/mapping/cs""" },
            new[] { @"""http://schemas.microsoft.com/ado/2008/10/edmx""", @"""http://schemas.microsoft.com/ado/2009/11/edmx""" },
            new[] { @"""http://schemas.microsoft.com/ado/2009/02/edm/ssdl""", @"""http://schemas.microsoft.com/ado/2009/11/edm/ssdl""" },
            new[] { @"""http://schemas.microsoft.com/ado/2008/09/edm""", @"""http://schemas.microsoft.com/ado/2009/11/edm""" },
        };
        foreach (var nsPair in nsPairs) {
            Debug.Assert(edmxContent.Contains(nsPair[0]));
            edmxContent = edmxContent.Replace(nsPair[0], nsPair[1]);
        }
    }

    private static void IdentifyKeys(EntityType entity, EntityProperty property) {
        if (entity.ConceptualEntity?.Key?.PropertyRefs?.Count > 0 && property.ConceptualName.HasNonWhiteSpace()) {
            var key = entity.ConceptualEntity.Key.PropertyRefs.FirstOrDefault(o => o.Name == property.ConceptualName);
            property.IsConceptualKey = key != null;
            if (property.IsConceptualKey)
                entity.ConceptualKey.Add(property);
        }

        if (entity.StorageEntity?.Key?.PropertyRefs?.Count > 0 && property.ColumnName.HasNonWhiteSpace()) {
            var key = entity.StorageEntity.Key.PropertyRefs.FirstOrDefault(o => o.Name == property.ColumnName);
            property.IsStorageKey = key != null;
            if (property.IsStorageKey)
                entity.StorageKey.Add(property);
        }
    }

    private static void ConsolidateInheritedProperties(EntityType derivedType, Dictionary<string, EntityProperty> allBaseProperties,
        HashSet<NavigationProperty> allBaseNavigations) {
        // remove properties from derived types that are already mapped in base types
        var toRemove = derivedType.Properties
            .Where(o => o.ColumnName != null)
            .Select(o => (Property: o, Keeper: allBaseProperties.TryGetValue(o.ColumnName)))
            .Where(o => o.Keeper != null)
            .ToArray();
        foreach (var (rem, keeper) in toRemove) {
            if (rem.IsMapped && !keeper.IsMapped)
                continue; // property is meant only for derived type
            if (rem.IsConceptualKey) {
                if (rem.Entity.ConceptualKey.Contains(rem)) {
                    rem.Entity.ConceptualKey.Remove(rem);
                    rem.Entity.ConceptualKey.Add(keeper);
                }
            }

            if (rem.IsStorageKey) {
                if (rem.Entity.StorageKey.Contains(rem)) {
                    rem.Entity.StorageKey.Remove(rem);
                    rem.Entity.StorageKey.Add(keeper);
                }
            }

            foreach (var navigation in allBaseNavigations) {
                if (!navigation.FkProperties.IsNullOrEmpty())
                    for (var i = 0; i < navigation.FkProperties.Length; i++) {
                        var property = navigation.FkProperties[i];
                        if (property == rem) navigation.FkProperties[i] = keeper;
                    }

                if (navigation.Association is FkAssociation fkAssociation && fkAssociation.ReferentialConstraint != null) {
                    for (var i = 0; i < fkAssociation.ReferentialConstraint.DependentProperties.Length; i++) {
                        var property = fkAssociation.ReferentialConstraint.DependentProperties[i];
                        if (property == rem) fkAssociation.ReferentialConstraint.DependentProperties[i] = keeper;
                    }

                    for (var i = 0; i < fkAssociation.ReferentialConstraint.PrincipalProperties.Length; i++) {
                        var property = fkAssociation.ReferentialConstraint.PrincipalProperties[i];
                        if (property == rem) fkAssociation.ReferentialConstraint.PrincipalProperties[i] = keeper;
                    }
                }
            }

            var removed = derivedType.Properties.Remove(rem);
            Debug.Assert(removed);
        }
    }

    private void DetermineInheritanceStrategy(EntityType hierarchicalRoot, HashSet<EntityType> scope = null) {
        // at the root of each relational strategy.  try to identify what strategy is used.
        var fullHierarchyQuery = hierarchicalRoot.GetAllDerivedTypes(true);
        if (scope != null) fullHierarchyQuery = fullHierarchyQuery.Where(scope.Contains);
        var fullHierarchy = fullHierarchyQuery.Distinct().ToHashSetNew();
        Debug.Assert(fullHierarchy.Contains(hierarchicalRoot));
        if (fullHierarchy.Count <= 1) return;

        //var concreteTypes = fullHierarchy.Where(o => !o.IsAbstract).ToList();
        var baseTypes = fullHierarchy.Where(o => o.BaseType != null && fullHierarchy.Contains(o.BaseType))
            .Select(o => o.BaseType).Distinct().ToHashSetNew();
        var leafTypes = fullHierarchy.Where(o => !o.IsAbstract && !baseTypes.Contains(o)).ToHashSetNew();
        var dbTables = fullHierarchy
            .Where(o => o.StorageEntitySet != null && o.StorageEntity != null)
            .Select(o => o.StorageFullName)
            .Distinct()
            .ToHashSetNew();
        var tphRoots = new HashSet<EntityType>();
        var withDiscriminatorConditions = fullHierarchy.Where(o => o.Discriminator?.ColumnName != null).ToArray();
        var discriminatorsMatched = 0;
        if (withDiscriminatorConditions.Length > 0) {
            // discriminator conditions are associated specifically to TPH mapping.
            foreach (var entityType in withDiscriminatorConditions) {
                var d = entityType.Discriminator;
                var prop = baseTypes.Where(e => e.StorageEntity?.Properties?.Count > 0)
                    .SelectMany(e => e.Properties.Select(p => (e, p)))
                    .FirstOrDefault(o => d.ColumnName == o.p.ColumnName);
                if (prop.p == null) continue;

                // add the Discriminator mapping to the entity that owns the discriminator property
                prop.e.DiscriminatorPropertyMappings.Add((prop.p, d, entityType));
                discriminatorsMatched++;
            }

            if (discriminatorsMatched > 0)
                foreach (var entityType in baseTypes.Where(o => o.DiscriminatorPropertyMappings.Count > 0)) {
                    entityType.RelationalMappingStrategy = "TPH";
                    if (entityType.BaseType != null) {
                        logger.WriteWarning(
                            $"Entity {entityType.Name} is a TPH root, but also has a base type.  Mixed inheritance may be a problem for scaffolding in EF Core.");
                        //entityType.BaseType = null;
                    }

                    tphRoots.Add(entityType);
                }
        }

        foreach (var tphRoot in tphRoots) {
            // a TPH mapping should involve 1 table, making it impossible for leafs to be involved in another strategy
            // the root should not be simultaneously involved in any other strategy, such as TPT
            // therefore, any entities not directly identified by the TPH mapping should be broken apart
            var tphHierarchy = tphRoot.GetAllDerivedTypes(false).Distinct().ToHashSetNew();
            fullHierarchy.Remove(tphRoot);
            foreach (var mapping in tphRoot.DiscriminatorPropertyMappings) {
                tphHierarchy.Remove(mapping.ToEntity);
                fullHierarchy.Remove(mapping.ToEntity);
            }

            if (tphHierarchy.Count > 0) {
                // there are derived entities that have no discriminator mapping.
                // we should break the inheritance apart to avoid scaffolding errors.
                foreach (var derivedEntity in tphHierarchy.Where(o => !o.IsAbstract)) {
                    logger.WriteWarning(
                        $"Entity {derivedEntity.Name} has an invalid mapping to {tphRoot.Name}. Removing base type annotation.");
                    derivedEntity.BaseType = null;
                }
            }
        }

        if (tphRoots.Contains(hierarchicalRoot)) return; // TPH root cannot involve any other strategies
        if (tphRoots.Count > 0) {
            // subsections of the tree have TPH inheritance. remove those areas before inspecting further
            // it may no longer be one contiguous hierarchy, therefore, we should run the roots and run inspection again.
            var hierarchicalRoots = new HashSet<EntityType>();
            foreach (var grp in fullHierarchy.Where(o => o.BaseType != null).GroupBy(o => o.BaseType)) {
                var bt = grp.Key;
                var hierarchyRoot = bt.GetHierarchyRoot();
                hierarchicalRoots.Add(hierarchyRoot);
            }

            foreach (var hierarchicalRoot2 in hierarchicalRoots) {
                DetermineInheritanceStrategy(hierarchicalRoot2, fullHierarchy);
            }

            return;
        }


        if (dbTables.Count == 1) {
            // full hierarchy mapped to 1 table. TPH?
            //var withoutConditions = entities.Where(o => o.Discriminator == null).ToArray();
            if (!withDiscriminatorConditions.Any() || hierarchicalRoot.Discriminator != null) return;
            // this is a TPH mapping strategy

            if (discriminatorsMatched <= 0) return;
            foreach (var entityType in baseTypes.Where(o => o.DiscriminatorPropertyMappings.Count > 0)) {
                entityType.RelationalMappingStrategy = "TPH";
            }
        } else if (dbTables.Count == leafTypes.Count) {
            // table for each concrete type = TPC
            // can also add check to ensure that no table exists for the root entity.
            if (hierarchicalRoot.StorageEntity?.Name == null) {
                hierarchicalRoot.RelationalMappingStrategy = "TPC";
            }
        } else if (dbTables.Count > leafTypes.Count) {
            // table for each concrete type + abstract types = TPT
            // can also add check to ensure each table has FK back to root entity table.
            hierarchicalRoot.RelationalMappingStrategy = "TPT";
        }
    }

    private void ResolveDesignAssociation(NavigationProperty navigation, ConceptualAssociation ass,
        EndRole[] endRoles, EndRole toEndRole,
        List<AssociationSetMapping> associationSetMappings) {
        if (associationSetMappings.IsNullOrEmpty()) return;
        var associationSetMapping = associationSetMappings.FirstOrDefault(o =>
            o.Name == navigation.Relationship.SplitNamespaceAndName().name);
        if (associationSetMapping?.EndProperties?.Count != 2) return;
        var endProperty =
            associationSetMapping.EndProperties.FirstOrDefault(o => o.Name == navigation.ConceptualName);
        if (endProperty == null) return;
        var inverseEndProperty = associationSetMapping.EndProperties?.FirstOrDefault(o => o != endProperty);
        if (inverseEndProperty == null) return;
        var inverseEntity = toEndRole?.Entity;
        var inverseNavigation =
                inverseEntity?.GetNavigations()?.FirstOrDefault(o => o.ConceptualName == inverseEndProperty.Name) ??
                inverseEntity?.GetNavigations()?.FirstOrDefault(o => o.Relationship == navigation.Relationship)
            ;
        if (navigation == inverseNavigation) {
            logger.WriteWarning($"Invalid design navigation {navigation.ConceptualName}: navigation == inverse");
            return;
        }

        if (inverseNavigation == null && !navigation.IsPrincipalEnd) {
            logger.WriteWarning($"Invalid design navigation {navigation.ConceptualName}: inverse cannot be resolved");
            return; // allow
        }

        // Note, for a many-to-many, there are no dependents in the end entities. Rather, the FKs are all in the junction,
        // which has no conceptual representation.  Therefore, the ScalarProperties in the mapping ends refer to the junction only.
        var a = new DesignAssociation(ass, endRoles, navigation, inverseNavigation);
        if (navigation.Association != a) throw new InvalidConstraintException("Association not wired to navigation");
    }

    private void ResolveFkAssociation(NavigationProperty navigation, ConceptualAssociation ass,
        EndRole[] endRoles, EndRole toEndRole, EdmxRoot edmx) {
        var constraints = new List<IReferentialConstraint>();
        if (navigation?.ConceptualAssociation?.ReferentialConstraint != null)
            constraints.Add(navigation?.ConceptualAssociation?.ReferentialConstraint);
        if (navigation?.StorageAssociation?.ReferentialConstraint != null)
            constraints.Add(navigation?.StorageAssociation?.ReferentialConstraint);
        if (constraints.IsNullOrEmpty()) return;
        EntityProperty[] pProps = null;
        EntityProperty[] dProps = null;
        EndRole principalEndRole = null;
        EndRole dependentEndRole = null;
        foreach (var constraint in constraints) {
            principalEndRole ??= endRoles.FirstOrDefault(o => o.Role == constraint.Principal.Role);
            dependentEndRole ??= endRoles.FirstOrDefault(o => o.Role == constraint.Dependent.Role);
        }

        if (principalEndRole?.Entity == null || dependentEndRole?.Entity == null) return;

        foreach (var constraint in constraints) {
            if (pProps.IsNullOrEmpty())
                pProps = GetProperties(principalEndRole.Entity,
                    constraint.Principal.Properties.Select(o => o.Name).ToArray(), constraint.IsConceptual);
            if (dProps.IsNullOrEmpty())
                dProps = GetProperties(dependentEndRole.Entity,
                    constraint.Dependent.Properties.Select(o => o.Name).ToArray(), constraint.IsConceptual);
        }

        if (pProps.IsNullOrEmpty() || dProps.IsNullOrEmpty() || pProps.Length != dProps.Length) return;

        var inverseNavigation =
            toEndRole.Entity.GetNavigations().SingleOrDefault(o =>
                o.Relationship == navigation.Relationship && o != navigation);

        if (navigation == inverseNavigation) {
            logger.WriteWarning($"Invalid navigation {navigation.ConceptualName}: navigation == inverse");
            return;
        }

        if (inverseNavigation == null && !navigation.IsPrincipalEnd) {
            logger.WriteWarning($"Invalid navigation {navigation.ConceptualName}: inverse cannot be resolved");
            return; // allow
        }

        var a = new FkAssociation(ass, endRoles, navigation, inverseNavigation,
            new(constraints, pProps, dProps, principalEndRole.Entity, dependentEndRole.Entity)
        );
        if (navigation.Association != a) throw new InvalidConstraintException("Association not wired to navigation");

        a.ReferentialConstraint.StorageAssociation ??= navigation.StorageAssociation;
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

    private EntityProperty[] GetProperties(EntityType entity, string[] propNames, bool namesAreConceptual) {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        var props = propNames.Select(n => entity.GetProperties(isMapped: namesAreConceptual ? true : null)
                .FirstOrDefault(o => o.GetName(namesAreConceptual).EqualsIgnoreCase(n)))
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

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
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

    private EdmxParsed State { get; set; }

    private string FilePath => State.FilePath;

    //private Dictionary<string, AssociationBase> AssociationsByName => State.AssociationsByName;
    private Dictionary<string, EnumType> EnumsByName => State.EnumsByName;
    private Dictionary<string, EnumType> EnumsByConceptualSchemaName => State.EnumsByConceptualSchemaName;
    private Dictionary<string, EnumType> EnumsByExternalTypeName => State.EnumsByExternalTypeName;
    private ObservableCollection<Schema> Schemas => State.Schemas;
    private ObservableCollection<EntityType> Entities => State.Entities;

    private ObservableCollection<NavigationProperty> NavProps => State.NavProps;
    //private ObservableCollection<EntityProperty> Props => State.Props;

    #endregion
}
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EntityFrameworkRuler.Design.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Castle.DynamicProxy;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Design.Services.Models;
using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using IInterceptor = Castle.DynamicProxy.IInterceptor;
using EntityFrameworkRuler.Common.Annotations;
using Humanizer;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ValueGeneration;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Design.Services;

/// <summary>
/// This override will apply custom property type mapping to the generated entities.
/// It is also possible to remove columns at this level.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class RuledRelationalScaffoldingModelFactory : IScaffoldingModelFactory, IInterceptor {
    private readonly IMessageLogger reporter;
    private readonly IDesignTimeRuleLoader designTimeRuleLoader;
    private readonly IRuleModelUpdater ruleModelUpdater;
    private readonly ICandidateNamingService candidateNamingService;
    private readonly IPluralizer pluralizer;
    private readonly ICSharpUtilities cSharpUtilities;
    private DbContextRuleNode dbContextRule;
    private readonly RelationalScaffoldingModelFactory proxy;
    private readonly MethodInfo visitForeignKeyMethod;
    private readonly MethodInfo addNavigationPropertiesMethod;
    private readonly MethodInfo visitTableMethod;
    private readonly MethodInfo getEntityTypeNameMethod;
    private readonly MethodInfo assignOnDeleteActionMethod;


    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected readonly ScaffoldedTableTracker ScaffoldTracker;

    private RuledCSharpUniqueNamer<DatabaseTable, EntityRule> tableNamer;
    private RuledCSharpUniqueNamer<DatabaseTable, EntityRule> dbSetNamer;
    private ModelReverseEngineerOptions options;
    private DatabaseModel generatingDatabaseModel;

    /// <summary> enlists post creation actions to be executed after visiting database model </summary>
    protected readonly List<Action<ModelBuilder>> PostCreationActions = new();

    private readonly HashSet<string> ignoreEntityAnnotations = new(new[] {
        EfCoreAnnotationNames.DiscriminatorProperty,
        EfCoreAnnotationNames.DiscriminatorValue,
        EfCoreAnnotationNames.DiscriminatorMappingComplete
    });

    private KeyBuilder randomKeyBuilder;
    private CSharpHelper code;


    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuledRelationalScaffoldingModelFactory(IServiceProvider serviceProvider,
        IMessageLogger reporter,
        IDesignTimeRuleLoader designTimeRuleLoader,
        IRuleModelUpdater ruleModelUpdater,
        ICandidateNamingService candidateNamingService,
        IPluralizer pluralizer,
        ICSharpUtilities cSharpUtilities) {
        this.reporter = reporter;
        this.designTimeRuleLoader = designTimeRuleLoader;
        this.ruleModelUpdater = ruleModelUpdater;
        this.candidateNamingService = candidateNamingService;
        this.pluralizer = pluralizer;
        this.cSharpUtilities = cSharpUtilities;
        ScaffoldTracker = new(reporter);
        // avoid runtime binding errors against EF6 by using reflection and a proxy to access the resources we need.
        // this allows more fluid compatibility with EF versions without retargeting this project.

        try {
            proxy = serviceProvider.CreateClassProxy<RelationalScaffoldingModelFactory>(this);
        } catch (Exception ex) {
            reporter.WriteError($"Error creating proxy of RelationalScaffoldingModelFactory: {ex.Message}");
            throw;
        }

        var t = typeof(RelationalScaffoldingModelFactory);
        // protected virtual IMutableForeignKey? VisitForeignKey(ModelBuilder modelBuilder,DatabaseForeignKey foreignKey)
        visitForeignKeyMethod = GetMethodOrLog("VisitForeignKey", o => t.GetMethod<ModelBuilder, DatabaseForeignKey>(o));

        // protected virtual void AddNavigationProperties(IMutableForeignKey foreignKey)
        addNavigationPropertiesMethod = GetMethodOrLog("AddNavigationProperties", o => t.GetMethod<IMutableForeignKey>(o));

        // protected virtual string GetEntityTypeName(DatabaseTable table)
        getEntityTypeNameMethod = GetMethodOrLog("GetEntityTypeName", o => t.GetMethod<DatabaseTable>(o));

        // protected virtual EntityTypeBuilder? VisitTable(ModelBuilder modelBuilder, DatabaseTable table)
        visitTableMethod = GetMethodOrLog("VisitTable", o => t.GetMethod<ModelBuilder, DatabaseTable>(o));

        // private static void AssignOnDeleteAction(DatabaseForeignKey databaseForeignKey, IMutableForeignKey foreignKey)
        assignOnDeleteActionMethod =
            GetMethodOrLog("AssignOnDeleteAction", o => t.GetStaticMethod<DatabaseForeignKey, IMutableForeignKey>(o));

        MethodInfo GetMethodOrLog(string name, Func<string, MethodInfo> getter) {
            var m = getter(name);
            if (m == null)
                reporter.WriteWarning($"Method not found: RelationalScaffoldingModelFactory.{name}()");
            return m;
        }
    }

    /// <inheritdoc />
    public IModel Create(DatabaseModel databaseModel, ModelReverseEngineerOptions ops) {
        var model = proxy.Create(databaseModel, ops);
        return model;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual IModel Create(DatabaseModel databaseModel, ModelReverseEngineerOptions ops,
        Func<DatabaseModel, ModelReverseEngineerOptions, IModel> baseCall) {
        options = ops;
        generatingDatabaseModel = databaseModel;
        Func<DatabaseTable, NamedElementState<DatabaseTable, EntityRule>> tableNameAction;
        Func<DatabaseTable, NamedElementState<DatabaseTable, EntityRule>> dbSetNameAction;

        // Note, table naming logic has to be overriden at this level because the pluralizer step is executed AFTER
        // the CandidateNamingService returns its result.  This means that a user specified name will be subject to change
        // by the pluralizer/singularizer.  To avoid altering the user's input, we have to return more information about
        // a candidate name, hence NamedElementState, where IsFrozen can be set.

        // Preventing the pluralizer from affecting navigation names set by the user would involve replacing VisitForeignKeys
        // and AddNavigationProperties, which has significant EF wiring logic - so this is not advisable.
        // As an alternative, we may consider setting _options.NoPluralize to true during the processing of these methods only, and
        // moving the pluralize call into GetDependentEndCandidateNavigationPropertyName/GetPrincipalEndCandidateNavigationPropertyName.
        // However it is less likely there will be any need for this measure to protect nav names.

        if (ops.UseDatabaseNames) {
            tableNameAction = t => new(t.Name, t);
            dbSetNameAction = t => new(t.Name, t);
        } else {
            if (candidateNamingService is RuledCandidateNamingService ruledNamer) {
                tableNameAction = t => ruledNamer.GenerateCandidateNameState(t);
                dbSetNameAction = t => ruledNamer.GenerateCandidateNameState(t, true);
            } else
                dbSetNameAction = tableNameAction = t => new(candidateNamingService.GenerateCandidateIdentifier(t), t);
        }

        tableNameAction = tableNameAction.Cached();
        dbSetNameAction = dbSetNameAction.Cached();

        tableNamer = new(tableNameAction, cSharpUtilities, ops.NoPluralize ? null : pluralizer.Singularize);
        dbSetNamer = new(dbSetNameAction, cSharpUtilities, ops.NoPluralize ? null : pluralizer.Pluralize);

        var model = baseCall(databaseModel, ops);
        ruleModelUpdater?.OnModelCreated(model);
        return model;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual TypeScaffoldingInfo GetTypeScaffoldingInfo(DatabaseColumn column, Func<TypeScaffoldingInfo> baseCall) {
        var typeScaffoldingInfo = baseCall();
        Debug.Assert(explicitEntityRuleMapping.table == column.Table);
        var entityRule = explicitEntityRuleMapping.table == column.Table ? explicitEntityRuleMapping.entityRule : null;
        if (entityRule == null) return typeScaffoldingInfo;

        var propertyRule = entityRule.TryResolveRuleFor(column.Name);
        if (propertyRule == null || propertyRule.Rule.NewType.HasNonWhiteSpace() != true) return typeScaffoldingInfo;

        var clrType = designTimeRuleLoader?.TryResolveType(propertyRule.Rule.NewType, typeScaffoldingInfo?.ClrType, reporter);
        if (clrType == null) return typeScaffoldingInfo;
        reporter.WriteVerbose($"RULED: Column {column.Table.GetFullName()}.{column.Name} type set to {clrType.FullName}");
        // Regenerate the TypeScaffoldingInfo based on our new CLR type.
        return typeScaffoldingInfo.WithType(clrType);
    }

    private (DatabaseTable table, EntityRuleNode entityRule) explicitEntityRuleMapping;
    private (EntityRuleNode Dependent, EntityRuleNode Principal, ForeignKeyRuleNode FkRule) explicitFkEntityMapping;

    /// <summary> Get the entity rule for this table </summary>
    protected virtual ICollection<EntityRuleNode> TryResolveRuleFor(DatabaseTable table) {
        if (table != null) {
            if (explicitEntityRuleMapping.table == table) return new[] { explicitEntityRuleMapping.entityRule };

            if (explicitFkEntityMapping.Dependent?.DatabaseTable?.Table == table) {
                if (explicitFkEntityMapping.Dependent != null) return new[] { explicitFkEntityMapping.Dependent };
                if (explicitFkEntityMapping.Dependent.Rule.NewName.HasNonWhiteSpace())
                    return new[] { explicitFkEntityMapping.Dependent };
            }

            if (explicitFkEntityMapping.Principal?.DatabaseTable?.Table == table) {
                if (explicitFkEntityMapping.Principal != null) return new[] { explicitFkEntityMapping.Principal };
                if (explicitFkEntityMapping.Principal.Rule.NewName.HasNonWhiteSpace())
                    return new[] { explicitFkEntityMapping.Principal };
            }
        }

        dbContextRule ??= designTimeRuleLoader?.GetDbContextRules() ?? new DbContextRuleNode(DbContextRule.DefaultNoRulesFoundBehavior);
        var tableNodes = dbContextRule.TryResolveRuleFor(table);
        return tableNodes ?? Array.Empty<EntityRuleNode>();
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    // ReSharper disable once RedundantAssignment
    protected virtual ModelBuilder VisitDatabaseModel(ModelBuilder modelBuilder, DatabaseModel databaseModel, Func<ModelBuilder> baseCall) {
        modelBuilder = baseCall();
        foreach (var action in PostCreationActions) action(modelBuilder);
        // Model post processing.
#if DEBUG
        var employee = modelBuilder.Model.FindEntityType("Employee");
        if (employee != null) {
            var navs = employee.GetNavigations().ToList();
            var orders = navs.FirstOrDefault(o => o.Name == "Orders");
            Debug.Assert(orders != null);
        }

        var BaseParasolidDefinition = modelBuilder.Model.FindEntityType("BaseParasolidDefinition");
        if (BaseParasolidDefinition != null) {
            var navs = BaseParasolidDefinition.GetNavigations().ToList();
            var orders = navs.FirstOrDefault(o => o.Name == "Orders");
            //Debug.Assert(orders != null);
        }

        var BaseParasolidLatheDefinition = modelBuilder.Model.FindEntityType("BaseParasolidLatheDefinition");
        if (BaseParasolidLatheDefinition != null) {
            var navs = BaseParasolidLatheDefinition.GetNavigations().ToList();
            var orders = navs.FirstOrDefault(o => o.Name == "Orders");
            //Debug.Assert(orders != null);
        }
#endif
        return modelBuilder;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual ModelBuilder VisitTables(ModelBuilder modelBuilder, ICollection<DatabaseTable> tables) {
        dbContextRule ??= designTimeRuleLoader?.GetDbContextRules() ?? new DbContextRuleNode(DbContextRule.DefaultNoRulesFoundBehavior);
        ScaffoldTracker.InitializeScope(tables, dbContextRule);

        foreach (var entityRule in dbContextRule.Entities) {
            var schemaName = entityRule.Parent.Rule.SchemaName.EmptyIfNullOrWhitespace();
            var tableName = entityRule.DbName ?? string.Empty;
            var table = ScaffoldTracker.FindTableNode(schemaName, tableName);
            var includeSchema = entityRule.Parent.Rule.ShouldMap();
            var includeEntity = entityRule.Rule.ShouldMap() && includeSchema;

            if (table != null) {
                entityRule.MapTo(table);
                Debug.Assert(table.EntityRules.Contains(entityRule));

                if (!includeSchema) {
                    ScaffoldTracker.OmitSchema(table.Schema);
                    continue;
                }

                if (!includeEntity) {
                    ScaffoldTracker.OmitTable(table);
                    continue;
                }
            }

            if (table == null && entityRule.BaseEntityRuleNode == null) {
                // invalid entry
                if (entityRule.Rule.ShouldMap()) {
                    if (tableName.HasNonWhiteSpace())
                        reporter.WriteWarning(
                            $"RULED: Entity for {schemaName}.{tableName} cannot be generated because the table cannot be found.");
                    else {
                        var name = entityRule.GetFinalName();
                        if (name.HasNonWhiteSpace())
                            reporter.WriteVerbose(
                                $"RULED: Entity for rule '{name}' cannot be generated because no table or base type is defined.");
                    }
                }

                continue;
            }

            if (table != null)
                DoInvokeVisitTable(table, entityRule);
            else {
                // Entity with no database table.  A base type must be available otherwise it can't be created.
                string entityTypeName = null;
                if (entityRule.Rule.NewName.HasNonWhiteSpace() || entityRule.Rule.EntityName.HasNonWhiteSpace())
                    entityTypeName = entityRule.GetFinalName().Trim();

                if (entityTypeName == null || !entityTypeName.IsValidSymbolName()) {
                    reporter.WriteWarning($"RULED: Entity '{entityTypeName}' cannot be generated because it has an invalid name.");
                    continue;
                }

                var builder = modelBuilder.Entity(entityTypeName);
                builder = ApplyEntityRules(modelBuilder, builder, null, entityRule);
                Debug.Assert(ReferenceEquals(entityRule.Builder, builder));
            }
        }

        if (!dbContextRule.Rule.IncludeUnknownSchemas &&
            !dbContextRule.Rule.Schemas.Any(s => s.IncludeUnknownTables || s.IncludeUnknownViews)) return modelBuilder;

        // we should perform another pass over the list to include any extra tables that were missing from the rule collection
        foreach (var kvp in ScaffoldTracker.Tables) {
            var schema = kvp.Schema;
            var schemaRule = dbContextRule.Schemas.GetByDbName(schema);
            var includeSchema = schemaRule?.NotMapped == false || (schemaRule == null && dbContextRule.Rule.IncludeUnknownSchemas);
            if (!includeSchema) {
                ScaffoldTracker.OmitSchema(schema);
                continue;
            }

            schemaRule ??= dbContextRule.AddSchema(schema); // add on the fly

            foreach (var table in kvp.Tables) {
                if (table.EntityRules.Count > 0 || table.Builders.Any()) continue; // it's already been mapped
                var entityRules = schemaRule.TryResolveRuleFor(table.Name);
                var includeEntity = CanGenerateEntity(schemaRule, entityRules, table);
                if (!includeEntity) {
                    ScaffoldTracker.OmitTable(table);
                    continue;
                }

                // Note, we ONLY generate entities at this point when NO rule exists. Therefore, we always add on the fly now
                var entityRule = schemaRule.AddEntity(table.Name);
                DoInvokeVisitTable(table, entityRule);
            }
        }

        return modelBuilder;

        void DoInvokeVisitTable(DatabaseTableNode table, EntityRuleNode entityRule) {
            if (entityRule == null) throw new ArgumentNullException(nameof(entityRule));
            explicitEntityRuleMapping = (table, entityRule);
            try {
                // We have to call the base VisitTable in order to perform the basic wiring.
                // The call will be captured, and the result of the wiring will be customized based on the rules.
                var builder = InvokeVisitTable(modelBuilder, table);
                if (builder != null && !ReferenceEquals(entityRule.Builder, builder))
                    throw new("Builder not linked to entity rule properly");
                table.MapTo(entityRule);
            } finally {
                explicitEntityRuleMapping = default;
            }
        }

        bool CanGenerateEntity(SchemaRuleNode schemaRule, ICollection<EntityRuleNode> entityRule, DatabaseTableNode table) {
            if (entityRule?.Any(o => o != null) == true) return false;
            if (schemaRule == null) return true;

            var isView = table.Table is DatabaseView;
            if (isView) {
                if (!schemaRule.Rule.IncludeUnknownViews) return false;
            } else {
                // ensure M2M junctions are not auto-excluded
                if (table.Table.IsSimpleManyToManyJoinEntityType()) return true;
                if (!schemaRule.Rule.IncludeUnknownTables) return false;
            }

            return true;
        }
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    private EntityTypeBuilder InvokeVisitTable(ModelBuilder modelBuilder, DatabaseTable table) {
        return visitTableMethod?.Invoke(proxy, new object[] { modelBuilder, table }) as EntityTypeBuilder;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual EntityTypeBuilder VisitTable(ModelBuilder modelBuilder, DatabaseTable table, Func<EntityTypeBuilder> baseCall) {
        var entityRules = TryResolveRuleFor(table);
        entityRules = entityRules.Count > 1 ? entityRules.Where(o => o.Rule.ShouldMap()).ToArray() : entityRules;
        Debug.Assert(entityRules.Count <= 1);
        var entityRule = entityRules.FirstOrDefault();
        if (entityRule != null && table is DatabaseView view) {
            // views require that keys are applied manually.  because nullability is changed here,
            // it will then influence  the nullability of the resulting entity properties and increase
            // likelihood that the entity key get is generated.
            TryAddTableKey(view, entityRules);
        }

        return ApplyEntityRules(modelBuilder, baseCall(), table, entityRule);
    }


    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    protected virtual EntityTypeBuilder ApplyEntityRules(ModelBuilder modelBuilder,
        EntityTypeBuilder entityTypeBuilder,
        DatabaseTable table, EntityRuleNode entityRuleNode) {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        var entityRule = entityRuleNode?.Rule;
        if (entityRule == null) return entityTypeBuilder;
        Debug.Assert(entityRule.ShouldMap());
        if (entityTypeBuilder == null) return null;

        entityRuleNode.MapTo(entityTypeBuilder);
        bool isTphLeaf = false;
        if (entityRuleNode.BaseEntityRuleNode != null) {
            // get the base entity builder and reference the type directly.
            Debug.Assert(entityRuleNode.IsAlreadyScaffolded);

            var baseName = entityRuleNode.BaseEntityRuleNode.Builder?.Metadata.Name ?? entityRuleNode.BaseEntityRuleNode.GetFinalName();

            // Note, upon setting the base type, shared properties will be removed from the derived type
            entityTypeBuilder.HasBaseType(baseName);

            var baseStrategy = entityRuleNode.GetBaseTypes().Select(o => o.Rule.GetMappingStrategy()?.ToUpper())
                .FirstOrDefault(o => o.HasNonWhiteSpace());
            if (baseStrategy?.Length == 3)
                switch (baseStrategy) {
                    case "TPH":
                        // ToTable() and DbSet should be REMOVED for TPH leafs
                        isTphLeaf = true;
                        entityTypeBuilder.ToTable((string)null);
                        entityTypeBuilder.Metadata.RemoveAnnotation(EfScaffoldingAnnotationNames.DbSetName);
                        entityTypeBuilder.Metadata.RemoveAnnotation(EfRelationalAnnotationNames.Schema);
                        entityTypeBuilder.Metadata.RemoveAnnotation(EfRelationalAnnotationNames.TableName);
                        break;
                    case "TPT":
                        break;
                    case "TPC":
                        break;
                }
        }

        var strategy = entityRule.GetMappingStrategy()?.ToUpper();
        if (strategy?.Length == 3)
            // This is root of a hierarchy
            switch (strategy) {
                case "TPH":
                    // ToTable() and DbSet should be defined for TPH root
                    entityTypeBuilder.ToTable(table.Name);
                    var discriminatorColumn = entityRule.GetDiscriminatorColumn() ??
                                              entityRule.Properties.FirstOrDefault(o => o.DiscriminatorConditions.Count > 0)?.Name;

                    if (discriminatorColumn.HasNonWhiteSpace()) {
                        //var assets = new DiscriminatorMappingAssets(entityTypeBuilder, discriminatorColumn, table, entityRule);
                        //DiscriminatorMappings.Add(assets);
                        PostCreationActions.Add(m => ApplyDiscriminator(m, entityTypeBuilder, discriminatorColumn, table, entityRule));
                    }

                    break;
                case "TPT":
                    break;
                case "TPC":
                    break;
            }


        bool AnnotationFilter(AnnotationItem annotation) {
            if (ignoreEntityAnnotations.Contains(annotation.Key)) return false;
            if (isTphLeaf && annotation.Key.In(EfScaffoldingAnnotationNames.DbSetName,
                    EfRelationalAnnotationNames.Schema,
                    EfRelationalAnnotationNames.TableName,
                    EfRelationalAnnotationNames.Comment)) return false;
            return true;
        }

        entityTypeBuilder.Metadata.ApplyAnnotations(entityRule.Annotations, () => entityTypeBuilder.Metadata.Name, reporter,
            AnnotationFilter);

        var entity = entityTypeBuilder.Metadata;

        // process properties
        var excludedProperties = new HashSet<IMutableProperty>();
        foreach (var property in entity.GetProperties().Where(o => o.DeclaringEntityType == entity)) {
            var column = property.GetColumnNameNoDefault() ?? property.Name;
            var propertyRule = entityRuleNode.TryResolveRuleFor(column);
            if (propertyRule == null && entityRule.IncludeUnknownColumns) propertyRule = entityRuleNode.AddProperty(property, column);

            var shouldMapProperty = propertyRule?.Rule != null && propertyRule.Rule.ShouldMap();
            if (!shouldMapProperty) {
                // some property mappings are required by EF.  Check if the property is needed now and override any omission rule.

                if (table != null) {
                    var columnName = property.GetColumnNameNoDefault();
                    if (columnName.HasNonWhiteSpace()) {
                        var pks = table.PrimaryKey?.Columns.Where(o => o.Name == columnName).ToArray() ?? Array.Empty<DatabaseColumn>();
                        // Should not remove primary key properties.  The entity will not work. UNLESS the pkey is in the base type
                        if (pks.Length > 0 && !BaseHasColumn(columnName)) {
                            propertyRule?.Rule?.SetShouldMap(true);
                            shouldMapProperty = true;
                        }
                    }
                }
            }

            if (!shouldMapProperty) excludedProperties.Add(property);

            propertyRule?.MapTo(property, column);

            if (propertyRule?.Rule.Annotations.Count > 0 && propertyRule.Rule.ShouldMap()) {
                property.ApplyAnnotations(propertyRule.Rule.Annotations, () => $"{entityTypeBuilder.Metadata.Name}.{property.Name}",
                    reporter);
            }
        }

        foreach (var property in excludedProperties) RemovePropertyAndReferences(property);

        // Note, there are no Navigations yet because FKs are processed after visiting tables

        if (table.ForeignKeys.Count > 0 && ScaffoldTracker.HasOmissions) {
            // check to see if any of the foreign keys map to omitted tables. if so, nuke them.
            var fksToBeRemoved = new HashSet<DatabaseForeignKey>();
            foreach (var foreignKey in table.ForeignKeys)
                if (ScaffoldTracker.IsOmitted(foreignKey))
                    fksToBeRemoved.Add(foreignKey);

            foreach (var dbFk in fksToBeRemoved) {
                var eFk = entity.GetForeignKeys().FirstOrDefault(o => o.GetConstraintNameForTableOrView() == dbFk.Name);
                if (eFk == null) continue;
                var removed = entity.RemoveForeignKey(eFk);
                Debug.Assert(removed != null);
            }
        }

        if (!entity.GetProperties().Any() && entityRuleNode.BaseEntityRuleNode == null) {
            // remove the entire table
            ScaffoldTracker.OmitTable(table);
            modelBuilder.Model.RemoveEntityType(entityTypeBuilder.Metadata);
            reporter.WriteInformation($"RULED: Entity {entityTypeBuilder.Metadata.Name} omitted.");
            return null;
        }

        foreach (var excludedProperty in excludedProperties)
            reporter.WriteInformation($"RULED: Property {entityTypeBuilder.Metadata.Name}.{excludedProperty.Name} omitted.");

        return entityTypeBuilder;

        bool BaseHasColumn(string checkColumn) {
            // return true if the column is in the base type
            if (entityRuleNode?.BaseEntityRuleNode?.Builder != null) {
                var baseEntity = entityRuleNode.BaseEntityRuleNode.Builder;
                var baseProperty = baseEntity.Metadata.GetProperties().FirstOrDefault(o => {
                    var column = o.GetColumnNameNoDefault() ?? o.Name;
                    return column == checkColumn;
                });
                return baseProperty != null;
            } else {
                var baseProperty = entityRuleNode?.BaseEntityRuleNode?.FindPropertyByColumn(checkColumn);
                return baseProperty != null && baseProperty.Rule.ShouldMap();
            }
        }

        void RemovePropertyAndReferences(IMutableProperty p) {
#if DEBUG
            if (table != null) {
                var columnName = p.GetColumnNameNoDefault();
                var pks = table.PrimaryKey?.Columns.Where(o => o.Name == columnName).ToArray() ?? Array.Empty<DatabaseColumn>();
                Debug.Assert(pks.Length == 0 || BaseHasColumn(columnName),
                    "Should not remove primary key properties.  The entity will not work.");
            }
#endif

            RemoveIndexesWith(p);
            RemoveKeysWith(p);
            // if (!BaseHasColumn(columnName)) {
            //     // FKs should be able to work if the key is in the base type
            //     RemoveFKsWith(p, columnName);
            // }

            var removed = entity.RemoveProperty(p);
            Debug.Assert(removed != null);
        }

        void RemoveIndexesWith(IMutableProperty p) {
            foreach (var item in entity.GetIndexes()
                         .Where(o => o.Properties.Any(ip => ip == p)).ToArray()) {
                var removed = entity.RemoveIndex(item);
                Debug.Assert(removed != null);
            }
        }

        void RemoveKeysWith(IMutableProperty p) {
            foreach (var item in entity.GetKeys()
                         .Where(o => o.Properties.Any(ip => ip == p)).ToArray()) {
                var removed = entity.RemoveKey(item);
                Debug.Assert(removed != null);
            }
        }

        // void RemoveFKsWith(IMutableProperty p, string columnName) {
        //     // Note, FKs are not linked to entities until VisitForeignKeys. Must remove FKs from table instead.
        //     if (table != null && columnName.HasCharacters()) {
        //         var fks = table.ForeignKeys.Where(o => o.Columns.Any(c => c.Name == columnName)).ToArray();
        //         foreach (var fk in fks) {
        //             var removed = table.ForeignKeys.Remove(fk);
        //             Debug.Assert(removed);
        //         }
        //     }
        //
        //     // attempt entity foreign key removal anyway:
        //     foreach (var item in entity.GetForeignKeys()
        //                  .Where(o => o.Properties.Any(ip => ip == p)).ToArray()) {
        //         var removed = entity.RemoveForeignKey(item);
        //         Debug.Assert(removed != null);
        //     }
        // }
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual void ApplyDiscriminator(ModelBuilder modelBuilder, EntityTypeBuilder entityTypeBuilder,
        string discriminatorColumn, DatabaseTable table, EntityRule entityRule) {
        var column = table.Columns.FirstOrDefault(o => o.Name == discriminatorColumn);
        if (column == null) {
            reporter.WriteWarning(
                $"RULED: Entity {entityTypeBuilder.Metadata.Name} discriminator column '{discriminatorColumn}' not found.");
            return;
        }

        var property = entityTypeBuilder.Metadata.GetProperties()
            .FirstOrDefault(o => o.GetColumnNameNoDefault().EqualsIgnoreCase(discriminatorColumn));
        if (property == null) {
            reporter.WriteWarning(
                $"RULED: Entity {entityTypeBuilder.Metadata.Name} discriminator property for column '{column.Name}' not found.");
            return;
        }

        var type = property.ClrType;
        var discriminatorBuilder = entityTypeBuilder.HasDiscriminator(property.Name, type);
        var propertyRule = entityRule.Properties.FirstOrDefault(o => o.Name == column.Name);
        if (propertyRule == null) return;
        var mapping = new List<(object Value, string ToEntityName)>();
        foreach (var condition in propertyRule.DiscriminatorConditions) {
            object value;
            try {
                if (type == typeof(string))
                    value = condition.Value;
                else
                    value = Convert.ChangeType(condition.Value, type);
            } catch (Exception ex) {
                reporter.WriteWarning(
                    $"RULED: Entity {entityTypeBuilder.Metadata.Name} discriminator value '{condition.Value?.Truncate(20)}' could not be converted to {type.Name}: {ex.Message}");
                return;
            }

            var toEntity = modelBuilder.Model.FindEntityType(condition.ToEntityName);
            Debug.Assert(toEntity != null);
            if (toEntity != null) {
                mapping.Add((value, toEntity.Name));
                discriminatorBuilder.HasValue(toEntity.Name, value);
                reporter.WriteVerbose(
                    $"RULED: Entity {entityTypeBuilder.Metadata.Name} discriminator value '{value?.ToString()?.Truncate(20)}' mapped to entity {condition.ToEntityName}");
            } else
                reporter.WriteVerbose(
                    $"RULED: Entity {entityTypeBuilder.Metadata.Name} discriminator value '{value?.ToString()?.Truncate(20)}' could not be mapped to missing entity: {condition.ToEntityName}");
        }

        discriminatorBuilder.IsComplete(true);

        var config = GenerateEntityTypeAnnotations("entity", entityTypeBuilder.Metadata, mapping).ToString();
        entityTypeBuilder.Metadata.SetOrRemoveAnnotation(RulerAnnotations.DiscriminatorConfig, config);
    }

    /// <summary> Construct the discriminator configuration code. </summary>
    /// <param name="entityTypeBuilderName">entity type variable name</param>
    /// <param name="entityType">entity type</param>
    /// <param name="mapping"> value mapping </param>
    /// <param name="stringBuilder">string builder to add text to</param>
    protected virtual IndentedStringBuilder GenerateEntityTypeAnnotations(string entityTypeBuilderName, IMutableEntityType entityType,
        List<(object Value, string ToEntityName)> mapping,
        IndentedStringBuilder stringBuilder = null) {
        IAnnotation discriminatorPropertyAnnotation = null;
        IAnnotation discriminatorValueAnnotation = null;
        IAnnotation discriminatorMappingCompleteAnnotation = null;
        stringBuilder ??= new();
        code ??= new(new MockTypeMappingSource());
        foreach (var annotation in entityType.GetAnnotations()) {
            switch (annotation.Name) {
                case EfCoreAnnotationNames.DiscriminatorProperty:
                    discriminatorPropertyAnnotation = annotation;
                    break;
                case EfCoreAnnotationNames.DiscriminatorValue:
                    discriminatorValueAnnotation = annotation;
                    break;
                case EfCoreAnnotationNames.DiscriminatorMappingComplete:
                    discriminatorMappingCompleteAnnotation = annotation;
                    break;
            }
        }

        if ((discriminatorPropertyAnnotation?.Value
             ?? discriminatorMappingCompleteAnnotation?.Value
             ?? discriminatorValueAnnotation?.Value)
            == null) return stringBuilder;
        stringBuilder
            .AppendLine()
            .Append(entityTypeBuilderName)
            .Append(".")
            .Append("HasDiscriminator");

        if (discriminatorPropertyAnnotation?.Value != null) {
            var discriminatorProperty = entityType.FindProperty((string)discriminatorPropertyAnnotation.Value)!;
            var propertyClrType = FindValueConverter(discriminatorProperty)?.ProviderClrType
                                      .MakeNullable(discriminatorProperty.IsNullable)
                                  ?? discriminatorProperty.ClrType;
            stringBuilder
                .Append("<")
                .Append(code.Reference(propertyClrType))
                .Append(">(")
                .Append(code.Literal((string)discriminatorPropertyAnnotation.Value))
                .Append(")");
            if (mapping?.Count > 0) {
                foreach (var map in mapping) {
                    stringBuilder
                        .Append(".")
                        .Append("HasValue")
                        .Append("(typeof(")
                        .Append(map.ToEntityName)
                        .Append("), ")
                        .Append(code.UnknownLiteral(map.Value))
                        .Append(")");
                }
            }
        } else {
            stringBuilder
                .Append("()");
        }

        if (discriminatorMappingCompleteAnnotation?.Value != null) {
            var value = (bool)discriminatorMappingCompleteAnnotation.Value;

            stringBuilder
                .Append(".")
                .Append("IsComplete")
                .Append("(")
                .Append(code.Literal(value))
                .Append(")");
        }

        if (discriminatorValueAnnotation?.Value != null) {
            var value = discriminatorValueAnnotation.Value;
            var discriminatorProperty = entityType.FindDiscriminatorProperty();
            if (discriminatorProperty != null) {
                var valueConverter = FindValueConverter(discriminatorProperty);
                if (valueConverter != null) {
                    value = valueConverter.ConvertToProvider(value);
                }
            }

            stringBuilder
                .Append(".")
                .Append("HasValue")
                .Append("(")
                .Append(code.UnknownLiteral(value))
                .Append(")");
        }

        stringBuilder.AppendLine(";");

        return stringBuilder;

        ValueConverter FindValueConverter(IMutableProperty property)
            => property.GetValueConverter() ?? property.FindTypeMapping()?.Converter;
    }


    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual KeyBuilder VisitPrimaryKey(EntityTypeBuilder builder, DatabaseTable table, Func<KeyBuilder> baseCall) {
        if (explicitEntityRuleMapping.table == table && explicitEntityRuleMapping.entityRule?.BaseEntityRuleNode != null)
            // EF requires that a PK is defined only on the base type. what about in TPT where there is a table?
            // Also can't return null here otherwise that will kill the entity.
            // Return a random key (it wont be used for anything!)
            return randomKeyBuilder;

        var kb = baseCall();
        if (kb != null) randomKeyBuilder = kb;
        return kb;
    }

    /// <summary> Performed after scaffolding all entities, this method will visit all database FKs and convert them to entity FKs. </summary>
    protected virtual ModelBuilder VisitForeignKeys(ModelBuilder modelBuilder, IList<DatabaseForeignKey> foreignKeys,
        Func<IList<DatabaseForeignKey>, ModelBuilder> baseCall) {
        try {
            if (visitForeignKeyMethod == null || addNavigationPropertiesMethod == null) return baseCall(foreignKeys);

            ArgumentNullException.ThrowIfNull(foreignKeys);
            ArgumentNullException.ThrowIfNull(modelBuilder);

            var schemaNames = foreignKeys.Select(o => o.Table.Schema.EmptyIfNullOrWhitespace())
                .Where(o => o.HasNonWhiteSpace()).Distinct().ToArray();

            var schemas = schemaNames.Select(o => dbContextRule?.TryResolveRuleFor(o))
                .Where(o => o?.Rule?.UseManyToManyEntity == true).ToArray();

            foreignKeys = AddMissingDatabaseForeignKeys(foreignKeys);

            if (ScaffoldTracker.HasOmissions) {
                // check to see if the foreign key maps to an omitted table. if so, nuke it.
                var fksToBeRemoved = new HashSet<DatabaseForeignKey>();
                foreach (var foreignKey in foreignKeys)
                    if (ScaffoldTracker.IsOmitted(foreignKey))
                        fksToBeRemoved.Add(foreignKey);

                if (fksToBeRemoved.Count > 0) {
                    if (foreignKeys.IsReadOnly) foreignKeys = new List<DatabaseForeignKey>(foreignKeys);
                    fksToBeRemoved.ForAll(o => foreignKeys.Remove(o));
                }
            }

            if (schemas.IsNullOrEmpty()) {
                var fks = baseCall(foreignKeys);

                return fks;
            }

            foreach (var grp in foreignKeys.GroupBy(o => o.Table.Schema.EmptyIfNullOrWhitespace())) {
                var schema = grp.Key;
                var schemaForeignKeys = grp.ToArray();
                var schemaReference = schemas.FirstOrDefault(o => o?.Rule?.SchemaName == schema);
                if (schemaReference == null) {
                    modelBuilder = baseCall(schemaForeignKeys);
                    continue;
                }

                // force simple ManyToMany junctions to be generated as entities
                reporter.WriteInformation($"RULED: Simple many-to-many junctions in {schema} are being forced to generate entities.");
                foreach (var fk in schemaForeignKeys) InvokeVisitForeignKey(modelBuilder, fk);

                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                foreach (var foreignKey in entityType.GetForeignKeys())
                    InvokeAddNavigationProperties(foreignKey);
            }

            return modelBuilder;
        } finally {
            RemoveOmittedForeignKeys();
            //TestNavigations(modelBuilder);
        }
    }

    /// <summary> Performed after scaffolding all entities, this method will visit a database FK and convert it to an entity FK. </summary>
    private IMutableForeignKey VisitForeignKey(ModelBuilder modelBuilder, DatabaseForeignKey fk, Func<IMutableForeignKey> baseCall) {
#if DEBUG
        if (fk.Name.StartsWithIgnoreCase("FK_JobSessions_ToolingDefinition")) Debugger.Break();
#endif
        IMutableForeignKey newFk;
        bool isValidFk = false;
        try {
            explicitFkEntityMapping = ResolveForeignKeyEntities(fk);
            isValidFk = explicitFkEntityMapping.Dependent?.Builder != null && explicitFkEntityMapping.Principal?.Builder != null &&
                        explicitFkEntityMapping.FkRule.IsRuleValid;
            newFk = baseCall();

            if (newFk != null) {
                newFk = ValidateForeignKey(modelBuilder, fk, newFk, explicitFkEntityMapping.Dependent, explicitFkEntityMapping.Principal,
                    explicitFkEntityMapping.FkRule);
            } else {
                // FK was not created!
#if DEBUG
                reporter.WriteInformation($"Entity FK {fk.Name} was NOT created by EFCore");
#endif
            }
        } catch (ArgumentException ex) {
            // may be error related to missing FK cols that were omitted by rules.
            if (dbContextRule == null || (isValidFk && !ScaffoldTracker.IsOmitted(fk))) throw;

            reporter.WriteWarning($"EF Core failed to process FK {fk.Name} because table elements are omitted.");
            return null; // carrying on
        } finally {
            explicitFkEntityMapping = default;
        }

        return newFk;
    }

    /// <summary> Performed after scaffolding all entities, this method will validate that the database FK has been correctly
    /// converted to an entity FK.  EF Core is not expecting table splitting and inheritance in this process, so it often selects
    /// the wrong entity to link the FK to.  This method will detect and correct the entity mapping. </summary>
    private (EntityRuleNode Dependent, EntityRuleNode Principal, ForeignKeyRuleNode FkRule) ResolveForeignKeyEntities(
        DatabaseForeignKey foreignKey) {
        // if the FK was intended to be placed on a design time entity such as a split or derived type, then we may need to
        // remove and re-add the FK using the correct entity types.  load the rule and find out.
        var addedForeignKeyRule = dbContextRule.ForeignKeys.FirstOrDefault(o => o.ForeignKey == foreignKey) ??
                                  dbContextRule.ForeignKeys.GetByFinalName(foreignKey.Name);
        EntityRuleNode correctPrincipal = null;
        EntityRuleNode correctDependent = null;
        if (addedForeignKeyRule != null) {
            correctPrincipal = dbContextRule.TryResolveRuleForEntityName(addedForeignKeyRule.Rule.PrincipalEntity);
            correctDependent = dbContextRule.TryResolveRuleForEntityName(addedForeignKeyRule.Rule.DependentEntity);
            CheckRule(ref correctPrincipal, foreignKey.PrincipalTable, foreignKey.PrincipalColumns);
            CheckRule(ref correctDependent, foreignKey.Table, foreignKey.Columns);
            if (correctPrincipal != null && correctDependent != null)
                return (correctDependent, correctPrincipal, addedForeignKeyRule);
        }

        // check the ends that EFCore selected. If they have base types, and the FK props are in the base type, then the
        // FK should be remapped to the base entity instead.
        correctPrincipal ??= GetCorrectEntityRule(foreignKey.PrincipalTable, foreignKey.PrincipalColumns);
        correctDependent ??= GetCorrectEntityRule(foreignKey.Table, foreignKey.Columns);

        if (addedForeignKeyRule == null && correctPrincipal != null && correctDependent != null) {
            var foreignKeyRule = new ForeignKeyRule() {
                Name = foreignKey.Name,
                DependentEntity = correctDependent.GetFinalName(),
                PrincipalEntity = correctPrincipal.GetFinalName(),
                DependentProperties = foreignKey.Columns.Select(o => o.Name).ToArray(),
                PrincipalProperties = foreignKey.PrincipalColumns.Select(o => o.Name).ToArray(),
            };
            addedForeignKeyRule = new(foreignKeyRule, dbContextRule);
        }

        return (correctDependent, correctPrincipal, addedForeignKeyRule);

        EntityRuleNode GetCorrectEntityRule(DatabaseTable table, IList<DatabaseColumn> key) {
            var entityRules = dbContextRule.TryResolveRuleFor(table);
            if (entityRules.Count == 0) return null;
            if (entityRules.Count == 1) return entityRules.FirstOrDefault(o => o.IsAlreadyScaffolded);
            var scaffolded = entityRules.Where(o => o.IsAlreadyScaffolded).ToArray();
            if (scaffolded.Length <= 1) return scaffolded.FirstOrDefault();

            foreach (var entityRuleNode in scaffolded) {
                var result = GetCorrectEntityOrNull(entityRuleNode?.Builder?.Metadata, table, key);
                if (result != null) return entityRuleNode;
            }

            // use the base type's table since shared properties are removed from derived tables
            foreach (var entityRuleNode in scaffolded) {
                foreach (var baseType in entityRuleNode.GetBaseTypes()) {
                    var result = GetCorrectEntityOrNull(baseType?.Builder?.Metadata, baseType?.DatabaseTable, key);
                    if (result != null) return entityRuleNode; // the derived type will work, but table names may differ here.
                }
            }

            return null;
        }

        static IMutableEntityType GetCorrectEntityOrNull(IMutableEntityType entityType, DatabaseTable table,
            IList<DatabaseColumn> key) {
            if (table == null) return null;
            if (entityType?.BaseType == null) return ReturnIfHasColumns(entityType, key);
            var allBases = entityType.GetAllBaseTypes() // starting with root
                .Where(o => o.GetTableOrViewSchema().EmptyIfNullOrWhitespace() == table.Schema.EmptyIfNullOrWhitespace() &&
                            o.GetTableOrViewName() == table.Name)
                .ToArray();
            if (allBases.Length == 0) return ReturnIfHasColumns(entityType, key);
            Debug.Assert(!allBases.Contains(entityType));

            foreach (var baseType in allBases)
                if (HasColumns(baseType, key))
                    return baseType;

            return ReturnIfHasColumns(entityType, key);
        }

        static IMutableEntityType ReturnIfHasColumns(IMutableEntityType entityType, IList<DatabaseColumn> key) {
            if (entityType != null && HasColumns(entityType, key)) return entityType;
            return null;
        }

        static bool HasColumns(IMutableEntityType entityType, IList<DatabaseColumn> key) {
            var properties = entityType?.GetPropertiesFromDbColumns(key);
            var validCount = properties?.Count(o => o != null && o.DeclaringEntityType == entityType);
            return validCount == key.Count;
        }

        void CheckRule(ref EntityRuleNode rule, DatabaseTable table, IList<DatabaseColumn> key) {
            if (rule?.Builder?.Metadata == null || table == null || key.IsNullOrEmpty()) return;
            var result = GetCorrectEntityOrNull(rule.Builder.Metadata, table, key);
            if (result == null) rule = null;
        }
    }

    /// <summary> Performed after scaffolding all entities, this method will validate that the database FK has been correctly
    /// converted to an entity FK.  EF Core is not expecting table splitting and inheritance in this process, so it often selects
    /// the wrong entity to link the FK to.  This method will detect and correct the entity mapping. </summary>
    private IMutableForeignKey ValidateForeignKey(ModelBuilder modelBuilder, DatabaseForeignKey foreignKey,
        IMutableForeignKey entityForeignKey, EntityRuleNode dependent, EntityRuleNode principal, ForeignKeyRuleNode fkRule) {
        // if the FK was intended to be placed on a design time entity such as a split or derived type, then we may need to
        // remove and re-add the FK using the correct entity types.  load the rule and find out.
        if (dependent?.Builder?.Metadata == null || principal?.Builder?.Metadata == null || fkRule == null) {
            return entityForeignKey;
        }

        if (entityForeignKey.PrincipalEntityType.Name != principal.Builder.Metadata.Name ||
            entityForeignKey.DeclaringEntityType.Name != dependent.Builder.Metadata.Name) {
            entityForeignKey = RemapForeignKey(modelBuilder, foreignKey, entityForeignKey, fkRule, dependent, principal);
        }

        return entityForeignKey;
    }

    /// <summary> This method correct the entity mapping for a FK that has been incorrectly mapped by EF Core. </summary>
    private IMutableForeignKey RemapForeignKey(ModelBuilder modelBuilder,
        DatabaseForeignKey foreignKey,
        IMutableForeignKey entityForeignKey,
        ForeignKeyRuleNode addedForeignKeyRule, EntityRuleNode dependent, EntityRuleNode principal) {
        // the FK definition states that it should map to different entities than the current.
        // could be naming problem or could be table splitting/derived table issue
        // verify that entities actually exist by the names identified on the FK.  if so, the mapping is wrong!
        // if the underlying tables are equivalent in each case, then the FK wiring is correct, it just got mapped to the wrong
        // entities.  then we can remove the FK and re-add against the correct entity types.
        // var dependentEntityRule = dbContextRule.TryResolveRuleForEntityName(addedForeignKeyRule.Rule.DependentEntity);
        // var principalEntityRule = dbContextRule.TryResolveRuleForEntityName(addedForeignKeyRule.Rule.PrincipalEntity);
        // var dependentNavRule = dependentEntityRule?.GetNavigations().FirstOrDefault(o => !o.IsPrincipal && o.FkName == foreignKey.Name);
        // var principalNavRule = principalEntityRule?.GetNavigations().FirstOrDefault(o => o.IsPrincipal && o.FkName == foreignKey.Name);
        var currentPrincipal = entityForeignKey.PrincipalEntityType;
        var currentDependent = entityForeignKey.DeclaringEntityType;
        var principalEntityType =
            principal?.Builder?.Metadata ?? modelBuilder.Model.FindEntityType(addedForeignKeyRule.Rule.PrincipalEntity);
        var dependentEntityType =
            dependent?.Builder?.Metadata ?? modelBuilder.Model.FindEntityType(addedForeignKeyRule.Rule.DependentEntity);
        if (principalEntityType == null || dependentEntityType == null) {
            reporter.WriteWarning($"Unable to correctly map FK {foreignKey.Name} because the expected entities are not in the model");
            return RemoveForeignKey();
        }

        if (principalEntityType == currentPrincipal && dependentEntityType == currentDependent) {
            return entityForeignKey; // it is mapped correctly
        }

        // the targets actually exist, and dont match the current.
        var rootPrincipal = GetRootType(principalEntityType);
        var rootDependent = GetRootType(dependentEntityType);
        var tgtPrincipalTable = rootPrincipal.GetTableName();
        var tgtDependentTable = rootDependent.GetTableName();
        var curPrincipalTable = GetRootType(currentPrincipal).GetTableName();
        var curDependentTable = GetRootType(currentDependent).GetTableName();
        if (tgtPrincipalTable != curPrincipalTable || tgtDependentTable != curDependentTable) {
            reporter.WriteWarning(
                $"Unable to correctly map FK {foreignKey.Name} because the actual FK is not mapped to expected base tables");
            return entityForeignKey;
        }

        // tables are a match, which verifies that the FK wiring was good and entity selection was not.
        // we can now begin remapping the FK properties
        var dependentProperties = dependentEntityType
            .GetPropertiesFromDbColumns(foreignKey.Columns)
            .ToList()
            .AsReadOnly();

        if (dependentProperties.Any(o => o == null)) {
            reporter.WriteWarning(
                $"Unable to correctly map FK {foreignKey.Name} because dependent properties cannot be resolved on entity {dependentEntityType.Name}");
            return RemoveForeignKey();
        }

        var principalPropertiesMap = foreignKey.PrincipalColumns
            .Select(
                fc => (property: principalEntityType.GetPropertyFromDbColumn(fc.Name), column: fc)).ToList();
        if (principalPropertiesMap.Any(o => o.property == null)) {
            reporter.WriteWarning(
                $"Unable to correctly map FK {foreignKey.Name} because principal properties cannot be resolved on entity {principalEntityType.Name}");
            return RemoveForeignKey();
        }

        var principalProperties = principalPropertiesMap
            .Select(tuple => tuple.property)
            .ToList();

        var principalKey = principalEntityType.FindKey(principalProperties);
        if (principalKey == null) {
            var index = principalEntityType
                .GetIndexes()
                .FirstOrDefault(i => i.Properties.SequenceEqual(principalProperties) && i.IsUnique);
            if (index != null) {
                // ensure all principal properties are non-nullable even if the columns
                // are nullable on the database. EF's concept of a key requires this.
                var nullablePrincipalProperties =
                    principalPropertiesMap.Where(tuple => tuple.property.IsNullable).ToList();
                if (nullablePrincipalProperties.Count > 0) {
                    reporter.WriteWarning(
                        $"The principal end of the foreign key {foreignKey.Name} has nullable columns. Altering properties now...");
                    nullablePrincipalProperties.ForEach(tuple => tuple.property.IsNullable = false);
                }

                principalKey = principalEntityType.AddKey(principalProperties);
            } else {
                //var principalColumns = foreignKey.PrincipalColumns.Select(c => c.Name).ToList();
                reporter.WriteWarning($"Could not scaffold the foreign key {foreignKey.Name}.  The principal key was not found");
                return RemoveForeignKey();
            }
        }

        var empty = RemoveForeignKey();
        if (empty != null) {
            reporter.WriteWarning(
                $"Could not correctly map foreign key {foreignKey.Name}.  The incorrect mapping could not be removed from the entity");
            return empty; // could not be removed? we cannot continue
        }

        var existingForeignKey = dependentEntityType.FindForeignKey(dependentProperties, principalKey, principalEntityType);
        if (existingForeignKey is not null) {
            reporter.WriteWarning($"Could not scaffold the foreign key {foreignKey.Name}.  One with the same mapping already exists");
            return RemoveForeignKey();
        }

        var newForeignKey = dependentEntityType.AddForeignKey(
            dependentProperties, principalKey, principalEntityType);

        var dependentKey = dependentEntityType.FindKey(dependentProperties);
        var dependentIndexes = dependentEntityType.GetIndexes()
            .Where(i => i.Properties.SequenceEqual(dependentProperties));
        newForeignKey.IsUnique = dependentKey != null
                                 || dependentIndexes.Any(i => i.IsUnique);

        if (!string.IsNullOrEmpty(foreignKey.Name)
            && foreignKey.Name != newForeignKey.GetDefaultName()) {
            newForeignKey.SetConstraintName(foreignKey.Name);
        }

        InvokeAssignOnDeleteAction(foreignKey, newForeignKey);

        newForeignKey.AddAnnotations(foreignKey.GetAnnotations());

        return newForeignKey;


        IMutableEntityType GetRootType(IMutableEntityType entityType) {
            return entityType.GetAllBaseTypes().FirstOrDefault() ?? entityType;
        }

        IMutableForeignKey RemoveForeignKey() {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (addedForeignKeyRule == null || currentDependent == null) return entityForeignKey;

            var removed = currentDependent.RemoveForeignKey(entityForeignKey);
            Debug.Assert(removed == entityForeignKey);
            return removed == entityForeignKey ? null : entityForeignKey;
        }

        void InvokeAssignOnDeleteAction(DatabaseForeignKey databaseForeignKey, IMutableForeignKey mutableForeignKey) {
            assignOnDeleteActionMethod?.Invoke(null, new object[] { databaseForeignKey, mutableForeignKey });
        }
    }


    /// <summary> In order to provide navigations for design time relations, and certain inheritance and splitting scenarios,
    /// this method will create database FKs that will later be converted to entity FKs and navigations by EF Core. </summary>
    private IList<DatabaseForeignKey> AddMissingDatabaseForeignKeys(IList<DatabaseForeignKey> foreignKeys) {
        var dbModel = generatingDatabaseModel;
        if (dbModel == null && foreignKeys.Count > 0) dbModel = foreignKeys[0].Table?.Database;
        var knownFkNames = foreignKeys.Select(o => o.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var knownFksByTable = foreignKeys.GroupBy(o => o.Table).ToDictionary(o => o.Key, o => o.ToList());
        var unknownFks = dbContextRule.ForeignKeys.Where(o => !knownFkNames.Contains(o.FkName) && o.IsRuleValid)
            .ToList();
        if (dbModel == null || unknownFks.Count <= 0) return foreignKeys;

        foreach (var unknownFk in unknownFks) {
            // add foreign keys based on rules
            // note, we must generate both navigations due to expectations of the T4s when generating navigation code.
            var pEntity = dbContextRule.TryResolveRuleForEntityName(unknownFk.Rule.PrincipalEntity);
            var dEntity = dbContextRule.TryResolveRuleForEntityName(unknownFk.Rule.DependentEntity);
            if (!pEntity.IsAlreadyScaffolded || !dEntity.IsAlreadyScaffolded) continue;
            var pTable = pEntity?.DatabaseTable;
            var dTable = dEntity?.DatabaseTable;
            if (pTable == null || dTable == null) {
                // for now at least, the entity must be mapped to a table
                continue;
            }

            if (unknownFk.Rule.Name.IsNullOrWhiteSpace())
                unknownFk.Rule.Name = $"FK_Ruled_{dEntity.GetFinalName()}_{pEntity.GetFinalName()}";

            var dbFk = new DatabaseForeignKey {
                PrincipalTable = pTable,
                Name = unknownFk.Rule.Name,
                OnDelete = ReferentialAction.NoAction,
                Table = dTable,
            };
            dbFk.PrincipalColumns.AddRange(pTable.Table.GetTableColumns(unknownFk.Rule?.PrincipalProperties));
            dbFk.Columns.AddRange(dTable.Table.GetTableColumns(unknownFk.Rule?.DependentProperties));
            if (dbFk.Name.IsNullOrWhiteSpace() || dbFk.PrincipalColumns.IsNullOrEmpty() ||
                dbFk.Columns.Count != dbFk.PrincipalColumns.Count) {
                reporter.WriteWarning(
                    $"RULED: Skipping custom FK {dbFk.Name} because it does not form a valid constraint.");
                continue;
            }

            // validate that there is not already a FK with the same columns
            if (!IsUnique(dbFk, knownFksByTable)) {
                reporter.WriteWarning(
                    $"RULED: Skipping custom FK {dbFk.Name} because it shares columns with an existing constraint.");
                continue;
            }

            if (dbFk.Table.PrimaryKey == null || dbFk.Table.PrimaryKey.Columns.IsNullOrEmpty()) {
                // Primary end navigation requires the primary key.
                reporter.WriteWarning(
                    $"RULED: Skipping custom FK {dbFk.Name} because the declaring table {dbFk.Table.GetFullName()} is keyless.");
                continue;
            }

            var fksByTbl = knownFksByTable.GetOrAddNew(dbFk.Table, o => new());
            fksByTbl.Add(dbFk);
            if (foreignKeys.IsReadOnly) foreignKeys = new List<DatabaseForeignKey>(foreignKeys);
            foreignKeys.Add(dbFk);
            unknownFk.MapTo(dbFk);
            reporter.WriteInformation(
                $"RULED: Adding custom FK {dbFk.Name} between {pEntity.GetFinalName()} and {dEntity.GetFinalName()}.");

            // Watch the following issue for necessary modification to this code to ensure these navs are code-only:
            // https://github.com/dotnet/efcore/issues/15854
        }

        return foreignKeys;


        bool AreColumnsEqual(DatabaseForeignKey a, DatabaseForeignKey b) {
            if (a.Columns.Count != b.Columns.Count) return false;
            for (var i = 0; i < a.Columns.Count; i++) {
                if (b.Columns[i] != a.Columns[i]) return false;
            }

            if (a.PrincipalColumns.Count != b.PrincipalColumns.Count) return false;
            for (var i = 0; i < a.PrincipalColumns.Count; i++) {
                if (b.PrincipalColumns[i] != a.PrincipalColumns[i]) return false;
            }

            return true;
        }

        bool IsUnique(DatabaseForeignKey dbForeignKey,
            Dictionary<DatabaseTable, List<DatabaseForeignKey>> knownFksByTable) {
            var fksByTbl = knownFksByTable.TryGetValue(dbForeignKey.Table);
            if (!(fksByTbl?.Count > 0)) return true;
            foreach (var foreignKey in fksByTbl) {
                // check for matching columns
                if (AreColumnsEqual(foreignKey, dbForeignKey)) return false;
            }

            return true;
        }
    }

    private bool TryAddTableKey(DatabaseTable table, ICollection<EntityRuleNode> entityRuleNodes) {
        if (entityRuleNodes is null || entityRuleNodes.Count == 0) return false;
        if (table is not DatabaseView view) return false;
        var keyedEntity = entityRuleNodes
                              .FirstOrDefault(e =>
                                  e.Rule.ShouldMap() && e.GetProperties().Any(o => o.Rule.IsKey && o.Parent.DbName == view.Name))
                          ?? entityRuleNodes
                              .FirstOrDefault(e => e.GetProperties().Any(o => o.Rule.IsKey && o.Parent.DbName == view.Name));
        if (keyedEntity == null) return false;
        // EF Core does not generate keys for views. But we might have it in the rules.
        var keyColNames = keyedEntity.GetProperties()
            .Where(o => o.Rule.IsKey && o.Parent.DbName == view.Name)
            .Select(o => o.Rule.Name).ToArray();
        var keyCols = view.GetTableColumns(keyColNames);
        if (keyCols.IsNullOrEmpty()) return false;

        foreach (var col in keyCols) {
            // All properties on which a key is declared must be marked as non-nullable/required
            if (col.IsNullable) col.IsNullable = false;
        }

        // we can create a key on the view
        view.PrimaryKey = new() {
            Name = $"PK_Ruled_{keyedEntity.GetFinalName()}",
            Table = view,
        };
        view.PrimaryKey.Columns.AddRange(keyCols);
        reporter.WriteInformation(
            $"RULED: Adding key {view.PrimaryKey.Name} to table {table.GetFullName()} for navigation support.");
        return true;
    }

    private bool TryAddEntityKey(IMutableForeignKey foreignKey) {
        if (foreignKey.DependentToPrincipal == null || foreignKey.PrincipalToDependent != null ||
            !foreignKey.DeclaringEntityType.IsKeyless) return false;
        return TryAddEntityKey(foreignKey.DeclaringEntityType);
    }

    private bool TryAddEntityKey(IMutableEntityType e) {
        return TryAddEntityKey(e, dbContextRule.TryResolveRuleForEntityName(e.Name));
    }

    private bool TryAddEntityKey(IMutableEntityType e, EntityRuleNode entityRuleNode) {
        if (entityRuleNode == null) return false;
        var table = entityRuleNode.DatabaseTable?.Table;
        if (table is not DatabaseView view || !(table?.PrimaryKey?.Columns.Count > 0)) return false;

        // Even though we applied a key to the view, EF does not apply it to the entity. Attempt to do so now.
        var props = e.GetPropertiesFromDbColumns(table.PrimaryKey.Columns);
        if (props.Length <= 0 || props.Any(o => o == null)) return false;

        e.IsKeyless = false;
        e.SetPrimaryKey(props);
        if (reporter.MinimumLevel >= LogType.Verbose)
            reporter.WriteVerbose(
                $"RULED: Adding primary key to entity {e.Name}: {props.Select(o => o.Name).Join()}");
        return true;
    }


    private void RemoveOmittedForeignKeys() {
        // remove the omitted foreign keys now
        foreach (var foreignKey in ScaffoldTracker.GetOmittedForeignKeys()) {
            var name = foreignKey.GetConstraintNameForTableOrView();
            RemoveNavigationFromEntity(foreignKey.PrincipalToDependent);
            RemoveNavigationFromEntity(foreignKey.DependentToPrincipal);
            var dRemoved = foreignKey.DeclaringEntityType.RemoveForeignKey(foreignKey);
            Debug.Assert(dRemoved != null);
            if (name.HasNonWhiteSpace())
                reporter.WriteInformation($"RULED: Foreign key {name} omitted.");

            void RemoveNavigationFromEntity(IMutableNavigation nav) {
                if (nav?.DeclaringEntityType is not EntityType et) return;
                var removed = et.RemoveNavigation(nav.Name);
                Debug.Assert(removed != null);
            }
        }
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual IMutableForeignKey InvokeVisitForeignKey(ModelBuilder modelBuilder, DatabaseForeignKey fk) {
        return (IMutableForeignKey)visitForeignKeyMethod!.Invoke(proxy, new object[] { modelBuilder, fk });
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual void InvokeAddNavigationProperties(IMutableForeignKey foreignKey) {
        addNavigationPropertiesMethod!.Invoke(proxy, new object[] { foreignKey });
    }

    /// <summary> Performed after scaffolding all entities, and after visiting database FKs and converting them to entity FKs,
    /// this method will configure the navigations to be used with the given entity FK. </summary>
    protected virtual void AddNavigationProperties(IMutableForeignKey foreignKey, Action<IMutableForeignKey> baseCall) {
        dbContextRule ??= designTimeRuleLoader?.GetDbContextRules() ?? new DbContextRuleNode(DbContextRule.DefaultNoRulesFoundBehavior);

        baseCall(foreignKey);
        var fkName = foreignKey.GetConstraintNameForTableOrView();
        var isManyToMany = foreignKey.IsManyToMany();

        if (foreignKey.DependentToPrincipal == null || foreignKey.PrincipalToDependent == null) {
            // technically, EF supports a single ended navigation (no inverse) but the T4s are built to iterate FKs
            // and generate navigations for both ends of the relation.  For this reason, if we omit one navigation then
            // Null-Ref errors will occur because the T4 code expects both ends to be set at all times.
            // We can mandate changes to the T4s such that each end is checked for null first, but for simplicity sake,
            // if one end is not defined, we will eliminate the entire FK.
            // Note, the principal end may not be defined when foreignKey.DeclaringEntityType.IsKeyless.
            ScaffoldTracker.Omit(foreignKey);
        } else {
            var dependentExcluded = ApplyNavRule(foreignKey.DependentToPrincipal, foreignKey.DeclaringEntityType, false);
            var principalExcluded = ApplyNavRule(foreignKey.PrincipalToDependent, foreignKey.PrincipalEntityType, true);

            if (dependentExcluded && principalExcluded) {
                // we will only exclude a navigation if BOTH ends are excluded, thus, removing the FK altogether.
                // see reasoning above
                ScaffoldTracker.Omit(foreignKey);
            } else {
#if DEBUG
                var dNavs = foreignKey.DeclaringEntityType.GetNavigations().ToList();
                var pNavs = foreignKey.PrincipalEntityType.GetNavigations().ToList();
                var dNav = dNavs.FirstOrDefault(o => o.ForeignKey == foreignKey);
                var pNav = pNavs.FirstOrDefault(o => o.ForeignKey == foreignKey);
                Debug.Assert(dNav != null && pNav != null);
#endif
            }
        }

        bool ApplyNavRule(IMutableNavigation navigation, IMutableEntityType entityType, bool thisIsPrincipal) {
            var entityRule = dbContextRule.TryResolveRuleForEntityName(entityType.Name);
            var navigationRule = entityRule?.TryResolveNavigationRuleFor(fkName,
                () => entityType.Name,
                thisIsPrincipal,
                isManyToMany);

            //var fkDefinition = dbContextRule.ForeignKeys.GetByFinalName(fkName);

            var navEntity = thisIsPrincipal ? foreignKey.PrincipalEntityType : foreignKey.DeclaringEntityType;
            Debug.Assert(navEntity == entityType);
#if DEBUG
            if (navigation.Name == "LatheSurfaceDefinition") Debugger.Break();
#endif
            if (navigationRule?.Rule != null) {
                // validate name.  pluralizer often changes custom names
                var newName = navigationRule.Rule.NewName.NullIfEmpty()?.Trim();
                if (newName != null && newName != navigation.Name) {
                    /* Beware, the following error may occur here: The property or navigation 'NewName' cannot be added to the
                     entity type 'TypeName' because a property or navigation with the same name already exists */
                    var oldName = navigation.Name;
                    if (thisIsPrincipal) {
                        var existingNav = navEntity.GetNavigations().FirstOrDefault(o => o.Name == newName);
                        if (existingNav == null) {
                            foreignKey.SetPrincipalToDependent((MemberInfo)null);
                            navigation = foreignKey.SetPrincipalToDependent(newName);
                            LogNameChange();
                        }
                    } else {
                        var existingNav = navEntity.GetNavigations().FirstOrDefault(o => o.Name == newName);
                        if (existingNav == null) {
                            foreignKey.SetDependentToPrincipal((MemberInfo)null);
                            navigation = foreignKey.SetDependentToPrincipal(newName);
                            LogNameChange();
                        }
                    }

                    void LogNameChange() =>
                        reporter.WriteVerbose(
                            $"RULED: Corrected navigation {navEntity.Name}.{oldName} name to '{newName}'");
                }
            }

            if (entityRule != null && navigationRule == null && entityRule.Rule.IncludeUnknownColumns)
                navigationRule = entityRule.AddNavigation(navigation, fkName, thisIsPrincipal, isManyToMany);

            navigationRule?.MapTo(navigation, fkName, thisIsPrincipal, isManyToMany);

            navigation.ApplyAnnotations(navigationRule?.Rule?.Annotations, () => $"{entityType.Name}.{navigation?.Name}", reporter);

            // exclude this navigation (when rule is null or explicitly not mapped)
            var excluded = navigationRule?.Rule?.ShouldMap() != true;

            // Exception #1: if the navigation is a part of a required mapping back to a base type in a TPT hierarchy
            // if (navigationRule != null && navigationRule.Parent != null) {
            //     var hierarchyRoot = navigationRule.Parent.GetBaseTypes(true)
            //         .FirstOrDefault(o => o.Rule.GetMappingStrategy() != null);
            //     if (hierarchyRoot?.Builder != null) {
            //         var strategy = hierarchyRoot.Rule.GetMappingStrategy();
            //         if (strategy.In("TPT") && hierarchyRoot.Builder.Metadata != entityType) {
            //             // if the navigation is specifically between the base type and leaf, then it must be included
            //             if (navigation.Inverse.DeclaringEntityType.Name == hierarchyRoot.Builder.Metadata.Name) {
            //                 excluded = false;
            //                 navigationRule.Rule.SetShouldMap(true);
            //             }
            //         }
            //     }
            // }

            return excluded;
        }
    }


    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual string GetEntityTypeName(DatabaseTable table) {
        // Note, we lack the context necessary to know which exact entity we are targeting since tables and entities are not 1:1
        // Use locally set explicit context values in order to pinpoint the correct selections
        if (table != null) {
            if (explicitEntityRuleMapping.table == table && explicitEntityRuleMapping.entityRule?.Rule.NewName.HasNonWhiteSpace() == true)
                return explicitEntityRuleMapping.entityRule.Rule.NewName;

            // this is the mechanism by which EF Core locates the correct entity endpoint for a FK. Use context to eliminate guess work:
            if (explicitFkEntityMapping.Dependent?.DatabaseTable?.Table == table) {
                if (explicitFkEntityMapping.Dependent.Builder != null) return explicitFkEntityMapping.Dependent.Builder.Metadata.Name;
                if (explicitFkEntityMapping.Dependent.Rule.NewName.HasNonWhiteSpace())
                    return explicitFkEntityMapping.Dependent.Rule.NewName;
            }

            if (explicitFkEntityMapping.Principal?.DatabaseTable?.Table == table) {
                if (explicitFkEntityMapping.Principal.Builder != null) return explicitFkEntityMapping.Principal.Builder.Metadata.Name;
                if (explicitFkEntityMapping.Principal.Rule.NewName.HasNonWhiteSpace())
                    return explicitFkEntityMapping.Principal.Rule.NewName;
            }
        }

        return tableNamer.GetName(table);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual string GetDbSetName(DatabaseTable table) {
        if (explicitEntityRuleMapping.table == table && explicitEntityRuleMapping.entityRule?.Rule.NewName.HasNonWhiteSpace() == true) {
            var name = explicitEntityRuleMapping.entityRule.Rule.NewName;
            name = options?.NoPluralize == true ? name : pluralizer.Pluralize(name);
            return name;
        }

        return dbSetNamer.GetName(table);
    }

    void IInterceptor.Intercept(IInvocation invocation) {
        switch (invocation.Method.Name) {
            case "GetTypeScaffoldingInfo" when invocation.Arguments.Length == 1 && invocation.Arguments[0] is DatabaseColumn dc: {
                TypeScaffoldingInfo BaseCall() {
                    invocation.Proceed();
                    return (TypeScaffoldingInfo)invocation.ReturnValue;
                }

                var response = GetTypeScaffoldingInfo(dc, BaseCall);
                invocation.ReturnValue = response;
                break;
            }
            case "VisitDatabaseModel" when invocation.Arguments.Length == 2 && invocation.Arguments[0] is ModelBuilder mb &&
                                           invocation.Arguments[1] is DatabaseModel dbm: {
                ModelBuilder BaseCall() {
                    invocation.Proceed();
                    return (ModelBuilder)invocation.ReturnValue;
                }

                var response = VisitDatabaseModel(mb, dbm, BaseCall);
                invocation.ReturnValue = response;
                break;
            }
            case "VisitTables" when invocation.Arguments.Length == 2 && invocation.Arguments[0] is ModelBuilder mb &&
                                    invocation.Arguments[1] is ICollection<DatabaseTable> dt: {
                // ModelBuilder BaseCall(ModelBuilder modelBuilder, ICollection<DatabaseTable> databaseTables) {
                //     invocation.Proceed();
                //     return (ModelBuilder)invocation.ReturnValue;
                // }

                var response = VisitTables(mb, dt);
                invocation.ReturnValue = response;
                break;
            }
            case "VisitTable" when invocation.Arguments.Length == 2 && invocation.Arguments[0] is ModelBuilder mb &&
                                   invocation.Arguments[1] is DatabaseTable dt: {
                EntityTypeBuilder BaseCall() {
                    invocation.Proceed();
                    return (EntityTypeBuilder)invocation.ReturnValue;
                }

                var response = VisitTable(mb, dt, BaseCall);
                invocation.ReturnValue = response;
                break;
            }
            case "VisitPrimaryKey" when invocation.Arguments.Length == 2 &&
                                        invocation.Arguments[0] is EntityTypeBuilder entityTypeBuilder &&
                                        invocation.Arguments[1] is DatabaseTable table: {
                KeyBuilder BaseCall() {
                    invocation.Proceed();
                    return (KeyBuilder)invocation.ReturnValue;
                }

                var response = VisitPrimaryKey(entityTypeBuilder, table, BaseCall);
                invocation.ReturnValue = response;
                break;
            }
            case "VisitForeignKeys" when invocation.Arguments.Length == 2 && invocation.Arguments[0] is ModelBuilder mb &&
                                         invocation.Arguments[1] is IList<DatabaseForeignKey> fks: {
                ModelBuilder BaseCall(IList<DatabaseForeignKey> databaseForeignKeys) {
                    invocation.SetArgumentValue(1, databaseForeignKeys);
                    invocation.Proceed();
                    return (ModelBuilder)invocation.ReturnValue;
                }

                var response = VisitForeignKeys(mb, fks, BaseCall);
                invocation.ReturnValue = response;
                break;
            }
            case "VisitForeignKey" when invocation.Arguments.Length == 2 && invocation.Arguments[0] is ModelBuilder mb &&
                                        invocation.Arguments[1] is DatabaseForeignKey fk: {
                IMutableForeignKey BaseCall() {
                    //invocation.SetArgumentValue(1, databaseForeignKeys);
                    invocation.Proceed();
                    return (IMutableForeignKey)invocation.ReturnValue;
                }

                var response = VisitForeignKey(mb, fk, BaseCall);
                invocation.ReturnValue = response;
                break;
            }
            case "Create" when invocation.Arguments.Length == 2 && invocation.Arguments[0] is DatabaseModel dm &&
                               invocation.Arguments[1] is ModelReverseEngineerOptions op: {
                IModel BaseCall(DatabaseModel databaseModel, ModelReverseEngineerOptions options2) {
                    invocation.SetArgumentValue(0, databaseModel);
                    invocation.SetArgumentValue(1, options2);
                    invocation.Proceed();
                    return (IModel)invocation.ReturnValue;
                }

                var response = Create(dm, op, BaseCall);
                invocation.ReturnValue = response;
                break;
            }
            case "GetEntityTypeName" when invocation.Arguments.Length == 1 && invocation.Arguments[0] is DatabaseTable t: {
                var response = GetEntityTypeName(t);
                invocation.ReturnValue = response;
                break;
            }
            case "GetDbSetName" when invocation.Arguments.Length == 1 && invocation.Arguments[0] is DatabaseTable t: {
                var response = GetDbSetName(t);
                invocation.ReturnValue = response;
                break;
            }
            case "AddNavigationProperties" when invocation.Arguments.Length == 1 && invocation.Arguments[0] is IMutableForeignKey fk: {
                void BaseCall(IMutableForeignKey fk1) {
                    invocation.SetArgumentValue(0, fk1);
                    invocation.Proceed();
                }

                AddNavigationProperties(fk, BaseCall);
                break;
            }
            default:
                invocation.Proceed();
                break;
        }
    }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class MockTypeMappingSource : ITypeMappingSource {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public CoreTypeMapping FindMapping(IProperty property) => throw new NotSupportedException();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public CoreTypeMapping FindMapping(MemberInfo member) => throw new NotSupportedException();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public CoreTypeMapping FindMapping(Type type) => throw new NotSupportedException();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public CoreTypeMapping FindMapping(Type type, IModel model) => throw new NotSupportedException();
}
﻿using System.Diagnostics;
using System.Reflection.Metadata;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Common.Annotations;
using EntityFrameworkRuler.Generator.EdmxModel;
using EntityFrameworkRuler.Generator.Services;
using EntityFrameworkRuler.Rules;
using EntityFrameworkRuler.Saver;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Generator;

/// <summary> Generate rules from an EDMX such that they can be applied to a Reverse Engineered Entity Framework model to achieve the same structure as in the EDMX. </summary>
public sealed class RuleGenerator : RuleHandler, IRuleGenerator {
    /// <summary> Create rule generator for deriving entity structure rules from an EDMX </summary>
    public RuleGenerator() : this(null, null, null, null) { }

    /// <summary> Create rule generator for deriving entity structure rules from an EDMX </summary>
    /// <param name="namingService"> Service that decides how to name navigation properties.  Similar to EF ICandidateNamingService but this one utilizes the EDMX model only. </param>
    /// <param name="edmxParser"> Service that parses an EDMX file into an object model usable for rule generation. </param>
    /// <param name="ruleSaver"> Service that can persist a rule model to disk </param>
    /// <param name="logger"> Response logger </param>
    [ActivatorUtilitiesConstructor]
    public RuleGenerator(IRulerNamingService namingService, IEdmxParser edmxParser, IRuleSaver ruleSaver, IMessageLogger logger) :
        base(logger) {
        NamingService = namingService;
        EdmxParser = edmxParser;
        RuleSaver = ruleSaver;
    }

    #region properties

    /// <summary>
    /// Service that decides how to name navigation properties.
    /// Similar to EF ICandidateNamingService but this one utilizes the EDMX model only.
    /// </summary>
    public IRulerNamingService NamingService { get; set; }

    /// <summary> Service that parses an EDMX file into an object model usable for rule generation. </summary>
    public IEdmxParser EdmxParser { get; set; }

    /// <summary> Rule Saver </summary>
    public IRuleSaver RuleSaver { get; }

    #endregion

    /// <inheritdoc />
    public Task<SaveRulesResponse> SaveRules(IRuleModelRoot rule, string projectBasePath, string dbContextRulesFile = null) {
        return SaveRules(new SaveOptions(projectBasePath: projectBasePath, dbContextRulesFile: dbContextRulesFile, rules: rule));
    }

    /// <inheritdoc />
    public Task<SaveRulesResponse> SaveRules(string projectBasePath, string dbContextRulesFile, IRuleModelRoot rule,
        params IRuleModelRoot[] rules) {
        var options = new SaveOptions(projectBasePath: projectBasePath, dbContextRulesFile: dbContextRulesFile);
        if (rule != null) options.Rules.Add(rule);
        if (rules != null) options.Rules.AddRange(rules);
        return SaveRules(options);
    }

    /// <inheritdoc />
    public Task<SaveRulesResponse> SaveRules(SaveOptions request) {
        var saver = RuleSaver ?? new RuleSaver();
        return saver.SaveRules(request);
    }

    /// <inheritdoc />
    public Task<GenerateRulesResponse> GenerateRulesAsync(GeneratorOptions request) {
        return Task.Factory.StartNew(() => GenerateRules(request));
    }

    /// <inheritdoc />
    public GenerateRulesResponse GenerateRules(string edmxFilePath, bool useDatabaseNames = false, bool noPluralize = false,
        bool includeUnknowns = false, bool includeUnknownColumns = true) {
        return GenerateRules(new(edmxFilePath, useDatabaseNames, noPluralize, includeUnknowns, includeUnknownColumns));
    }

    /// <inheritdoc />
    public GenerateRulesResponse GenerateRules(GeneratorOptions request) {
        var response = new GenerateRulesResponse(Logger);
        response.Log += OnResponseLog;
        try {
            var edmxFilePath = request?.EdmxFilePath;
            try {
                var parser = EdmxParser ?? new EdmxParser();
                response.EdmxParsed ??= parser.Parse(edmxFilePath, response.Logger);
            } catch (Exception ex) {
                response.GetInternals().LogError($"Error parsing EDMX: {ex.Message}");
                return response;
            }

            GenerateAndAdd(parsed => GetDbContextRules(parsed, request));

            return response;
        } finally {
            response.Log -= OnResponseLog;
        }

        void GenerateAndAdd<T>(Func<EdmxParsed, T> gen) where T : class, IRuleModelRoot {
            try {
                var rulesRoot = gen(response.EdmxParsed);
                response.Add(rulesRoot);
            } catch (Exception ex) {
                response.GetInternals().LogError($"Error generating output for {typeof(T)}: {ex.Message}");
            }
        }
    }


    #region Main rule gen methods

    private DbContextRule GetDbContextRules(EdmxParsed edmx, GeneratorOptions request) {
        if (edmx?.Entities.IsNullOrEmpty() != false) {
            var noRulesFoundBehavior = DbContextRule.DefaultNoRulesFoundBehavior;
            noRulesFoundBehavior.Name = edmx?.ContextName;
            return noRulesFoundBehavior;
        }

        var root = new DbContextRule {
            Name = edmx.ContextName,
            IncludeUnknownSchemas = request.IncludeUnknownSchemasAndTables
        };
        var namingService = NamingService ??= new RulerNamingService(null, request);

        foreach (var grp in edmx.Entities.GroupBy(o => o.DbSchema)) {
            if (grp.Key.IsNullOrWhiteSpace()) continue;

            var schemaRule = new SchemaRule();
            root.Schemas.Add(schemaRule);
            schemaRule.SchemaName = grp.Key;
            schemaRule.UseSchemaName = false; // will append schema name to entity name
            schemaRule.IncludeUnknownTables = request.IncludeUnknownSchemasAndTables;
            schemaRule.IncludeUnknownViews = request.IncludeUnknownSchemasAndTables;
            foreach (var entity in grp.OrderBy(o => o.Name)) {
                // if entity name is different than db, it has to go into output
                //var altered = false;
                // Get the expected EF entity identifier based on options.. just like EF would:
                var expectedClassName = namingService.GetExpectedEntityTypeName(entity);
                var entityRule = new EntityRule {
                    Name = entity.StorageName,
                    EntityName = entity.StorageName == expectedClassName ? null : expectedClassName,
                    NewName = entity.Name.CoalesceWhiteSpace(expectedClassName, entity.StorageName),
                    IncludeUnknownColumns = request.IncludeUnknownColumns,
                    BaseTypeName = entity.BaseType?.Name,
                };

                if (entity.IsAbstract) entityRule.IsAbstract(true);
                var relationalMappingStrategy = entity.RelationalMappingStrategy;
                if (relationalMappingStrategy?.Length == 3) {
                    entityRule.SetMappingStrategy(relationalMappingStrategy);
                }

                var comment = entity.ConceptualEntity?.Documentation?.GetComment();
                if (comment.HasNonWhiteSpace()) entityRule.SetComment(comment);

                Debug.Assert(entityRule.EntityName?.IsValidSymbolName() != false);
                Debug.Assert(entityRule.NewName.IsValidSymbolName());

                var isView = entity.StorageEntitySet?.Type?.StartsWithIgnoreCase("View") == true;

                foreach (var property in entity.Properties) {
                    // if property name is different than db, it has to go into output
                    // Get the expected EF property identifier based on options.. just like EF would:
                    var expectedPropertyName = namingService.GetExpectedPropertyName(property, expectedClassName);
                    var propertyRule = new PropertyRule() {
                        Name = property.ColumnName,
                        PropertyName = expectedPropertyName == property.ColumnName ? null : expectedPropertyName,
                        NewName = property.ConceptualName == expectedPropertyName ? null : property.ConceptualName,
                        NewType = (property.EnumType?.ExternalTypeName ?? property.EnumType?.FullName).ToFriendlyTypeName()
                    };

                    if (!property.IsMapped || property.ColumnName == null) {
                        propertyRule.NotMapped = true;
                        if (property.IsStorageKey || property.IsConceptualKey) {
                            propertyRule.IsKey = true;
                            if (property.ColumnName != null && (entity.BaseType == null || relationalMappingStrategy != "TPH")) {
                                // this property should be included on the type because it's a primary key
                                propertyRule.NotMapped = false;
                            }
                        }
                    }

                    if (isView) {
                        // EF Core scaffolding does not infer primary keys on views, meaning navigations are not possible.
                        // Identify the key here so that it can be applied to the reverse engineered model later.
                        propertyRule.IsKey = property.IsStorageKey || property.IsConceptualKey;
                    }

                    var storeGenPattern = property.ConceptualProperty?.GetStoreGeneratedPattern() ?? EfrStoreGeneratedPattern.None;
                    if (storeGenPattern == EfrStoreGeneratedPattern.None)
                        storeGenPattern = property.StorageProperty?.GetStoreGeneratedPattern() ?? EfrStoreGeneratedPattern.None;

                    if (storeGenPattern != EfrStoreGeneratedPattern.None && !property.IsStorageKey) {
                        // if its a storage key, EF Core scaffolding will detect it anyway. otherwise, the store gen pattern should be noted.
                        if (storeGenPattern == EfrStoreGeneratedPattern.Computed) {
                            // A value is generated on both insert and update.
                            propertyRule.SetOrRemoveAnnotation(RulerAnnotations.ComputedGenerationPattern, true);
                        } else if (storeGenPattern == EfrStoreGeneratedPattern.Identity) {
                            // A value is generated on insert and remains unchanged on update.
                            propertyRule.SetOrRemoveAnnotation(RulerAnnotations.IdentityGenerationPattern, true);
                        }
                    }

                    comment = property.ConceptualProperty?.Documentation?.GetComment();
                    if (comment.HasNonWhiteSpace()) propertyRule.SetComment(comment);

                    Debug.Assert(propertyRule.PropertyName == null || propertyRule.PropertyName.IsValidSymbolName());
                    Debug.Assert(propertyRule.NewName == null || propertyRule.NewName.IsValidSymbolName());
                    entityRule.Properties.Add(propertyRule);
                }

                if (relationalMappingStrategy == "TPH" && entity.DiscriminatorPropertyMappings.Count > 0) {
                    foreach (var discriminatorPropertyMapping in entity.DiscriminatorPropertyMappings) {
                        var property = discriminatorPropertyMapping.Property;
                        if (property.ColumnName.IsNullOrWhiteSpace()) continue;
                        var propertyRule = entityRule.Properties.FirstOrDefault(o => o.Name == property.ColumnName);
                        if (propertyRule == null) {
                            // Must add property rule for the column even though it's not mapped to a property in the EDMX
                            var expectedPropertyName = namingService.GetExpectedPropertyName(property, expectedClassName);
                            propertyRule = new() {
                                Name = property.ColumnName,
                                PropertyName = expectedPropertyName == property.ColumnName ? null : expectedPropertyName,
                                NotMapped = true
                            };
                            Debug.Assert(propertyRule.PropertyName == null || propertyRule.PropertyName.IsValidSymbolName());
                            Debug.Assert(propertyRule.NewName == null || propertyRule.NewName.IsValidSymbolName());
                            entityRule.Properties.Add(propertyRule);
                        }

                        // with discriminators for this property, set the value-to-entity mapping annotations
                        entityRule.SetDiscriminatorColumn(property.ColumnName);

                        var toEntity = discriminatorPropertyMapping.ToEntity;
                        var expectedToClassName = namingService.GetExpectedEntityTypeName(toEntity);
                        var c = new DiscriminatorCondition() {
                            Value = discriminatorPropertyMapping.Condition.Value,
                            ToEntityName = toEntity.Name.CoalesceWhiteSpace(expectedToClassName, toEntity.StorageName)
                        };
                        propertyRule.DiscriminatorConditions.Add(c);
                    }
                }

                foreach (var navigation in entity.NavigationProperties) {
                    var navigationRule = new NavigationRule {
                        NewName = navigation.ConceptualName,
                        Name = namingService.FindCandidateNavigationNames(navigation)
                            .FirstOrDefault(o => o != navigation.ConceptualName)
                    };

                    //if (!generateAll && navigationRename.Name.IsNullOrWhiteSpace()) continue;

                    // fill in other metadata
                    var inverseNav = navigation.InverseNavigation;
                    var inverseEntity = inverseNav?.Entity;
                    navigationRule.FkName = navigation.Association?.Name ?? navigation.ConceptualAssociation?.Name;
                    navigationRule.Multiplicity = navigation.Multiplicity.ToMultiplicityString();
                    navigationRule.ToEntity =
                        inverseEntity?.ConceptualEntity?.Name ?? inverseEntity?.StorageNameIdentifier ??
                        navigation.ToRole?.Entity?.Name ?? navigation.ToRoleName;
                    navigationRule.IsPrincipal = navigation.IsPrincipalEnd;

                    comment = navigation.ConceptualNavigationProperty?.Documentation?.GetComment();
                    if (comment.HasNonWhiteSpace()) navigationRule.SetComment(comment);

#if DEBUG
                    if (Debugger.IsAttached && navigationRule.ToEntity.IsNullOrWhiteSpace())
                        Debugger.Break();
                    if (Debugger.IsAttached && navigationRule.FkName.IsNullOrWhiteSpace()
                                            && (navigation.Multiplicity != Multiplicity.Many ||
                                                navigation.InverseNavigation?.Multiplicity != Multiplicity.Many))
                        Debugger.Break();
#endif
                    entityRule.Navigations.Add(navigationRule);

                    if (navigation.Association is FkAssociation fkAssociation &&
                        fkAssociation.ReferentialConstraint != null &&
                        fkAssociation.Name.HasNonWhiteSpace() &&
                        fkAssociation.ReferentialConstraint.StorageAssociation == null &&
                        !fkAssociation.ReferentialConstraint.PrincipalProperties.IsNullOrEmpty() &&
                        !fkAssociation.ReferentialConstraint.DependentProperties.IsNullOrEmpty() &&
                        root.ForeignKeys.All(fk => fk.Name != fkAssociation.Name)
                       ) {
                        // FK association with no storage constraint (created via EDMX designer). Add to json so that the column mapping
                        // is available during scaffolding
                        var name = fkAssociation.Name;
                        var constraint = fkAssociation.ReferentialConstraint;
                        var fkr = new ForeignKeyRule() {
                            Name = name,
                            PrincipalEntity = constraint.PrincipalProperties[0].EntityName,
                            DependentEntity = constraint.DependentProperties[0].EntityName,
                            PrincipalProperties = constraint.PrincipalProperties.Select(pp => pp.ColumnName ?? pp.ConceptualName).ToArray(),
                            DependentProperties = constraint.DependentProperties.Select(pp => pp.ColumnName ?? pp.ConceptualName).ToArray(),
                        };
                        root.ForeignKeys.Add(fkr);
                    }
                }

                schemaRule.Entities.Add(entityRule);
            }
        }

        foreach (var function in edmx.Functions.OrderBy(o => o.Name)) {
            var schemaRule = root.Schemas.FirstOrDefault(o => string.Equals(o.SchemaName, function.Schema, StringComparison.OrdinalIgnoreCase));
            if (schemaRule == null) {
                schemaRule = new();
                schemaRule.SchemaName = function.Schema;
                schemaRule.UseSchemaName = false; // will append schema name to entity name
                root.Schemas.Add(schemaRule);
                schemaRule.IncludeUnknownTables = request.IncludeUnknownSchemasAndTables;
                schemaRule.IncludeUnknownViews = request.IncludeUnknownSchemasAndTables;
            }

            var functionRule = new FunctionRule() {
                Name = function.DbName,
                NewName = function.DbName != function.Name ? function.Name : null,
                NotMapped = !function.IsMapped
            };
            if (function.ImportMapping?.ResultMapping?.ComplexTypeMapping != null) {
                functionRule.ResultTypeName = function.ImportMapping?.ResultMapping.ComplexTypeMapping?.TypeName;
            }

            if (function.Import?.Parameters?.Count > 0) {
                foreach (var importParameter in function.Import.Parameters) {
                    var p = new FunctionParameterRule() {
                        Name = importParameter.Name,
                        TypeName = importParameter.Type
                    };
                    functionRule.Parameters.Add(p);
                }
            }

            schemaRule.Functions.Add(functionRule);
        }

        return root;
    }

    #endregion
}

/// <summary> Generate rules response </summary>
public sealed class GenerateRulesResponse : LoggedResponse {
    private readonly List<IRuleModelRoot> rules = new();

    /// <inheritdoc />
    public GenerateRulesResponse(IMessageLogger logger) : base(logger) { }

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary> The rules generated from the EDMX via the GenerateRules() call </summary>
    public IReadOnlyCollection<IRuleModelRoot> Rules => rules;

    /// <summary> The generated DB context rule model </summary>
    public DbContextRule DbContextRule => rules.OfType<DbContextRule>().SingleOrDefault();


    /// <summary> The EDMX model generated from the EDMX file during the GenerateRules() call </summary>
    public EdmxParsed EdmxParsed { get; internal set; }

    internal void Add<T>(T rulesRoot) where T : class, IRuleModelRoot {
        rules.Add(rulesRoot);
    }

    /// <inheritdoc />
    public override bool Success => base.Success && rules.Count > 0;
}
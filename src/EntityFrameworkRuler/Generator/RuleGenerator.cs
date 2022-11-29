using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EntityFrameworkRuler.Common;
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
    [ActivatorUtilitiesConstructor]
    public RuleGenerator() : this(null, null, null) { }

    /// <summary> Create rule generator for deriving entity structure rules from an EDMX </summary>
    /// <param name="namingService"> Service that decides how to name navigation properties.  Similar to EF ICandidateNamingService but this one utilizes the EDMX model only. </param>
    /// <param name="edmxParser"> Service that parses an EDMX file into an object model usable for rule generation. </param>
    /// <param name="ruleSaver"> Service that can persist a rule model to disk </param>
    [ActivatorUtilitiesConstructor]
    public RuleGenerator(IRulerNamingService namingService, IEdmxParser edmxParser, IRuleSaver ruleSaver) {
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
        bool includeUnknowns = false, bool compactRules = false) {
        return GenerateRules(new(edmxFilePath, useDatabaseNames, noPluralize, includeUnknowns, compactRules));
    }

    /// <inheritdoc />
    public GenerateRulesResponse GenerateRules(GeneratorOptions request) {
        var response = new GenerateRulesResponse();
        response.Log += OnResponseLog;
        try {
            var edmxFilePath = request?.EdmxFilePath;
            try {
                var parser = EdmxParser ?? new EdmxParser();
                response.EdmxParsed ??= parser.Parse(edmxFilePath);
            } catch (Exception ex) {
                response.LogError($"Error parsing EDMX: {ex.Message}");
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
                response.LogError($"Error generating output for {typeof(T)}: {ex.Message}");
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
            IncludeUnknownSchemas = request.IncludeUnknowns
        };
        var namingService = NamingService ??= new RulerNamingService(null, request);

        var generateAll = !request.IncludeUnknowns || !request.CompactRules;
        foreach (var grp in edmx.Entities.GroupBy(o => o.DbSchema)) {
            if (grp.Key.IsNullOrWhiteSpace()) continue;

            var schemaRule = new SchemaRule();
            root.Schemas.Add(schemaRule);
            schemaRule.SchemaName = grp.Key;
            schemaRule.UseSchemaName = false; // will append schema name to entity name
            schemaRule.IncludeUnknownTables = request.IncludeUnknowns;
            schemaRule.IncludeUnknownViews = request.IncludeUnknowns;
            foreach (var entity in grp.OrderBy(o => o.Name)) {
                // if entity name is different than db, it has to go into output
                var altered = false;
                // Get the expected EF entity identifier based on options.. just like EF would:
                var expectedClassName = namingService.GetExpectedEntityTypeName(entity);
                var tbl = new TableRule {
                    Name = entity.StorageName,
                    EntityName = entity.StorageName == expectedClassName ? null : expectedClassName,
                    NewName = entity.Name.CoalesceWhiteSpace(expectedClassName, entity.StorageName),
                    IncludeUnknownColumns = request.IncludeUnknowns
                };

                if (tbl.Name != tbl.NewName) altered = true;

                Debug.Assert(tbl.EntityName?.IsValidSymbolName() != false);
                Debug.Assert(tbl.NewName.IsValidSymbolName());

                foreach (var property in entity.Properties) {
                    // if property name is different than db, it has to go into output
                    // Get the expected EF property identifier based on options.. just like EF would:
                    var expectedPropertyName = namingService.GetExpectedPropertyName(property, expectedClassName);
                    if (!generateAll && (expectedPropertyName.IsNullOrWhiteSpace() ||
                                         (property.Name == expectedPropertyName && property.EnumType == null))) continue;
                    //tbl.Columns ??= new();
                    tbl.Columns.Add(new() {
                        Name = property.DbColumnName,
                        PropertyName = expectedPropertyName == property.DbColumnName ? null : expectedPropertyName,
                        NewName = property.Name == expectedPropertyName ? null : property.Name,
                        NewType = property.EnumType?.ExternalTypeName ?? property.EnumType?.FullName
                    });
                    Debug.Assert(tbl.Columns[^1].PropertyName == null || tbl.Columns[^1].PropertyName.IsValidSymbolName());
                    Debug.Assert(tbl.Columns[^1].NewName == null || tbl.Columns[^1].NewName.IsValidSymbolName());
                    altered = true;
                }

                foreach (var navigation in entity.NavigationProperties) {
                    //tbl.Navigations ??= new();
                    var navigationRename = new NavigationRule {
                        NewName = navigation.Name
                    };

                    navigationRename.Name
                        .AddRange(namingService.FindCandidateNavigationNames(navigation)
                            .Where(o => o != navigation.Name));

                    if (navigationRename.Name.Count == 0) continue;

                    //if (navigationRename.Name.Any(o => o.Contains("Inverse"))) Debugger.Break();

                    // fill in other metadata
                    var inverseNav = navigation.InverseNavigation;
                    var inverseEntity = inverseNav?.Entity;
                    navigationRename.FkName = navigation.Association?.Name;
                    navigationRename.Multiplicity = navigation.Multiplicity.ToMultiplicityString();
                    navigationRename.ToEntity =
                        inverseEntity?.ConceptualEntity?.Name ?? inverseEntity?.StorageNameIdentifier;
                    navigationRename.IsPrincipal = navigation.IsPrincipalEnd;

                    tbl.Navigations.Add(navigationRename);
                    altered = true;
                }

                if (altered || generateAll) schemaRule.Tables.Add(tbl);
            }
        }

        return root;
    }

    #endregion
}

public sealed class GenerateRulesResponse : LoggedResponse {
    private readonly List<IRuleModelRoot> rules = new();

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary> The rules generated from the EDMX via the GenerateRules() call </summary>
    public IReadOnlyCollection<IRuleModelRoot> Rules => rules;

    public DbContextRule DbContextRule => rules.OfType<DbContextRule>().SingleOrDefault();


    /// <summary> The EDMX model generated from the EDMX file during the GenerateRules() call </summary>
    public EdmxParsed EdmxParsed { get; internal set; }

    internal void Add<T>(T rulesRoot) where T : class, IRuleModelRoot {
        rules.Add(rulesRoot);
    }

    /// <inheritdoc />
    public override bool Success => base.Success && rules.Count > 0;
}
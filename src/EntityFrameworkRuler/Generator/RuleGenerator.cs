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
public sealed class RuleGenerator : RuleSaver, IRuleGenerator {
    /// <summary> Create rule generator for deriving entity structure rules from an EDMX </summary>
    /// <param name="edmxFilePath"> The EDMX file path </param>
    /// <param name="namingService"> Service that decides how to name navigation properties.  Similar to EF ICandidateNamingService but this one utilizes the EDMX model only. </param>
    /// <param name="noMetadata"> If true, generate rule files with no extra metadata about the entity models.  Only generate minimal change information. </param>
    /// <param name="noPluralize"> A value indicating whether to use the pluralizer. </param>
    /// <param name="useDatabaseNames"> A value indicating whether to use the database schema names directly. </param>
    public RuleGenerator(string edmxFilePath, IRulerNamingService namingService = null, bool noMetadata = false,
        bool noPluralize = false, bool useDatabaseNames = false)
        : this(new() {
            EdmxFilePath = edmxFilePath,
            NoPluralize = noPluralize,
            UseDatabaseNames = useDatabaseNames
        }, null) {
        this.namingService = namingService;
    }

    /// <summary> Create rule generator for deriving entity structure rules from an EDMX </summary>
    /// <param name="options"> Generator options. </param>
    /// <param name="namingService"> Service that decides how to name navigation properties.  Similar to EF ICandidateNamingService but this one utilizes the EDMX model only. </param>
    [ActivatorUtilitiesConstructor]
    public RuleGenerator(GeneratorOptions options, IRulerNamingService namingService = null) : base(options) {
        this.namingService = namingService;
    }

    #region properties

    public new GeneratorOptions Options => (GeneratorOptions)base.Options;

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary> The EDMX file path </summary>
    public string EdmxFilePath {
        get => Options.EdmxFilePath;
        set => Options.EdmxFilePath = value;
    }

    private IRulerNamingService namingService;

    /// <summary>
    /// Service that decides how to name navigation properties.
    /// Similar to EF ICandidateNamingService but this one utilizes the EDMX model only.
    /// </summary>
    public IRulerNamingService NamingService {
        get => namingService ??= new RulerNamingService(Options, null);
        set => namingService = value;
    }

    #endregion

    /// <summary> Generate rules from an EDMX such that they can be applied to a Reverse Engineered Entity Framework model to achieve the same structure as in the EDMX.
    /// Errors are monitored and added to local Errors collection. </summary>
    public Task<GenerateRulesResponse> TryGenerateRulesAsync() {
        return Task.Factory.StartNew(TryGenerateRules);
    }

    /// <summary> Generate rules from an EDMX such that they can be applied to a Reverse Engineered Entity Framework model to achieve the same structure as in the EDMX.
    /// Errors are monitored and added to local Errors collection. </summary>
    public GenerateRulesResponse TryGenerateRules() {
        var response = new GenerateRulesResponse();
        response.OnLog += ResponseOnLog;
        try {
            try {
                response.EdmxParsed ??= EdmxParser.Parse(EdmxFilePath);
            } catch (Exception ex) {
                response.LogError($"Error parsing EDMX: {ex.Message}");
                return response;
            }


            GenerateAndAdd(GetDbContextRules);

            return response;
        } finally {
            response.OnLog -= ResponseOnLog;
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

    private DbContextRule GetDbContextRules(EdmxParsed edmx) {
        if (edmx?.Entities.IsNullOrEmpty() != false) {
            var noRulesFoundBehavior = DbContextRule.DefaultNoRulesFoundBehavior;
            noRulesFoundBehavior.Name = edmx?.ContextName;
            return noRulesFoundBehavior;
        }

        var root = new DbContextRule {
            Name = edmx.ContextName,
            IncludeUnknownSchemas = Options.IncludeUnknowns
        };
        var generateAll = !Options.IncludeUnknowns || !Options.CompactRules;
        foreach (var grp in edmx.Entities.GroupBy(o => o.DbSchema)) {
            if (grp.Key.IsNullOrWhiteSpace()) continue;

            var schemaRule = new SchemaRule();
            root.Schemas.Add(schemaRule);
            schemaRule.SchemaName = grp.Key;
            schemaRule.UseSchemaName = false; // will append schema name to entity name
            schemaRule.IncludeUnknownTables = Options.IncludeUnknowns;
            schemaRule.IncludeUnknownViews = Options.IncludeUnknowns;
            foreach (var entity in grp.OrderBy(o => o.Name)) {
                // if entity name is different than db, it has to go into output
                var altered = false;
                // Get the expected EF entity identifier based on options.. just like EF would:
                var expectedClassName = NamingService.GetExpectedEntityTypeName(entity);
                var tbl = new TableRule {
                    Name = entity.StorageName,
                    EntityName = entity.StorageName == expectedClassName ? null : expectedClassName,
                    NewName = entity.Name.CoalesceWhiteSpace(expectedClassName, entity.StorageName),
                    IncludeUnknownColumns = Options.IncludeUnknowns
                };

                if (tbl.Name != tbl.NewName) altered = true;

                Debug.Assert(tbl.EntityName?.IsValidSymbolName() != false);
                Debug.Assert(tbl.NewName.IsValidSymbolName());

                foreach (var property in entity.Properties) {
                    // if property name is different than db, it has to go into output
                    // Get the expected EF property identifier based on options.. just like EF would:
                    var expectedPropertyName = NamingService.GetExpectedPropertyName(property, expectedClassName);
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
                        .AddRange(NamingService.FindCandidateNavigationNames(navigation)
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
    /// <summary> The rules generated from the EDMX via the TryGenerateRules() call </summary>
    public IReadOnlyCollection<IRuleModelRoot> Rules => rules;

    public DbContextRule DbContextRule => rules.OfType<DbContextRule>().SingleOrDefault();


    /// <summary> The correlated EDMX model that is read from the EDMX file during the TryGenerateRules() call </summary>
    public EdmxParsed EdmxParsed { get; internal set; }

    internal void Add<T>(T rulesRoot) where T : class, IRuleModelRoot {
        rules.Add(rulesRoot);
    }
}
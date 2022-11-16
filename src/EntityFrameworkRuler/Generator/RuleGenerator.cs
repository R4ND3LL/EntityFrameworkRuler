using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Generator.EdmxModel;
using EntityFrameworkRuler.Generator.Services;
using EntityFrameworkRuler.Rules;
using EntityFrameworkRuler.Rules.NavigationNaming;
using EntityFrameworkRuler.Rules.PrimitiveNaming;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMethodReturnValue.Global

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Generator;

/// <summary> Generate rules from an EDMX such that they can be applied to a Reverse Engineered Entity Framework model to achieve the same structure as in the EDMX. </summary>
public sealed class RuleGenerator : RuleProcessor, IRuleGenerator {
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
            NoMetadata = noMetadata,
            NoPluralize = noPluralize,
            UseDatabaseNames = useDatabaseNames
        }, null) {
        this.namingService = namingService;
    }

    /// <summary> Create rule generator for deriving entity structure rules from an EDMX </summary>
    /// <param name="options"> Generator options. </param>
    /// <param name="namingService"> Service that decides how to name navigation properties.  Similar to EF ICandidateNamingService but this one utilizes the EDMX model only. </param>
    [ActivatorUtilitiesConstructor]
    public RuleGenerator(GeneratorOptions options, IRulerNamingService namingService) {
        this.namingService = namingService;
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    #region properties

    public GeneratorOptions Options { get; }

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


            GenerateAndAdd(GetPrimitiveNamingRules);
            GenerateAndAdd(GetNavigationNamingRules);
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


    /// <summary> Persist the previously generated rules to the given target path. </summary>
    /// <param name="rules"> The rule models to save. </param>
    /// <param name="projectBasePath"> The location to save the rule files. </param>
    /// <param name="fileNameOptions"> Custom naming options for the rule files.  Optional. This parameter can be used to skip writing a rule file by setting that rule file to null. </param>
    /// <returns> True if completed with no errors.  When false, see Errors collection for details. </returns>
    /// <exception cref="Exception"></exception>
    public async Task<SaveRulesResponse> TrySaveRules(IEnumerable<IRuleModelRoot> rules, string projectBasePath,
        RuleFileNameOptions fileNameOptions = null) {
        var response = new SaveRulesResponse();
        response.OnLog += ResponseOnLog;
        try {
            var dir = new DirectoryInfo(projectBasePath);
            if (!dir.Exists) {
                response.LogError("Output folder does not exist");
                return response;
            }

            fileNameOptions ??= new();
            await TryWriteRules<PrimitiveNamingRules>(fileNameOptions.PrimitiveRulesFile);
            await TryWriteRules<NavigationNamingRules>(fileNameOptions.NavigationRulesFile);
            return response;


            async Task TryWriteRules<T>(string fileName) where T : class, IRuleModelRoot {
                try {
                    if (fileName.IsNullOrWhiteSpace()) return; // file skipped by user
                    T rulesRoot = rules?.OfType<T>().FirstOrDefault() ??
                                  throw new("Rule model null");
                    await WriteRules<T>(rulesRoot, fileName);
                    response.LogInformation($"{rulesRoot.Kind} rule file written to {fileName}");
                } catch (Exception ex) {
                    response.LogError($"Error writing rule to file {fileName}: {ex.Message}");
                }
            }

            Task WriteRules<T>(T rulesRoot, string filename)
                where T : class, IRuleModelRoot {
                var path = Path.Combine(dir.FullName, filename);
                return rulesRoot.ToJson<T>(path);
            }
        } finally {
            response.OnLog -= ResponseOnLog;
        }
    }


    #region Main rule gen methods

    private PrimitiveNamingRules GetPrimitiveNamingRules(EdmxParsed edmx) {
        if (edmx?.Entities.IsNullOrEmpty() != false) return new();

        var root = new PrimitiveNamingRules();

        foreach (var grp in edmx.Entities.GroupBy(o => o.DbSchema)) {
            if (grp.Key.IsNullOrWhiteSpace()) continue;

            var schemaRule = new SchemaRule();
            root.Schemas.Add(schemaRule);
            schemaRule.SchemaName = grp.Key;
            schemaRule.UseSchemaName = false; // will append schema name to entity name

            foreach (var entity in grp.OrderBy(o => o.Name)) {
                // if entity name is different than db, it has to go into output
                var renamed = false;
                // Get the expected EF entity identifier based on options.. just like EF would:
                var expectedClassName = NamingService.GetExpectedEntityTypeName(entity);
                var tbl = new TableRule {
                    Name = entity.StorageName,
                    EntityName = entity.StorageName == expectedClassName ? null : expectedClassName,
                    NewName = entity.Name.CoalesceWhiteSpace(expectedClassName, entity.StorageName)
                };

                if (tbl.Name != tbl.NewName) renamed = true;

                Debug.Assert(tbl.EntityName?.IsValidSymbolName() != false);
                Debug.Assert(tbl.NewName.IsValidSymbolName());

                foreach (var property in entity.Properties) {
                    // if property name is different than db, it has to go into output
                    // Get the expected EF property identifier based on options.. just like EF would:
                    var expectedPropertyName = NamingService.GetExpectedPropertyName(property, expectedClassName);
                    if (expectedPropertyName.IsNullOrWhiteSpace() ||
                        (property.Name == expectedPropertyName && property.EnumType == null)) continue;
                    tbl.Columns ??= new();
                    tbl.Columns.Add(new() {
                        Name = property.DbColumnName,
                        PropertyName = expectedPropertyName == property.DbColumnName ? null : expectedPropertyName,
                        NewName = property.Name == expectedPropertyName ? null : property.Name,
                        NewType = property.EnumType?.ExternalTypeName ?? property.EnumType?.FullName
                    });
                    Debug.Assert(tbl.Columns[^1].PropertyName == null || tbl.Columns[^1].PropertyName.IsValidSymbolName());
                    Debug.Assert(tbl.Columns[^1].NewName == null || tbl.Columns[^1].NewName.IsValidSymbolName());
                    renamed = true;
                }

                if (renamed) schemaRule.Tables.Add(tbl);
            }
        }

        return root;
    }

    private NavigationNamingRules GetNavigationNamingRules(EdmxParsed edmx) {
        var rule = new NavigationNamingRules();
        rule.Classes ??= new();

        if (edmx?.Entities.IsNullOrEmpty() != false) return new();

        foreach (var entity in edmx.Entities.OrderBy(o => o.Name)) {
            // omit the namespace. it often does not match the Reverse Engineered value
            rule.Namespace ??= ""; //entity.Namespace;

            // if entity name is different than db, it has to go into output
            var renamed = false;
            var tbl = new ClassReference {
                DbName = entity.Name == entity.StorageName ? null : entity.StorageName,
                Name = entity.Name, // expected name is EDMX entity name at this stage, because primitives have been applied
            };

            foreach (var navigation in entity.NavigationProperties) {
                tbl.Properties ??= new();
                var navigationRename = new NavigationRename {
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

                tbl.Properties.Add(navigationRename);
                renamed = true;
            }

            if (renamed) rule.Classes.Add(tbl);
        }

        return rule;
    }

    #endregion
}

public sealed class GenerateRulesResponse : LoggedResponse {
    private readonly List<IRuleModelRoot> rules = new();

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary> The rules generated from the EDMX via the TryGenerateRules() call </summary>
    public IReadOnlyCollection<IRuleModelRoot> Rules => rules;

    public PrimitiveNamingRules PrimitiveNamingRules => rules.OfType<PrimitiveNamingRules>().SingleOrDefault();
    public NavigationNamingRules NavigationNamingRules => rules.OfType<NavigationNamingRules>().SingleOrDefault();

    /// <summary> The correlated EDMX model that is read from the EDMX file during the TryGenerateRules() call </summary>
    public EdmxParsed EdmxParsed { get; internal set; }

    internal void Add<T>(T rulesRoot) where T : class, IRuleModelRoot {
        rules.Add(rulesRoot);
    }
}

public sealed class SaveRulesResponse : LoggedResponse {
}
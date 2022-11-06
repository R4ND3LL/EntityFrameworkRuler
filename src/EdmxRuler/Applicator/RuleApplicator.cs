using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Globalization;
using Bricelam.EntityFrameworkCore.Design;
using EdmxRuler.Extensions;
using EdmxRuler.Generator.EdmxModel;
using EdmxRuler.RuleModels;
using EdmxRuler.RuleModels.EnumMapping;
using EdmxRuler.RuleModels.NavigationNaming;
using EdmxRuler.RuleModels.PrimitiveNaming;
using Microsoft.CodeAnalysis;

[assembly: CLSCompliant(false)]

namespace EdmxRuler.Applicator;

public class RuleApplicator {
    /// <summary> Create rule applicator for making changes to project files </summary>
    /// <param name="projectBasePath">project folder containing rules and target files.</param>
    public RuleApplicator(string projectBasePath) {
        ProjectBasePath = projectBasePath;
    }

    #region properties

    public string ProjectBasePath { get; }

    #endregion

    /// <summary> Load all rule files from the project base path and apply to the enclosed project. </summary>
    /// <param name="fileNameOptions"> optional rule file naming options </param>
    /// <returns> List of errors. </returns>
    public async Task<LoadAndApplyRulesResponse> ApplyRulesInProjectPath(RuleFileNameOptions fileNameOptions = null) {
        var loadAndApplyRulesResponse = new LoadAndApplyRulesResponse {
            LoadRulesResponse = await LoadRulesInProjectPath(fileNameOptions)
        };
        loadAndApplyRulesResponse.ApplyRulesResponses =
            await ApplyRulesInternal(loadAndApplyRulesResponse.LoadRulesResponse.Rules);
        return loadAndApplyRulesResponse;
    }

    /// <summary> Apply the given rules to the target project. </summary>   
    /// <returns> List of errors. </returns>
    public async Task<List<ApplyRulesResponse>> ApplyRules(IEnumerable<IEdmxRuleModelRoot> rules) {
        if (rules == null) throw new ArgumentNullException(nameof(rules));
        var responses = await ApplyRulesInternal(rules);
        return responses;
    }

    /// <summary> Apply the given rules to the target project. </summary>  
    private async Task<List<ApplyRulesResponse>> ApplyRulesInternal(IEnumerable<IEdmxRuleModelRoot> rules) {
        if (rules == null) throw new ArgumentNullException(nameof(rules));

        List<ApplyRulesResponse> responses = new();
        foreach (var rule in rules)
            try {
                if (rule == null) continue;
                var response = rule switch {
                    PrimitiveNamingRules primitiveNamingRules => await ApplyRules(primitiveNamingRules),
                    NavigationNamingRules navigationNamingRules => await ApplyRules(navigationNamingRules),
                    EnumMappingRulesRoot enumMappingRulesRoot => await ApplyRules(enumMappingRulesRoot),
                    _ => null
                };

                if (response != null) responses.Add(response);
            } catch (Exception ex) {
                Console.WriteLine(ex);
                var response = new ApplyRulesResponse(rule);
                response.Errors.Add($"Error processing {rule}: {ex.Message}");
            }

        return responses;
    }

    /// <summary> Load all rule files from the project base path and apply to the enclosed project. </summary>
    /// <param name="fileNameOptions"> optional rule file naming options </param>
    /// <returns> Response with loaded rules and list of errors. </returns>
    public async Task<LoadRulesResponse> LoadRulesInProjectPath(RuleFileNameOptions fileNameOptions = null) {
        var response = new LoadRulesResponse();
        var rules = response.Rules;
        var errors = response.Errors;
        try {
            if (ProjectBasePath == null || !Directory.Exists(ProjectBasePath))
                throw new ArgumentException(nameof(ProjectBasePath));

            var fullProjectPath = Directory.GetFiles(ProjectBasePath, "*.csproj").FirstOrDefault();
            if (fullProjectPath == null) throw new ArgumentException("csproj not found", nameof(ProjectBasePath));

            fileNameOptions ??= new RuleFileNameOptions();

            var jsonFiles = new[] {
                    fileNameOptions.RenamingFilename, fileNameOptions.PropertyFilename,
                    fileNameOptions.EnumMappingFilename
                }
                .Where(o => o.HasNonWhiteSpace())
                .Select(o => o.Trim())
                .ToArray();


            if (jsonFiles.Length == 0) return response; // nothing to do

            foreach (var jsonFile in jsonFiles)
                try {
                    var fullPath = Path.Combine(ProjectBasePath, jsonFile);
                    var fileInfo = new FileInfo(fullPath);
                    if (!fileInfo.Exists) continue;

                    if (jsonFile == fileNameOptions.RenamingFilename) {
                        if (await TryReadRules<PrimitiveNamingRules>(fileInfo, errors) is { } schemas)
                            rules.Add(schemas);
                    } else if (jsonFile == fileNameOptions.PropertyFilename) {
                        if (await TryReadRules<NavigationNamingRules>(fileInfo, errors) is
                            { } propertyRenamingRoot)
                            rules.Add(propertyRenamingRoot);
                    } else if (jsonFile == fileNameOptions.EnumMappingFilename) {
                        if (await TryReadRules<EnumMappingRulesRoot>(fileInfo, errors) is { } enumMappingRoot)
                            rules.Add(enumMappingRoot);
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                    errors.Add($"Error processing {jsonFile}: {ex.Message}");
                }

            return response;
        } catch (Exception ex) {
            errors.Add($"Error: {ex.Message}");
            return response;
        }
    }

    private static async Task<T> TryReadRules<T>(FileInfo jsonFile, List<string> errors) where T : class, new() {
        var rules = await jsonFile.FullName.TryReadJsonFile<T>();
        if (rules == null) {
            errors.Add($"Unable to open {jsonFile.Name}");
            return null;
        }

        return rules;
    }


    public async Task<ApplyRulesResponse> ApplyRules(NavigationNamingRules navigationNamingRules,
        string contextFolder = null, string modelsFolder = null) {
        var response = new ApplyRulesResponse(navigationNamingRules);
        if (navigationNamingRules.Classes == null ||
            navigationNamingRules.Classes.Count == 0) return response; // nothing to do

        var project = await TryLoadProjectOrFallback(ProjectBasePath, contextFolder, modelsFolder, response.Errors);
        if (project == null || !project.Documents.Any()) return response;

        var renameCount = 0;
        foreach (var classRename in navigationNamingRules.Classes)
        foreach (var refRename in classRename.Properties) {
            var fromNames = new[] { refRename.Name, refRename.AlternateName }
                .Where(o => !string.IsNullOrEmpty(o)).Distinct().ToArray();
            if (fromNames.Length == 0) continue;

            Document docWithRename = null;
            foreach (var fromName in fromNames) {
                docWithRename = await project.Documents.RenamePropertyAsync(
                    classRename.Name,
                    fromName,
                    refRename.NewName);
                if (docWithRename != null) break;
            }

            if (docWithRename != null) {
                // documents have been mutated. update reference to workspace:
                project = docWithRename.Project;
                renameCount++;
                response.Information.Add(
                    $"Renamed class {classRename.Name} property {fromNames[0]} -> {refRename.NewName}");
            } else
                response.Information.Add(
                    $"Could not find table {classRename.Name} property {string.Join(", ", fromNames)}");
        }

        if (renameCount == 0) {
            response.Information.Add("No properties renamed");
            return response;
        }

        var saved = await project.Documents.SaveDocumentsAsync();
        response.Information.Add($"{renameCount} properties renamed across {saved} files");
        return response;
    }

    public async Task<ApplyRulesResponse> ApplyRules(EnumMappingRulesRoot enumMappingRulesRoot,
        string contextFolder = null,
        string modelsFolder = null) {
        var response = new ApplyRulesResponse(enumMappingRulesRoot);
        if (enumMappingRulesRoot.Classes == null || enumMappingRulesRoot.Classes.Count == 0)
            return response; // nothing to do

        var project = await TryLoadProjectOrFallback(ProjectBasePath, contextFolder, modelsFolder, response.Errors);
        if (project == null || !project.Documents.Any()) return response;

        var renameCount = 0;
        foreach (var classRename in enumMappingRulesRoot.Classes)
        foreach (var refRename in classRename.Properties) {
            var fromName = refRename.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fromName)) continue;

            var docWithRename = await project.Documents.ChangePropertyTypeAsync(
                classRename.Name,
                fromName,
                refRename.EnumType);

            if (docWithRename != null) {
                // documents have been mutated. update reference to workspace:
                project = docWithRename.Project;
                renameCount++;
                response.Information.Add(
                    $"Update class {classRename.Name} property {fromName} type to {refRename.EnumType}");
            } else
                response.Information.Add($"Could not find table {classRename.Name} property {fromName}");
        }

        if (renameCount == 0) {
            response.Information.Add("No properties types updated");
            return response;
        }

        var saved = await project.Documents.SaveDocumentsAsync();
        Debug.Assert(saved > 0, "No documents saved");
        response.Information.Add($"{renameCount} properties mapped to enums across {saved} files");
        return response;
    }

    public async Task<ApplyRulesResponse> ApplyRules(PrimitiveNamingRules primitiveNamingRules,
        string contextFolder = null, string modelsFolder = null) {
        // map to class renaming 
        var rules = primitiveNamingRules.ToPropertyRules();
        var response = await ApplyRules(rules, contextFolder, modelsFolder);
        response.Rule = primitiveNamingRules;
        return response;
    }


    private static async Task<Project> TryLoadProjectOrFallback(string fullProjectPath, string contextFolder,
        string modelsFolder, List<string> errors) {
        try {
            var dir = Path.GetDirectoryName(fullProjectPath);
            var project = await RoslynExtensions.LoadExistingProjectAsync(fullProjectPath);
            if (project?.Documents.Any() == true) return project;

            project = TryLoadFallbackAdhocProject(dir, contextFolder, modelsFolder, errors);
            return project;
        } catch (Exception ex) {
            errors.Add($"Error loading project: {ex.Message}");
            return null;
        }
    }

    private static Project TryLoadFallbackAdhocProject(string projectBasePath, string contextFolder,
        string modelsFolder, List<string> errors) {
        /* If existing project file fails to load, we can fallback to creating an adhoc workspace with an in memory project
                  * that contains only the cs files we are concerned with.
                  *
                  * Currently, this will successfully rename the model properties but it does NOT process the references from the Configuration
                  * classes.  This is likely because Roslyn is not resolving information about EntityTypeBuilder<> such that it
                  * can identify the model property references.
                  *
                  * This issue *should* be resolved simply by adding EFC references to the project (as is illustrated below) but
                  * it's still not working.
                  *
                  * To do: resolve paths to EFC assemblies and use those in the refAssemblies list below.
                  */
        var cSharpFolders = new HashSet<string>();
        if (contextFolder?.Length > 0 && Directory.Exists(Path.Combine(projectBasePath, contextFolder)))
            cSharpFolders.Add(Path.Combine(projectBasePath, contextFolder));

        if (modelsFolder?.Length > 0 && Directory.Exists(Path.Combine(projectBasePath, modelsFolder)))
            cSharpFolders.Add(Path.Combine(projectBasePath, modelsFolder));

        if (cSharpFolders.Count == 0)
            // use project base path
            cSharpFolders.Add(projectBasePath);

        var cSharpFiles = cSharpFolders
            .SelectMany(o => Directory.GetFiles(o, "*.cs", SearchOption.AllDirectories))
            .Distinct().ToList();

        if (cSharpFiles.Count == 0) {
            errors.Add("No .cs files found");
            return null;
        }

        var refAssemblies = new[] {
            // typeof(IEntityTypeConfiguration<>).Assembly, typeof(SqlServerValueGenerationStrategy).Assembly,
            // typeof(Microsoft.EntityFrameworkCore.SqlServer.Query.Internal.SearchConditionConvertingExpressionVisitor).Assembly,
            // typeof(Microsoft.EntityFrameworkCore.RelationalDbFunctionsExtensions).Assembly,
            typeof(NotMappedAttribute).Assembly
        }.Distinct().ToList();
        var refs = refAssemblies.Select(o => MetadataReference.CreateFromFile(o.Location)).ToList();

        try {
            using var workspace = cSharpFiles.GetWorkspaceForFilePaths(refs);
            var project = workspace.CurrentSolution.Projects.First();
            if (project?.Documents.Any() == true) return project;
        } catch (Exception ex) {
            errors.Add($"Unable to get in-memory project from workspace: {ex.Message}");
            return null;
        }

        return null;
    }
}

public sealed class LoadRulesResponse {
    public List<IEdmxRuleModelRoot> Rules { get; } = new();
    public List<string> Errors { get; } = new();
}

public sealed class ApplyRulesResponse {
    internal ApplyRulesResponse(IEdmxRuleModelRoot ruleModelRoot) {
        Rule = ruleModelRoot;
    }

    public IEdmxRuleModelRoot Rule { get; internal set; }
    public List<string> Errors { get; } = new();
    public List<string> Information { get; } = new();
}

public sealed class LoadAndApplyRulesResponse {
    public LoadRulesResponse LoadRulesResponse { get; internal set; }
    public List<ApplyRulesResponse> ApplyRulesResponses { get; internal set; }

    /// <summary> return all errors in this response (including rule application responses) </summary>
    public IEnumerable<string> GetErrors() {
        if (LoadRulesResponse?.Errors?.Count > 0)
            foreach (var error in LoadRulesResponse.Errors)
                yield return error;
        if (!(ApplyRulesResponses?.Count > 0)) yield break;
        foreach (var r in ApplyRulesResponses)
            if (r?.Errors?.Count > 0)
                foreach (var error in r.Errors)
                    yield return error;
    }

    /// <summary> return all information statements in this response (including rule application responses) </summary>
    public IEnumerable<string> GetInformation() {
        if (!(ApplyRulesResponses?.Count > 0)) yield break;
        foreach (var r in ApplyRulesResponses)
            if (r?.Information?.Count > 0)
                foreach (var error in r.Information)
                    yield return error;
    }
}
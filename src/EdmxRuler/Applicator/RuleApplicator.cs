using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EdmxRuler.Applicator.CsProjParser;
using EdmxRuler.Extensions;
using EdmxRuler.RuleModels;
using EdmxRuler.RuleModels.EnumMapping;
using EdmxRuler.RuleModels.NavigationNaming;
using EdmxRuler.RuleModels.PrimitiveNaming;
using Microsoft.CodeAnalysis;
using Project = Microsoft.CodeAnalysis.Project;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal
[assembly: CLSCompliant(false)]

namespace EdmxRuler.Applicator;

public sealed class RuleApplicator {
    /// <summary> Create rule applicator for making changes to project files </summary>
    /// <param name="projectBasePath">project folder containing rules and target files.</param>
    public RuleApplicator(string projectBasePath) {
        ProjectBasePath = projectBasePath;
    }

    #region properties

    /// <summary> The target project path. </summary>
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
                    EnumMappingRules enumMappingRulesRoot => await ApplyRules(enumMappingRulesRoot),
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
                    fileNameOptions.PrimitiveNamingFile, fileNameOptions.NavigationNamingFile,
                    fileNameOptions.EnumMappingFile
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

                    if (jsonFile == fileNameOptions.PrimitiveNamingFile) {
                        if (await TryReadRules<PrimitiveNamingRules>(fileInfo, errors) is { } schemas)
                            rules.Add(schemas);
                    } else if (jsonFile == fileNameOptions.NavigationNamingFile) {
                        if (await TryReadRules<NavigationNamingRules>(fileInfo, errors) is { } propertyRenamingRoot)
                            rules.Add(propertyRenamingRoot);
                    } else if (jsonFile == fileNameOptions.EnumMappingFile) {
                        if (await TryReadRules<EnumMappingRules>(fileInfo, errors) is { } enumMappingRoot)
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
        if (rules != null) return rules;
        errors.Add($"Unable to open {jsonFile.Name}");
        return null;
    }

    /// <summary> Apply the given rules to the target project. </summary>
    /// <param name="navigationNamingRules"> The rules to apply. </param>
    /// <param name="contextFolder"> Optional folder where data context is found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <param name="modelsFolder"> Optional folder where models are found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <returns></returns>
    public Task<ApplyRulesResponse> ApplyRules(NavigationNamingRules navigationNamingRules,
        string contextFolder = null, string modelsFolder = null) {
        var response = new ApplyRulesResponse(navigationNamingRules);
        return ApplyRulesCore(navigationNamingRules.Classes, navigationNamingRules.Namespace, response,
            contextFolder: contextFolder,
            modelsFolder: modelsFolder);
    }

    /// <summary> Apply the given rules to the target project. </summary>
    /// <param name="enumMappingRules"> The rules to apply. </param>
    /// <param name="contextFolder"> Optional folder where data context is found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <param name="modelsFolder"> Optional folder where models are found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <returns></returns>
    public Task<ApplyRulesResponse> ApplyRules(EnumMappingRules enumMappingRules,
        string contextFolder = null,
        string modelsFolder = null) {
        var response = new ApplyRulesResponse(enumMappingRules);
        return ApplyRulesCore(enumMappingRules.Classes, enumMappingRules.Namespace, response,
            contextFolder: contextFolder,
            modelsFolder: modelsFolder);
    }

    /// <summary> Apply the given rules to the target project. </summary>
    /// <param name="primitiveNamingRules"> The rules to apply. </param>
    /// <param name="contextFolder"> Optional folder where data context is found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <param name="modelsFolder"> Optional folder where models are found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <returns></returns>
    public async Task<ApplyRulesResponse> ApplyRules(PrimitiveNamingRules primitiveNamingRules,
        string contextFolder = null, string modelsFolder = null) {
        // map to class renaming 
        var rules = primitiveNamingRules.ToPropertyRules();
        var response = await ApplyRules(rules, contextFolder, modelsFolder);
        response.Rule = primitiveNamingRules;
        return response;
    }

    /// <summary> Apply the given rules to the target project. </summary>
    /// <param name="classRules"> The rules to apply. </param>
    /// <param name="namespaceName"></param>
    /// <param name="response"> The response to fill. </param>
    /// <param name="contextFolder"> Optional folder where data context is found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <param name="modelsFolder"> Optional folder where models are found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <returns></returns>
    private async Task<ApplyRulesResponse> ApplyRulesCore(IEnumerable<IEdmxRuleClassModel> classRules,
        string namespaceName,
        ApplyRulesResponse response,
        string contextFolder = null, string modelsFolder = null) {
        if (classRules == null) return response; // nothing to do

        var project = await TryLoadProjectOrFallback(ProjectBasePath, contextFolder, modelsFolder, response);
        if (project == null || !project.Documents.Any()) return response;

        var renameCount = 0;
        var classRenameCount = 0;
        var typeMapCount = 0;
        foreach (var classRef in classRules) {
            var newClassName = classRef.GetNewName();
            var oldClassName = classRef.GetOldName().CoalesceWhiteSpace(newClassName);
            newClassName = newClassName.CoalesceWhiteSpace(oldClassName);
            if (newClassName.IsNullOrWhiteSpace()) continue; // invalid entry

            // first rename class if needed
            var canRenameClass = newClassName != oldClassName;
            if (canRenameClass) {
                var docWithChange = await project.Documents.RenameClassAsync(namespaceName, oldClassName, newClassName);

                if (docWithChange != null) {
                    // documents have been mutated. update reference to project:
                    project = docWithChange.Project;
                    classRenameCount++;
                    response.Information.Add($"Renamed class {oldClassName} to {newClassName}");
                } else {
                    response.Information.Add($"Could not find class {oldClassName}");
                    // property processing may still work if the class is found under the new name
                }
            }

            // process property changes
            foreach (var propertyRef in classRef.GetProperties()) {
                var newName = propertyRef.GetNewName().NullIfWhitespace();
                var currentNames = propertyRef.GetCurrentNameOptions()
                    .Where(o => o.HasNonWhiteSpace()).Select(o => o.Trim()).Distinct().ToArray();

                var canRename = currentNames.Length > 0 && newName.HasNonWhiteSpace() &&
                                currentNames.Any(o => o != newName);
                if (canRename) {
                    Document docWithChange = null;
                    int i;
                    var fromNames = currentNames.Where(o => o != newName).ToArray();
                    for (i = 0; i < fromNames.Length; i++) {
                        var fromName = fromNames[i];
                        if (fromName == newName) continue;
                        docWithChange = await project.Documents.RenamePropertyAsync(namespaceName,
                            newClassName,
                            fromName,
                            newName);
                        if (docWithChange != null) break;
                    }

                    if (docWithChange != null) {
                        // documents have been mutated. update reference to project:
                        project = docWithChange.Project;
                        renameCount++;
                        response.Information.Add(
                            $"Renamed class {newClassName} property {fromNames[i]} -> {newName}");
                    } else {
                        response.Information.Add(
                            $"Could not find class {newClassName} property {string.Join(", ", fromNames)}");
                        // further processing may still work if the property is found under the new name
                    }
                }

                if (newName.HasNonWhiteSpace())
                    currentNames = new[] { newName }; // ensure we are looking for the new name only
                newName ??= currentNames.FirstOrDefault();

                var newType = propertyRef.GetNewTypeName();
                var canChangeType = newType.HasNonWhiteSpace() && currentNames.Any(o => o.HasNonWhiteSpace());
                if (canChangeType) {
                    Document docWithChange = null;
                    int i;
                    for (i = 0; i < currentNames.Length; i++) {
                        var fromName = currentNames[i];
                        docWithChange = await project.Documents.ChangePropertyTypeAsync(namespaceName,
                            newClassName,
                            fromName,
                            newType);
                        if (docWithChange != null) break;
                    }

                    if (docWithChange != null) {
                        // documents have been mutated. update reference to project:
                        project = docWithChange.Project;
                        typeMapCount++;
                        response.Information.Add(
                            $"Updated class {newClassName} property {currentNames[i]} type to {newType}");
                    } else
                        response.Information.Add(
                            $"Could not find class {newClassName} property {string.Join(", ", currentNames)}");
                }
            }
        }

        if (classRenameCount == 0 && renameCount == 0 && typeMapCount == 0) {
            response.Information.Add("No changes made");
            return response;
        }

        var saved = await project.Documents.SaveDocumentsAsync();

        var sb = new StringBuilder();
        if (classRenameCount > 0) {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append($"{classRenameCount} classes renamed");
        }

        if (renameCount > 0) {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append($"{renameCount} properties renamed");
        }

        if (typeMapCount > 0) {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append($"{typeMapCount} property types changed");
        }

        sb.Append($" across {saved} files");
        response.Information.Add(sb.ToString());
        return response;
    }

    private static async Task<Project> TryLoadProjectOrFallback(string projectBasePath, string contextFolder,
        string modelsFolder, ApplyRulesResponse response) {
        try {
            var dir = new DirectoryInfo(projectBasePath);
            var csProjFiles = dir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);

            Project project;
            foreach (var csProjFile in csProjFiles) {
                project = await RoslynExtensions.LoadExistingProjectAsync(csProjFile.FullName, response?.Errors);
                if (project?.Documents.Any() == true) return project;
            }

            response?.Information?.Add("Direct project load failed. Attempting adhoc project creation...");

            project = TryLoadFallbackAdhocProject(projectBasePath, contextFolder, modelsFolder, response);
            return project;
        } catch (Exception ex) {
            response?.Errors?.Add($"Error loading project: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// If existing project file fails to load, we can fallback to creating an adhoc workspace with an in memory project.
    /// that contains only the cs files we are concerned with.
    ///
    /// This approach is sensitive to lack of referenced symbols such as missing usings and references especially around
    /// the EF references.  i.e. if those references are not resolved then symbol inspection cannot fully work,
    /// and therefore some symbols is not be renamed.
    ///
    /// To combat that, we add all assemblies referenced by the current project to cover the basics.  Then we add
    /// in my mocked EF resources as substitution for EF references, which works well because all we need is the API surface.
    ///
    /// All entity configurations are then processed properly from a renaming perspective.
    ///
    /// Known issue:  Target projects that have ImplicitUsings enabled are naturally missing numerous usings statements.
    /// This interferes with symbol identification and will result in some symbols not being renamed.  Therefore,
    /// the target project should ideally have this feature disabled: &lt;ImplicitUsings&gt;disable&lt;/ImplicitUsings&gt;
    /// </summary>
    /// <param name="projectBasePath"> Project base path</param>
    /// <param name="contextFolder"> Optional folder where data context is found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <param name="modelsFolder"> Optional folder where models are found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <param name="response"> Errors list. </param>
    /// <returns></returns>
    private static Project TryLoadFallbackAdhocProject(string projectBasePath, string contextFolder,
        string modelsFolder, ApplyRulesResponse response) {
        var csProj = InspectProject(projectBasePath, response?.Errors);

        if (csProj?.ImplicitUsings.In("enabled", "enable", "true") == true) {
            // symbol renaming will likely not work correctly. 
            response?.Information.Add(
                "WARNING: ImplicitUsings is enabled on this project. Symbol renaming may not fully work due to missing reference information.");
        }

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

        var ignorePaths = new HashSet<string> {
            Path.Combine(projectBasePath, "obj") + Path.DirectorySeparatorChar,
            Path.Combine(projectBasePath, "Debug") + Path.DirectorySeparatorChar,
        };
        var toIgnore = cSharpFiles
            .Where(o => ignorePaths.Any(ip => o.StartsWith(ip, StringComparison.OrdinalIgnoreCase))).ToList();
        toIgnore.ForEach(o => cSharpFiles.Remove(o));

        if (cSharpFiles.Count == 0) {
            response?.Errors.Add("No .cs files found");
            return null;
        }

        var refAssemblies = new HashSet<Assembly>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies) {
            if (assembly.IsDynamic) continue;
            refAssemblies.Add(assembly);
        }

        try {
            using var workspace = cSharpFiles
                .GetWorkspaceForFilePaths(refAssemblies)
                .AddEntityResources();
            var project = workspace.CurrentSolution.Projects.First();
            if (project?.Documents.Any() == true) {
                // add project references?
                return project;
            }
        } catch (Exception ex) {
            response?.Errors.Add($"Unable to get in-memory project from workspace: {ex.Message}");
            return null;
        }

        return null;
    }

    private static CsProject InspectProject(string projectBasePath, List<string> errors) {
        try {
            var dir = new DirectoryInfo(projectBasePath);
            var csProjFiles = dir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);
            foreach (var csProjFile in csProjFiles) {
                //var csProjModel = EdmxSerializer.Deserialize(File.ReadAllText(csProjFile.FullName));
                var text = File.ReadAllText(csProjFile.FullName);
                CsProject csProj;
                try {
                    csProj = CsProjSerializer.Deserialize(text);
                } catch (Exception ex) {
                    errors?.Add($"Unable to parse csproj: {ex.Message}");
                    continue;
                }

                return csProj;
            }
        } catch (Exception ex) {
            errors?.Add($"Unable to read csproj: {ex.Message}");
        }

        return new CsProject();
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

[SuppressMessage("ReSharper", "ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator")]
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
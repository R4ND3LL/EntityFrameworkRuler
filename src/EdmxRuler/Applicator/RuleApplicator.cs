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
using EdmxRuler.Common;
using EdmxRuler.Extensions;
using EdmxRuler.Rules;
using EdmxRuler.Rules.NavigationNaming;
using EdmxRuler.Rules.PrimitiveNaming;
using EdmxRuler.Rules.PropertyTypeChanging;
using Microsoft.CodeAnalysis;
using Project = Microsoft.CodeAnalysis.Project;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal
[assembly: CLSCompliant(false)]

namespace EdmxRuler.Applicator;

public sealed class RuleApplicator : RuleProcessor, IRuleApplicator {
    /// <summary> Create rule applicator for making changes to project files </summary>
    /// <param name="projectBasePath">project folder containing rules and target files.</param>
    /// <param name="adhocOnly"> Form an adhoc in-memory project out of the target entity model files instead of loading project directly. </param>
    public RuleApplicator(string projectBasePath, bool adhocOnly = false)
        : this(new ApplicatorOptions() {
            ProjectBasePath = projectBasePath,
            AdhocOnly = adhocOnly
        }) {
    }

    /// <summary> Create rule applicator for making changes to project files </summary>
    public RuleApplicator(ApplicatorOptions options) {
        Options = options;
    }


    #region properties

    public ApplicatorOptions Options { get; }

    /// <summary> The target project path containing entity models. </summary>
    public string ProjectBasePath {
        get => Options.ProjectBasePath;
        set => Options.ProjectBasePath = value;
    }

    /// <summary> Form an adhoc in-memory project out of the target entity model files instead of loading project directly. </summary>
    public bool AdhocOnly {
        get => Options.AdhocOnly;
        set => Options.AdhocOnly = value;
    }

    #endregion

    /// <summary> Load all rule files from the project base path and apply to the enclosed project. </summary>
    /// <param name="fileNameOptions"> optional rule file naming options </param>
    /// <returns> List of errors. </returns>
    public async Task<LoadAndApplyRulesResponse> ApplyRulesInProjectPath(RuleFileNameOptions fileNameOptions = null) {
        var response = new LoadAndApplyRulesResponse {
            LoadRulesResponse = await LoadRulesInProjectPath(fileNameOptions)
        };
        response.OnLog += ResponseOnLog;
        try {
            response.ApplyRulesResponses =
                await ApplyRulesInternal(response.LoadRulesResponse.Rules);
            return response;
        } finally {
            response.OnLog += ResponseOnLog;
        }
    }

    /// <summary> Apply the given rules to the target project. </summary>
    /// <returns> List of errors. </returns>
    public async Task<IReadOnlyList<ApplyRulesResponse>> ApplyRules(IEnumerable<IEdmxRuleModelRoot> rules) {
        if (rules == null) throw new ArgumentNullException(nameof(rules));
        var responses = await ApplyRulesInternal(rules);
        return responses;
    }

    /// <summary> Apply the given rules to the target project. </summary>
    private async Task<IReadOnlyList<ApplyRulesResponse>> ApplyRulesInternal(IEnumerable<IEdmxRuleModelRoot> rules) {
        if (rules == null) throw new ArgumentNullException(nameof(rules));

        var state = new RoslynProjectState(this);
        List<ApplyRulesResponse> responses = new();
        foreach (var rule in rules.Where(o => o != null).OrderBy(o => o.Kind)) {
            ApplyRulesResponse response = null;
            try {
                if (rule == null) continue;

                response = new ApplyRulesResponse(rule);
                response.OnLog += ResponseOnLog;
                try {
                    switch (rule) {
                        case PrimitiveNamingRules primitiveNamingRules:
                            await ApplyPrimitiveRulesCore(primitiveNamingRules, response, null, null, state);
                            break;
                        case NavigationNamingRules navigationNamingRules:
                            await ApplyRulesCore(navigationNamingRules.Classes, navigationNamingRules.Namespace,
                                response, state: state);
                            break;
                        case PropertyTypeChangingRules propertyTypeChangingRules:
                            await ApplyRulesCore(propertyTypeChangingRules.Classes,
                                propertyTypeChangingRules.Namespace, response, state: state);
                            break;
                        default:
                            continue;
                    }
                } finally {
                    response.OnLog -= ResponseOnLog;
                }

                responses.Add(response);
            } catch (Exception ex) {
                Console.WriteLine(ex);
                response ??= new ApplyRulesResponse(rule);
                response.LogError($"Error processing {rule}: {ex.Message}");
                responses.Add(response);
            }
        }

        return responses;
    }

    /// <summary> Load all rule files from the project base path and apply to the enclosed project. </summary>
    /// <param name="fileNameOptions"> optional rule file naming options </param>
    /// <returns> Response with loaded rules and list of errors. </returns>
    public async Task<LoadRulesResponse> LoadRulesInProjectPath(RuleFileNameOptions fileNameOptions = null) {
        var response = new LoadRulesResponse();
        response.OnLog += ResponseOnLog;
        var rules = response.Rules;
        try {
            if (ProjectBasePath == null || !Directory.Exists(ProjectBasePath))
                throw new ArgumentException(nameof(ProjectBasePath));

            var fullProjectPath = Directory.GetFiles(ProjectBasePath, "*.csproj").FirstOrDefault();
            if (fullProjectPath == null) throw new ArgumentException("csproj not found", nameof(ProjectBasePath));

            fileNameOptions ??= new RuleFileNameOptions();

            var jsonFiles = new[] {
                    fileNameOptions.PrimitiveNamingFile, fileNameOptions.NavigationNamingFile,
                    fileNameOptions.PropertyTypeChangingFile
                }
                .Where(o => o.HasNonWhiteSpace())
                .Select(o => o.Trim())
                .ToArray();


            if (jsonFiles.Length == 0) return response; // nothing to do

            foreach (var jsonFile in jsonFiles)
                try {
                    if (jsonFile.IsNullOrWhiteSpace()) continue;
                    var fullPath = Path.Combine(ProjectBasePath, jsonFile);
                    var fileInfo = new FileInfo(fullPath);
                    if (!fileInfo.Exists) continue;

                    if (jsonFile == fileNameOptions.PrimitiveNamingFile) {
                        if (await TryReadRules<PrimitiveNamingRules>(fileInfo, response) is { } schemas)
                            rules.Add(schemas);
                    } else if (jsonFile == fileNameOptions.NavigationNamingFile) {
                        if (await TryReadRules<NavigationNamingRules>(fileInfo, response) is { } propertyRenamingRoot)
                            rules.Add(propertyRenamingRoot);
                    } else if (jsonFile == fileNameOptions.PropertyTypeChangingFile) {
                        if (await TryReadRules<PropertyTypeChangingRules>(fileInfo, response) is
                            { } propertyTypeChangingRules)
                            rules.Add(propertyTypeChangingRules);
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                    response.LogError($"Error processing {jsonFile}: {ex.Message}");
                }

            return response;
        } catch (Exception ex) {
            response.LogError($"Error: {ex.Message}");
            return response;
        } finally {
            response.OnLog -= ResponseOnLog;
        }
    }

    private static async Task<T> TryReadRules<T>(FileInfo jsonFile, LoggedResponse loggedResponse)
        where T : class, new() {
        var rules = await jsonFile.FullName.TryReadJsonFile<T>();
        if (rules != null) return rules;
        loggedResponse.LogError($"Unable to open {jsonFile.Name}");
        return null;
    }

    /// <summary> Apply the given rules to the target project. </summary>
    /// <param name="navigationNamingRules"> The rules to apply. </param>
    /// <param name="contextFolder"> Optional folder where data context is found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <param name="modelsFolder"> Optional folder where models are found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <returns></returns>
    public async Task<ApplyRulesResponse> ApplyRules(NavigationNamingRules navigationNamingRules,
        string contextFolder = null, string modelsFolder = null) {
        var response = new ApplyRulesResponse(navigationNamingRules);
        response.OnLog += ResponseOnLog;
        try {
            await ApplyRulesCore(navigationNamingRules.Classes, navigationNamingRules.Namespace, response,
                contextFolder: contextFolder,
                modelsFolder: modelsFolder);
        } finally {
            response.OnLog -= ResponseOnLog;
        }

        return response;
    }

    /// <summary> Apply the given rules to the target project. </summary>
    /// <param name="propertyTypeChangingRules"> The rules to apply. </param>
    /// <param name="contextFolder"> Optional folder where data context is found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <param name="modelsFolder"> Optional folder where models are found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <returns></returns>
    public async Task<ApplyRulesResponse> ApplyRules(PropertyTypeChangingRules propertyTypeChangingRules,
        string contextFolder = null,
        string modelsFolder = null) {
        var response = new ApplyRulesResponse(propertyTypeChangingRules);
        response.OnLog += ResponseOnLog;
        try {
            await ApplyRulesCore(propertyTypeChangingRules.Classes, propertyTypeChangingRules.Namespace, response,
                contextFolder: contextFolder,
                modelsFolder: modelsFolder);
            return response;
        } finally {
            response.OnLog -= ResponseOnLog;
        }
    }

    /// <summary> Apply the given rules to the target project. </summary>
    /// <param name="primitiveNamingRules"> The rules to apply. </param>
    /// <param name="contextFolder"> Optional folder where data context is found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <param name="modelsFolder"> Optional folder where models are found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <returns></returns>
    public async Task<ApplyRulesResponse> ApplyRules(PrimitiveNamingRules primitiveNamingRules,
        string contextFolder = null, string modelsFolder = null) {
        // map to class renaming
        var response = new ApplyRulesResponse(primitiveNamingRules);
        response.OnLog += ResponseOnLog;
        try {
            var state = new RoslynProjectState(this);
            await ApplyPrimitiveRulesCore(primitiveNamingRules, response, contextFolder, modelsFolder, state);
            return response;
        } finally {
            response.OnLog -= ResponseOnLog;
        }
    }

    private async Task ApplyPrimitiveRulesCore(PrimitiveNamingRules primitiveNamingRules, ApplyRulesResponse response,
        string contextFolder,
        string modelsFolder, RoslynProjectState state) {
        foreach (var schema in primitiveNamingRules.Schemas) {
            var schemaResponse = new ApplyRulesResponse(null);
            schemaResponse.OnLog += ResponseOnLog;
            try {
                await ApplyRulesCore(schema.Tables, schema.Namespace, schemaResponse, contextFolder, modelsFolder,
                    state);
                response.Merge(schemaResponse);
            } finally {
                schemaResponse.OnLog -= ResponseOnLog;
            }
        }
    }

    /// <summary> Apply the given rules to the target project. </summary>
    /// <param name="classRules"> The rules to apply. </param>
    /// <param name="namespaceName"></param>
    /// <param name="response"> The response to fill. </param>
    /// <param name="contextFolder"> Optional folder where data context is found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <param name="modelsFolder"> Optional folder where models are found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <param name="state"> Roslyn project state. Internal use only. </param>
    /// <returns></returns>
    private async Task ApplyRulesCore(IEnumerable<IEdmxRuleClassModel> classRules,
        string namespaceName,
        ApplyRulesResponse response,
        string contextFolder = null, string modelsFolder = null, RoslynProjectState state = null) {
        string ToFullClassName(string className) {
            return namespaceName.HasNonWhiteSpace() ? $"{namespaceName}.{className}" : className;
        }

        if (classRules == null) return; // nothing to do

        state ??= new RoslynProjectState(this);
        await state.TryLoadProjectOrFallbackOnce(ProjectBasePath, contextFolder, modelsFolder, response);
        if (state.Project == null || !state.Project.Documents.Any()) return;

        var propRenameCount = 0;
        var classRenameCount = 0;
        var typeMapCount = 0;
        var dirtyClassStates = new HashSet<ClassState>();
        foreach (var classRef in classRules) {
            var newClassName = classRef.GetNewName();
            var oldClassName = classRef.GetOldName().CoalesceWhiteSpace(newClassName);
            newClassName = newClassName.CoalesceWhiteSpace(oldClassName);
            if (newClassName.IsNullOrWhiteSpace()) continue; // invalid entry

            // first rename class if needed
            var canRenameClass = newClassName != oldClassName;
            var classState = new ClassState(state);
            if (canRenameClass) {
                var classActionResult =
                    await state.Project.RenameClassAsync(namespaceName, oldClassName, newClassName);

                if (classActionResult.ClassLocated) {
                    // documents have been mutated. update reference to project:
                    state.Project = classActionResult.Project;
                    classState.ClassSymbol = classActionResult.ClassSymbol;
                    classRenameCount++;
                    dirtyClassStates.Add(classState);
                    response.LogInformation($"Renamed class {oldClassName} to {newClassName}");
                } else {
                    // property processing may still work if the class is found under the new name
                    classState.ClassSymbol = (await state.Project.FindClassesByName(namespaceName, newClassName))
                        .FirstOrDefault();
                    if (classState.ClassExists) {
                        response.LogInformation(
                            $"Class already exists with target name: {ToFullClassName(newClassName)}");
                    } else {
                        response.LogInformation($"Could not find class {ToFullClassName(oldClassName)}");
                        continue;
                    }
                }
            } else {
                classState.ClassSymbol =
                    (await state.Project.FindClassesByName(namespaceName, newClassName)).FirstOrDefault();
            }

            if (!classState.ClassExists) {
                response.LogInformation($"Could not find class {ToFullClassName(newClassName)}");
                continue;
            }

            void ProjectUpdated(PropertyActionResult docWithChange) {
                Debug.Assert(docWithChange.PropertyLocated);
                state.Project = docWithChange.Project;
                classState.MarkClassStale();
                dirtyClassStates.Add(classState);
            }


            // process property changes
            foreach (var propertyRef in classRef.GetProperties()) {
                var newPropName = propertyRef.GetNewName().NullIfWhitespace();
                var currentNames = propertyRef.GetCurrentNameOptions()
                    .Where(o => o.HasNonWhiteSpace()).Select(o => o.Trim()).Distinct().ToArray();

                bool? propertyExists = null;
                var canRename = currentNames.Length > 0 && newPropName.HasNonWhiteSpace() &&
                                currentNames.Any(o => o != newPropName);
                if (canRename) {
                    var navMeta = propertyRef.GetNavigationMetadata();
                    PropertyActionResult propertyActionResult = default;
                    int i;
                    var fromNames = currentNames.Where(o => o != newPropName).ToArray();
                    for (i = 0; i < fromNames.Length; i++) {
                        var fromName = fromNames[i];
                        if (fromName == newPropName) continue;
                        propertyActionResult =
                            await state.Project.RenamePropertyAsync(await classState.GetClassSymbol(), fromName,
                                newPropName);
                        if (propertyActionResult.PropertyLocated) break;
                    }

                    if (propertyActionResult.PropertyLocated) {
                        // documents have been mutated. update reference to project:
                        ProjectUpdated(propertyActionResult);
                        propRenameCount++;
                        propertyExists = true;
                        response.LogInformation(
                            $"Renamed property {newClassName}.{fromNames[i]} to {newPropName}");
                    } else {
                        // further processing may still work if the property is found under the new name
                        var currentClassSymbol = await classState.GetClassSymbol();
                        propertyExists = await currentClassSymbol.PropertyExists(newPropName);
                        if (propertyExists.Value)
                            response.LogInformation(
                                $"Property already exists with target name: {newClassName}.{newPropName}");
                        else {
                            response.LogInformation(
                                $"Could not find property {ToFullClassName(newClassName)}.{string.Join("/", fromNames)}");
                            continue;
                        }
                    }
                }

                if (newPropName.HasNonWhiteSpace())
                    currentNames = new[] { newPropName }; // ensure we are looking for the new name only
                newPropName ??= currentNames.FirstOrDefault();

                if (!propertyExists.HasValue) {
                    var currentClassSymbol = await classState.GetClassSymbol();
                    propertyExists = await currentClassSymbol.PropertyExists(newPropName);
                }

                if (propertyExists != true) {
                    response.LogInformation($"Could not find property {ToFullClassName(newClassName)}.{newPropName}");
                    continue;
                }


                var newType = propertyRef.GetNewTypeName();
                var canChangeType = newType.HasNonWhiteSpace() && currentNames.Any(o => o.HasNonWhiteSpace());
                if (canChangeType) {
                    PropertyActionResult propertyActionResult = default;
                    int i;
                    for (i = 0; i < currentNames.Length; i++) {
                        var fromName = currentNames[i];
                        propertyActionResult =
                            await state.Project.ChangePropertyTypeAsync(await classState.GetClassSymbol(), fromName,
                                newType);
                        if (propertyActionResult.PropertyLocated) break;
                    }

                    if (propertyActionResult.PropertyLocated) {
                        // documents have been mutated. update reference to project:
                        ProjectUpdated(propertyActionResult);
                        typeMapCount++;
                        response.LogInformation(
                            $"Updated property {newClassName}.{currentNames[i]} type to {newType}");
                    } else {
                        // should not arrive here because logic above confirms that the property exists
#if DEBUG
                        if (Debugger.IsAttached) Debugger.Break();
#endif
                        response.LogInformation(
                            $"Could not find property {ToFullClassName(newClassName)}.{string.Join(", ", currentNames)}");
                    }
                }
            }
        }

        if (classRenameCount == 0 && propRenameCount == 0 && typeMapCount == 0) {
            response.LogInformation("No changes made");
            return;
        }

        int saved;
        if (classRenameCount > 0 || propRenameCount > 0) {
            // unfortunately we have to go over all documents and save because we don't know how far reaching the rename refactoring was.
            var changes = state.Project.GetChangedDocuments(state.OriginalProject);
            saved = changes?.Count > 0 ? await changes.SaveDocumentsAsync(false) : 0;
        } else {
            // type mapping only.  we know the exact docs that changed.
            Debug.Assert(typeMapCount > 0 && dirtyClassStates.Count > 0);
            var changedDocIds = dirtyClassStates.SelectMany(o => o.GetDocumentIds()).Distinct().ToList();
            saved = await state.Project.SaveDocumentsAsync(changedDocIds);
        }

        var sb = new StringBuilder();
        if (classRenameCount > 0) {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append($"{classRenameCount} classes renamed");
        }

        if (propRenameCount > 0) {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append($"{propRenameCount} properties renamed");
        }

        if (typeMapCount > 0) {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append($"{typeMapCount} property types changed");
        }

        sb.Append($" across {saved} files");
        response.LogInformation(sb.ToString());
        return;
    }

    private async Task<Project> TryLoadProjectOrFallback(string projectBasePath, string contextFolder,
        string modelsFolder, ApplyRulesResponse response) {
        try {
            var dir = new DirectoryInfo(projectBasePath);
            var csProjFiles = dir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);

            Project project;
            if (!AdhocOnly) {
                foreach (var csProjFile in csProjFiles) {
                    project = await RoslynExtensions.LoadExistingProjectAsync(csProjFile.FullName, response);
                    if (project?.Documents.Any() == true) return project;
                }

                response?.LogInformation("Direct project load failed. Attempting adhoc project creation...");
            } else {
                response?.LogInformation("Attempting adhoc project creation...");
            }

            project = TryLoadFallbackAdhocProject(projectBasePath, contextFolder, modelsFolder, response);
            return project;
        } catch (Exception ex) {
            response?.LogError($"Error loading project: {ex.Message}");
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
        var start = DateTimeExtensions.GetTime();
        var csProj = InspectProject(projectBasePath, response);

        if (csProj?.ImplicitUsings.In("enabled", "enable", "true") == true) {
            // symbol renaming will likely not work correctly.
            response?.LogInformation(
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
            response?.LogError("No .cs files found");
            return null;
        }

        var thisAssembly = typeof(RuleApplicator).Assembly;
        var thisAssemblyReferences = thisAssembly.GetReferencedAssemblies().Select(o => o.Name).ToHashSet();

        var refAssemblies = new HashSet<Assembly>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies) {
            if (assembly.IsDynamic) continue;
            var assemblyName = assembly.GetName();
            var name = assemblyName.Name;
            if (name.StartsWith("Microsoft.CodeAnalysis")) continue;
            if (name.StartsWith("MinVer")) continue;
            if (name.StartsWith("JetBrains")) continue;
            if (name.StartsWith("ReSharper")) continue;
            if (name.StartsWith("xunit")) continue;
            if (name.NotIn("System.Private.CoreLib", "netstandard"))
                if (!thisAssemblyReferences.Contains(name))
                    continue;
            refAssemblies.Add(assembly);
        }

        try {
            using var workspace = cSharpFiles
                .GetWorkspaceForFilePaths(refAssemblies)
                .AddEntityResources();
            var project = workspace.CurrentSolution.Projects.First();
            if (project?.Documents.Any() == true) {
                // add project references?
                var elapsed = DateTimeExtensions.GetTime() - start;
                response?.LogInformation($"Loaded adhoc project in {elapsed}ms");
                return project;
            }
        } catch (Exception ex) {
            response?.LogError($"Unable to get in-memory project from workspace: {ex.Message}");
            return null;
        }

        return null;
    }

    private static CsProject InspectProject(string projectBasePath, LoggedResponse loggedResponse) {
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
                    loggedResponse.LogError($"Unable to parse csproj: {ex.Message}");
                    continue;
                }

                return csProj;
            }
        } catch (Exception ex) {
            loggedResponse.LogError($"Unable to read csproj: {ex.Message}");
        }

        return new CsProject();
    }

    internal sealed class RoslynProjectState {
        private readonly RuleApplicator applicator;

        public RoslynProjectState(RuleApplicator applicator) {
            this.applicator = applicator;
        }

        private bool loadAttempted = false;
        public Project OriginalProject { get; private set; }
        public Project Project { get; set; }

        internal async Task TryLoadProjectOrFallbackOnce(string projectBasePath, string contextFolder,
            string modelsFolder, ApplyRulesResponse response) {
            if (loadAttempted) return;
            loadAttempted = true;
            OriginalProject = Project = await applicator.TryLoadProjectOrFallback(projectBasePath, contextFolder,
                modelsFolder,
                response);
        }
    }

    internal sealed class ClassState {
        public ClassState(RoslynProjectState projectState) {
            ProjectState = projectState;
        }

        public RoslynProjectState ProjectState { get; }
        public Project Project => ProjectState.Project;
        public bool ClassExists => classSymbol != default;
        public string ClassFullName { get; private set; }

        private bool classSymbolStale = false;
        private INamedTypeSymbol classSymbol;


        public INamedTypeSymbol ClassSymbol {
            set {
                if (SymbolEqualityComparer.Default.Equals(classSymbol, value)) return;
                classSymbol = value;
                OnClassStyleChanged();
            }
        }

        private void OnClassStyleChanged() {
            ClassFullName = classSymbol?.GetFullName();
        }

        public async ValueTask<INamedTypeSymbol> GetClassSymbol() {
            if (!classSymbolStale || classSymbol == null) return classSymbol;

            // update it from current project
            var classSymbolTask = classSymbol.CurrentFrom(Project);
            classSymbol = classSymbolTask.IsCompletedSuccessfully ? classSymbolTask.Result : await classSymbolTask;
            classSymbolStale = false;
            return classSymbol;
        }

        public void MarkClassStale() {
            classSymbolStale = true;
        }

        public IEnumerable<DocumentId> GetDocumentIds() {
            if (classSymbol == null || Project == null) yield break;
            foreach (var document in classSymbol.GetDocuments(Project)) {
                if (document == null) continue;
                yield return document.Id;
            }
        }
    }
}

public sealed class LoadRulesResponse : LoggedResponse {
    public List<IEdmxRuleModelRoot> Rules { get; } = new();
}

public sealed class ApplyRulesResponse : LoggedResponse {
    internal ApplyRulesResponse(IEdmxRuleModelRoot ruleModelRoot) {
        Rule = ruleModelRoot;
    }

    public IEdmxRuleModelRoot Rule { get; internal set; }
}

[SuppressMessage("ReSharper", "ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator")]
public sealed class LoadAndApplyRulesResponse : LoggedResponse {
    public LoadRulesResponse LoadRulesResponse { get; internal set; }
    public IReadOnlyList<ApplyRulesResponse> ApplyRulesResponses { get; internal set; }

    /// <summary> return all errors in this response (including rule application responses) </summary>
    public IEnumerable<string> GetErrors() => GetAllMessagesOfType(LogType.Error);

    /// <summary> return all information statements in this response (including rule application responses) </summary>
    public IEnumerable<string> GetInformation() => GetAllMessagesOfType(LogType.Information);

    /// <summary> return all information statements in this response (including rule application responses) </summary>
    public IEnumerable<string> GetWarnings() => GetAllMessagesOfType(LogType.Warning);

    /// <summary> return all errors in this response (including rule application responses) </summary>
    public IEnumerable<string> GetAllMessagesOfType(LogType type) {
        if (LoadRulesResponse?.Errors != null)
            foreach (var logMessage in LoadRulesResponse.Messages.Where(o => o.Type == type))
                yield return logMessage.Message;
        if (!(ApplyRulesResponses?.Count > 0)) yield break;
        foreach (var r in ApplyRulesResponses)
            if (r?.Messages != null)
                foreach (var logMessage in r.Messages.Where(o => o.Type == type))
                    yield return logMessage.Message;
    }
}
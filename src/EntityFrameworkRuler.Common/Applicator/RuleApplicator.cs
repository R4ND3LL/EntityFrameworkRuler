using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Project = Microsoft.CodeAnalysis.Project;
using RuleLoader = EntityFrameworkRuler.Loader.RuleLoader;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal
[assembly: CLSCompliant(false)]

namespace EntityFrameworkRuler.Applicator;

public sealed class RuleApplicator : RuleHandler, IRuleApplicator {
    /// <summary> Create rule applicator for making changes to project files </summary>
    public RuleApplicator() : this(null, null) { }

    /// <summary> Create rule applicator for making changes to project files </summary>
    [ActivatorUtilitiesConstructor]
    public RuleApplicator(IRuleLoader loader, IMessageLogger logger) : base(logger) {
        Loader = loader;
    }

    #region properties

    public IRuleLoader Loader { get; set; }

    #endregion

    /// <inheritdoc />
    public Task<LoadRulesResponse> LoadRulesInProjectPath(string projectBasePath, string dbContextRulesFile = null) {
        return LoadRulesInProjectPath(new LoadOptions(projectBasePath: projectBasePath, dbContextRulesFile: dbContextRulesFile));
    }

    /// <inheritdoc />
    public Task<LoadRulesResponse> LoadRulesInProjectPath(ILoadOptions request = null) {
        var loader = Loader ?? new RuleLoader();
        return loader.LoadRulesInProjectPath(request);
    }

    /// <inheritdoc />
    public Task<LoadAndApplyRulesResponse> ApplyRulesInProjectPath(string projectBasePath, bool adhocOnly = false) {
        return ApplyRulesInProjectPath(new LoadAndApplyOptions(projectBasePath, adhocOnly));
    }

    /// <inheritdoc />
    public async Task<LoadAndApplyRulesResponse> ApplyRulesInProjectPath(LoadAndApplyOptions request) {
        var loader = Loader ?? new RuleLoader();
        loader.Log += OnResponseLog;
        var loadRulesResponse = await LoadRulesInProjectPath(request);
        var response = new LoadAndApplyRulesResponse {
            LoadRulesResponse = loadRulesResponse
        };
        try {
            if (response.LoadRulesResponse?.Rules != null)
                response.ApplyRulesResponses = await ApplyRulesInternal(new(request.ProjectBasePath, request.AdhocOnly,
                    response.LoadRulesResponse.Rules.ToArray()));
            return response;
        } finally {
            loader.Log -= OnResponseLog;
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ApplyRulesResponse>> ApplyRules(string projectBasePath, IRuleModelRoot rule) {
        var options = new ApplicatorOptions(projectBasePath, false);
        if (rule != null) options.Rules.Add(rule);
        return ApplyRules(options);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ApplyRulesResponse>> ApplyRules(string projectBasePath, IRuleModelRoot rule, params IRuleModelRoot[] rules) {
        var options = new ApplicatorOptions(projectBasePath, false);
        if (rule != null) options.Rules.Add(rule);
        if (rules != null) options.Rules.AddRange(rules);
        return ApplyRules(options);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ApplyRulesResponse>> ApplyRules(string projectBasePath, bool adhocOnly, params IRuleModelRoot[] rules) {
        return ApplyRules(new ApplicatorOptions(projectBasePath, adhocOnly, rules));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApplyRulesResponse>> ApplyRules(ApplicatorOptions request) {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var responses = await ApplyRulesInternal(request);
        return responses;
    }

    /// <summary> Apply the given rules to the target project. </summary>
    private async Task<IReadOnlyList<ApplyRulesResponse>> ApplyRulesInternal(ApplicatorOptions request) {
        if (request?.Rules == null) throw new ArgumentNullException(nameof(request));

        var state = new RoslynProjectState(this);
        List<ApplyRulesResponse> responses = new();
        foreach (var rule in request.Rules.Where(o => o != null).OrderBy(o => o.Kind)) {
            ApplyRulesResponse response = null;
            try {
                if (rule == null) continue;

                response = new ApplyRulesResponse(rule, Logger);
                response.Log += OnResponseLog;
                try {
                    switch (rule) {
                        case DbContextRule dbContextRule:
                            await ApplyDbContextRulesCore(request, dbContextRule, response, state);
                            break;
                        default:
                            continue;
                    }
                } finally {
                    response.Log -= OnResponseLog;
                }

                responses.Add(response);
            } catch (Exception ex) {
                Console.WriteLine(ex);
                response ??= new(rule, Logger);
                response.GetInternals().LogError($"Error processing {rule}: {ex.Message}");
                responses.Add(response);
            }
        }

        return responses;
    }


    /// <summary> Apply the given rules to the target project. </summary>
    /// <param name="request"> The request object. Required. </param>
    /// <param name="dbContextRule"> The rule to apply. </param>
    /// <returns> ApplyRulesResponse </returns>
    public async Task<ApplyRulesResponse> ApplyRules(ApplicatorOptions request, DbContextRule dbContextRule) {
        // map to class renaming
        var response = new ApplyRulesResponse(dbContextRule, Logger);
        response.Log += OnResponseLog;
        try {
            var state = new RoslynProjectState(this);
            await ApplyDbContextRulesCore(request, dbContextRule, response, state);
            return response;
        } finally {
            response.Log -= OnResponseLog;
        }
    }

    private async Task ApplyDbContextRulesCore(ApplicatorOptions request, DbContextRule dbContextRule, ApplyRulesResponse response,
        RoslynProjectState state) {
        foreach (var schema in dbContextRule.Schemas) {
            var schemaResponse = new ApplyRulesResponse(null, Logger);
            schemaResponse.Log += OnResponseLog;
            try {
                await ApplyRulesCore(request, schema.Tables, schema.Namespace, schemaResponse, state);
                response.GetInternals().Merge(schemaResponse);
            } finally {
                schemaResponse.Log -= OnResponseLog;
            }
        }
    }

    /// <summary> Apply the given rules to the target project. </summary>
    /// <param name="request"></param>
    /// <param name="classRules"> The rules to apply. </param>
    /// <param name="namespaceName"></param>
    /// <param name="response"> The response to fill. </param>
    /// <param name="state"> Roslyn project state. Internal use only. </param>
    /// <returns></returns>
    private async Task ApplyRulesCore(ApplicatorOptions request, IEnumerable<IClassRule> classRules, string namespaceName,
        ApplyRulesResponse response, RoslynProjectState state = null) {
        string ToFullClassName(string className) {
            return namespaceName.HasNonWhiteSpace() ? $"{namespaceName}.{className}" : className;
        }

        if (classRules == null) return; // nothing to do

        state ??= new(this);
        await state.TryLoadProjectOrFallbackOnce(request, response);
        if (state.Project?.Documents.Any() != true) return;
        var responseInternal = response.GetInternals();
        var propRenameCount = 0;
        var classRenameCount = 0;
        var typeMapCount = 0;
        var dirtyClassStates = new HashSet<ClassState>();
        foreach (var classRule in classRules) {
            var newClassName = classRule.GetNewName();
            var oldClassName = classRule.GetExpectedEntityFrameworkName().NullIfEmpty() ?? newClassName;
            newClassName = newClassName.CoalesceWhiteSpace(oldClassName);
            if (newClassName.IsNullOrWhiteSpace()) continue; // invalid entry

            // first rename class if needed
            var canRenameClass = newClassName.HasNonWhiteSpace() && newClassName != oldClassName;
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
                    responseInternal.LogInformation($"Renamed class {oldClassName} to {newClassName}");
                } else {
                    // property processing may still work if the class is found under the new name
                    classState.ClassSymbol = (await state.Project.FindClassesByName(namespaceName, newClassName))
                        .FirstOrDefault();
                    if (classState.ClassExists) {
                        responseInternal.LogInformation(
                            $"Class already exists with target name: {ToFullClassName(newClassName)}");
                    } else {
                        responseInternal.LogInformation($"Could not find class {ToFullClassName(oldClassName)}");
                        continue;
                    }
                }
            } else {
                classState.ClassSymbol =
                    (await state.Project.FindClassesByName(namespaceName, newClassName)).FirstOrDefault();
            }

            if (!classState.ClassExists) {
                responseInternal.LogInformation($"Could not find class {ToFullClassName(newClassName)}");
                continue;
            }

            void ProjectUpdated(PropertyActionResult docWithChange) {
                Debug.Assert(docWithChange.PropertyLocated);
                state.Project = docWithChange.Project;
                classState.MarkClassStale();
                dirtyClassStates.Add(classState);
            }


            // process property changes
            foreach (var propertyRef in classRule.GetProperties()) {
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
                        responseInternal.LogInformation(
                            $"Renamed property {newClassName}.{fromNames[i]} to {newPropName}");
                    } else {
                        // further processing may still work if the property is found under the new name
                        var currentClassSymbol = await classState.GetClassSymbol();
                        propertyExists = await currentClassSymbol.PropertyExists(newPropName);
                        if (propertyExists.Value)
                            responseInternal.LogInformation(
                                $"Property already exists with target name: {newClassName}.{newPropName}");
                        else {
                            responseInternal.LogInformation(
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
                    responseInternal.LogInformation($"Could not find property {ToFullClassName(newClassName)}.{newPropName}");
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
                        responseInternal.LogInformation(
                            $"Updated property {newClassName}.{currentNames[i]} type to {newType}");
                    } else {
                        // should not arrive here because logic above confirms that the property exists
#if DEBUG
                        if (Debugger.IsAttached) Debugger.Break();
#endif
                        responseInternal.LogInformation(
                            $"Could not find property {ToFullClassName(newClassName)}.{string.Join(", ", currentNames)}");
                    }
                }
            }

            //state.Project = classState.Project;
        }

        if (classRenameCount == 0 && propRenameCount == 0 && typeMapCount == 0) {
            responseInternal.LogInformation("No changes made");
            return;
        }

        int saved;
        if (classRenameCount > 0 || propRenameCount > 0) {
            // perform a diff on the documents to find changes.
            var changes = state.Project.GetChangedDocuments(state.OriginalProject);
            saved = changes?.Count > 0 ? await changes.SaveDocumentsAsync() : 0;
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
        responseInternal.LogInformation(sb.ToString());
        return;
    }

    private async Task<Project> TryLoadProjectOrFallback(ApplicatorOptions request, ApplyRulesResponse response) {
        if (request == null || request.ProjectBasePath.IsNullOrEmpty()) return null;
        try {
            var csProjFiles = request.ProjectBasePath.FindCsProjFiles();

            Project project;
            if (!request.AdhocOnly) {
                foreach (var csProjFile in csProjFiles) {
                    if (csProjFile.Directory != null) request.ProjectBasePath = csProjFile.Directory.FullName;
                    project = await RoslynExtensions.LoadExistingProjectAsync(csProjFile.FullName, response);
                    if (project?.Documents.Any() == true) return project;
                }

                response?.GetInternals().LogInformation("Direct project load failed. Attempting adhoc project creation...");
            } else {
                response?.GetInternals().LogInformation("Attempting adhoc project creation...");
            }

            project = TryLoadFallbackAdhocProject(request, response);
            return project;
        } catch (Exception ex) {
            response?.GetInternals().LogError($"Error loading project: {ex.Message}");
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
    /// <param name="request"></param>
    /// <param name="response"> Errors list. </param>
    /// <returns></returns>
    private static Project TryLoadFallbackAdhocProject(ApplicatorOptions request, ApplyRulesResponse response) {
        var start = DateTimeExtensions.GetTime();
        var path = request.ProjectBasePath;
        var csProj = path.InspectProject(response);
        var responseInternal = response?.GetInternals();

        if (csProj?.ImplicitUsings.In("enabled", "enable", "true") == true) {
            // symbol renaming will likely not work correctly.
            responseInternal?.LogInformation(
                "WARNING: ImplicitUsings is enabled on this project. Symbol renaming may not fully work due to missing reference information.");
        }

        if (csProj?.File?.Directory?.FullName != null) path = csProj.File.Directory.FullName;

        var cSharpFolders = new HashSet<string>();
        if (request.ContextFolder?.Length > 0 && Directory.Exists(Path.Combine(path, request.ContextFolder)))
            cSharpFolders.Add(Path.Combine(path, request.ContextFolder));

        if (request.ModelsFolder?.Length > 0 && Directory.Exists(Path.Combine(path, request.ModelsFolder)))
            cSharpFolders.Add(Path.Combine(path, request.ModelsFolder));

        if (cSharpFolders.Count == 0)
            // use project base path
            cSharpFolders.Add(path);

        var cSharpFiles = cSharpFolders
            .SelectMany(o => Directory.GetFiles(o, "*.cs", SearchOption.AllDirectories))
            .Distinct().ToList();

        var ignorePaths = new HashSet<string> {
            Path.Combine(path, "obj") + Path.DirectorySeparatorChar,
            Path.Combine(path, "Debug") + Path.DirectorySeparatorChar,
        };
        var toIgnore = cSharpFiles.Where(o => ignorePaths.Any(o.StartsWithIgnoreCase)).ToList();
        toIgnore.ForEach(o => cSharpFiles.Remove(o));

        if (cSharpFiles.Count == 0) {
            responseInternal?.LogError("No .cs files found");
            return null;
        }

        var thisAssembly = typeof(RuleApplicator).Assembly;
        var thisAssemblyReferences = thisAssembly.GetReferencedAssemblies().Select(o => o.Name).ToHashSetNew(StringComparer.OrdinalIgnoreCase);

        var refAssemblies = new HashSet<Assembly>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies) {
            if (assembly.IsDynamic) continue;
            var assemblyName = assembly.GetName();
            var name = assemblyName.Name;
            if (name.IsNullOrEmpty()) continue;
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
            var project = workspace.CurrentSolution.Projects.FirstOrDefault();
            if (project?.Documents.Any() == true) {
                // add project references?
                var elapsed = DateTimeExtensions.GetTime() - start;
                responseInternal?.LogInformation($"Loaded adhoc project in {elapsed}ms");
                return project;
            }
        } catch (Exception ex) {
            responseInternal?.LogError($"Unable to get in-memory project from workspace: {ex.Message}");
            return null;
        }

        return null;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public sealed class RoslynProjectState {
        private readonly RuleApplicator applicator;

        public RoslynProjectState(RuleApplicator applicator) {
            this.applicator = applicator;
        }

        private bool loadAttempted = false;

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public Project OriginalProject { get; private set; }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public Project Project { get; set; }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public async Task TryLoadProjectOrFallbackOnce(ApplicatorOptions request, ApplyRulesResponse response) {
            if (loadAttempted) return;
            loadAttempted = true;
            OriginalProject = Project = await applicator.TryLoadProjectOrFallback(request, response);
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

        private bool classSymbolStale;
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
            classSymbol = classSymbolTask.IsCompleted && !classSymbolTask.IsCanceled && !classSymbolTask.IsFaulted
                ? classSymbolTask.Result
                : await classSymbolTask;
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

public sealed class ApplyRulesResponse : LoggedResponse {
    public ApplyRulesResponse(IRuleModelRoot ruleModelRoot, IMessageLogger logger) : base(logger) {
        Rule = ruleModelRoot;
    }

    public IRuleModelRoot Rule { get; internal set; }

    /// <inheritdoc />
    public override bool Success => base.Success && Rule != null;
}

[SuppressMessage("ReSharper", "ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator")]
public sealed class LoadAndApplyRulesResponse : ILoggedResponse {
    public LoadRulesResponse LoadRulesResponse { get; internal set; }
    public IReadOnlyList<ApplyRulesResponse> ApplyRulesResponses { get; internal set; }

    /// <summary> return all errors in this response (including rule application responses) </summary>
    public IEnumerable<string> Errors => GetAllMessagesOfType(LogType.Error);

    /// <summary> return all information statements in this response (including rule application responses) </summary>
    public IEnumerable<string> Information => GetAllMessagesOfType(LogType.Information);

    /// <summary> return all information statements in this response (including rule application responses) </summary>
    public IEnumerable<string> Warnings => GetAllMessagesOfType(LogType.Warning);

    /// <summary> return all errors in this response (including rule application responses) </summary>
    public IEnumerable<string> GetAllMessagesOfType(LogType type) {
        foreach (var logMessage in GetMessages().Where(o => o.Type == type))
            yield return logMessage.Message;
    }

    /// <summary> return all errors in this response (including rule application responses) </summary>
    public IEnumerable<LogMessage> GetMessages() {
        if (LoadRulesResponse?.Errors != null)
            foreach (var logMessage in LoadRulesResponse.Messages)
                yield return logMessage;
        if (!(ApplyRulesResponses?.Count > 0)) yield break;
        foreach (var r in ApplyRulesResponses)
            if (r?.Messages != null)
                foreach (var logMessage in r.Messages)
                    yield return logMessage;
    }

    /// <inheritdoc />
    public bool HasErrors => Errors.Any();

    public IMessageLogger Logger { get => null; set { } }

    /// <inheritdoc />
    public bool Success => LoadRulesResponse?.Success == true && ApplyRulesResponses?.All(o => o.Success) == true;
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EdmxRuler.Extensions;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Project = Microsoft.CodeAnalysis.Project;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace EdmxRuler.Applicator;

internal static class RoslynExtensions {
    private static VisualStudioInstance vsInstance;

    public static async Task<Document> RenameClassAsync(
        this IEnumerable<Document> documents,
        string namespaceName,
        string oldClassName,
        string newClassName,
        bool renameOverloads = false,
        bool renameInStrings = false,
        bool renameInComments = false,
        bool renameFile = false) {
        if (string.IsNullOrEmpty(oldClassName) || string.IsNullOrEmpty(newClassName) ||
            oldClassName == newClassName) return null;

        async Task<Document> Action(Document document, SyntaxNode root, TypeDeclarationSyntax classSyntax,
            ISymbol classSymbol) {
            // rename all references to the property
            if (classSymbol.Name == newClassName) {
                // name already matches target.
                return document;
            }

            var newSolution = await Renamer.RenameSymbolAsync(
                document.Project.Solution,
                classSymbol,
                new SymbolRenameOptions(renameOverloads, renameInStrings, renameInComments, renameFile),
                newClassName);

            // sln has been revised. return new doc
            var currentDocument = newSolution.GetDocument(document.Id);
            return currentDocument;
        }

        return await LocateAndActOnClass(documents, namespaceName, oldClassName, Action);
    }

    public static async Task<bool> ClassExists(
        this IEnumerable<Document> documents,
        string namespaceName,
        string oldClassName) {
        if (string.IsNullOrEmpty(oldClassName)) return false;

        Task<Document> Action(Document document, SyntaxNode root, TypeDeclarationSyntax classSyntax,
            ISymbol classSymbol) {
            return Task.FromResult(document);
        }

        var doc = await LocateAndActOnClass(documents, namespaceName, oldClassName, Action);
        return doc != null;
    }

    public static async Task<Document> RenamePropertyAsync(
        this IEnumerable<Document> documents,
        string namespaceName,
        string className,
        string oldPropertyName,
        string newPropertyName,
        bool renameOverloads = false,
        bool renameInStrings = false,
        bool renameInComments = false,
        bool renameFile = false) {
        if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(oldPropertyName) ||
            string.IsNullOrEmpty(newPropertyName) || oldPropertyName == newPropertyName)
            return null;

        async Task<Document> Action(Document document, SyntaxNode root, PropertyDeclarationSyntax propSyntax,
            ISymbol propSymbol) {
            // rename all references to the property
            if (propSymbol.Name == newPropertyName) {
                // name already matches target.
                return document;
            }

            var newSolution = await Renamer.RenameSymbolAsync(
                document.Project.Solution,
                propSymbol,
                new SymbolRenameOptions(renameOverloads, renameInStrings, renameInComments, renameFile),
                newPropertyName);

            // sln has been revised. return new doc
            var currentDocument = newSolution.GetDocument(document.Id);
            return currentDocument;
        }

        return await LocateAndActOnProperty(documents, namespaceName, className, oldPropertyName, Action);
    }

    public static async Task<bool> PropertyExists(this IEnumerable<Document> documents, string namespaceName,
        string className, string propertyName) {
        if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(propertyName))
            return false;

        Task<Document> Action(Document document, SyntaxNode root, PropertyDeclarationSyntax propSyntax,
            ISymbol propSymbol) {
            return Task.FromResult(document);
        }

        var doc = await LocateAndActOnProperty(documents, namespaceName, className, propertyName, Action);
        return doc != null;
    }

    public static async Task<Document> ChangePropertyTypeAsync(
        this IEnumerable<Document> documents, string namespaceName,
        string className,
        string propertyName,
        string newTypeName) {
        if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(propertyName) ||
            string.IsNullOrEmpty(newTypeName))
            return null;

        Task<Document> Action(Document document, SyntaxNode root, PropertyDeclarationSyntax propSyntax,
            ISymbol propSymbol) {
            // change type
            var currentTypeText = propSyntax.Type?.ToString()?.Trim();
            var nullable = string.Empty;
            if (currentTypeText != null && (currentTypeText.StartsWith("Nullable<") || currentTypeText.EndsWith("?"))) {
                nullable = "?";
            }

            var newType = SyntaxFactory.ParseTypeName($"{newTypeName}{nullable} ");
            var updatedProp = propSyntax.WithType(newType);
            var newRoot = root!.ReplaceNode(propSyntax, updatedProp);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return Task.FromResult(newDocument);
        }

        return await LocateAndActOnProperty(documents, namespaceName, className, propertyName, Action);
    }

    private static async Task<Document> LocateAndActOnClass(this IEnumerable<Document> documents,
        string namespaceName, string className,
        Func<Document, SyntaxNode, TypeDeclarationSyntax, ISymbol, Task<Document>> action) {
        if (string.IsNullOrEmpty(className) || action == null) return null;

        foreach (var document in documents) {
            var root = await document.GetSyntaxRootAsync();
            var classSyntax = root?.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>()
                .FirstOrDefault(o => o.Identifier.ToString() == className);
            // ReSharper disable once UseNullPropagation
            if (classSyntax is null) continue; // not found in this doc

            // found it
            var model = await document.GetSemanticModelAsync();

            var classSymbol = model?.GetDeclaredSymbol(classSyntax) ?? throw new Exception("Class symbol not found");

            if (!classSymbol.IsNamespaceMatch(namespaceName)) continue; // not the correct namespace

            // perform action and return new document
            var newDocument = await action(document, root, classSyntax, classSymbol);
            Debug.Assert(newDocument != null);
            return newDocument;
        }

        return null;
    }


    private static async Task<Document> LocateAndActOnProperty(this IEnumerable<Document> documents,
        string namespaceName, string className,
        string oldPropertyName, Func<Document, SyntaxNode, PropertyDeclarationSyntax, ISymbol, Task<Document>> action) {
        if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(oldPropertyName) || action == null) return null;

        foreach (var document in documents) {
            var root = await document.GetSyntaxRootAsync();
            var classSyntax = root?.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>()
                .FirstOrDefault(o => o.Identifier.ToString() == className);
            // ReSharper disable once UseNullPropagation
            if (classSyntax is null) continue; // not found in this doc

            // now find property
            var propSyntax = classSyntax.DescendantNodesAndSelf().OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(o => o.Identifier.Text == oldPropertyName);
            if (propSyntax is null) continue; // not found in this class

            // found it
            var model = await document.GetSemanticModelAsync();

            var propSymbol = model?.GetDeclaredSymbol(propSyntax) ?? throw new Exception("Property symbol not found");

            if (!propSymbol.IsNamespaceMatch(namespaceName)) continue; // not the correct namespace

            // perform action and return new document
            var newDocument = await action(document, root, propSyntax, propSymbol);
            Debug.Assert(newDocument != null);
            return newDocument;
        }

        return null;
    }

    /// <summary> return true if the namespace is empty (effectively dont care to match) or the symbol's containing namespace is a match </summary>
    private static bool IsNamespaceMatch(this ISymbol classSymbol, string namespaceName) {
        if (namespaceName.IsNullOrEmpty()) return true;
        var ns = classSymbol.ContainingNamespace;
        var symbolDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
        var fullyQualifiedName = ns.ToDisplayString(symbolDisplayFormat);
        return fullyQualifiedName == namespaceName;
    }

    public static async Task<int> SaveDocumentsAsync(this IEnumerable<Document> documents) {
        var saveCount = 0;
        foreach (var document in documents) {
            var path = document.FilePath;
            if (path == null) continue;
            var text = string.Join(
                Environment.NewLine,
                (await document.GetTextAsync()).Lines.Select(o => o.ToString())).Trim();
            var orig = (await File.ReadAllTextAsync(path, Encoding.UTF8))?.Trim();
            if (text == orig) continue;

            await File.WriteAllTextAsync(path, text, Encoding.UTF8);
            saveCount++;
        }

        return saveCount;
    }

    public static async Task<Project> LoadExistingProjectAsync(string csProjPath, List<string> errors = null) {
        try {
            vsInstance ??= MSBuildLocatorRegisterDefaults();
            Debug.WriteLine($"Using msbuild: {vsInstance.MSBuildPath}");
            using var workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += (_, failure) => Debug.WriteLine(failure.Diagnostic);
            var project = await workspace.OpenProjectAsync(csProjPath);
            var docs = project.Documents.ToArray();
            var diagnostics = workspace.Diagnostics;
            foreach (var diagnostic in diagnostics)
                if (diagnostic.Kind == WorkspaceDiagnosticKind.Failure) {
                    errors?.Add($"Error loading existing project: {diagnostic.Message}");
                    return null;
                }

            Debug.Assert(docs.Length > 0);
            return project;
        } catch (Exception ex) {
            errors?.Add($"Error loading existing project: {ex.Message}");
            return null;
        }
    }

    private static VisualStudioInstance MSBuildLocatorRegisterDefaults() {
        // override default behavior using using reflection to get the VS instances list and register the LATEST version of VS  
        try {
            var instances = MSBuildLocator.QueryVisualStudioInstances(VisualStudioInstanceQueryOptions.Default)
                .OrderByDescending(o => o.Version).ToArray();
            if (instances.Length > 0) {
                var latest = instances.FirstOrDefault();

                latest = (VisualStudioInstance)Activator.CreateInstance(
                    typeof(VisualStudioInstance),
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new object[] { latest.Name, latest.MSBuildPath + "\\", latest.Version, latest.DiscoveryType },
                    null,
                    null)!;

                MSBuildLocator.RegisterInstance(latest);
                return latest;
            }

            return MSBuildLocator.RegisterDefaults();
        } catch {
            // ignored
            return null;
        }
    }

    public static AdhocWorkspace GetWorkspaceForFilePaths(
        this IEnumerable<string> filePaths,
        IEnumerable<Assembly> projReferences = null) {
        var host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
        var ws = new AdhocWorkspace(host);
        var refAssemblies = new HashSet<Assembly>();
        if (projReferences != null) refAssemblies.AddRange(projReferences);
        refAssemblies.Add(typeof(object).Assembly);
        var references = refAssemblies.Select(o => MetadataReference.CreateFromFile(o.Location)).ToList();

        var projInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            "MyProject",
            "MyAssembly",
            LanguageNames.CSharp,
            metadataReferences: references);

        var projectId = ws.AddProject(projInfo).Id;

        foreach (var filePath in filePaths) {
            var info = new FileInfo(filePath);
            var content = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(content)) continue;

            var text = SourceText.From(content);
            var documentInfo = DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId),
                    info.Name,
                    null,
                    SourceCodeKind.Regular,
                    TextLoader.From(TextAndVersion.Create(text, VersionStamp.Default, info.FullName)))
                .WithFilePath(info.FullName);
            ws.AddDocument(documentInfo);
        }

        return ws;
    }

    public static AdhocWorkspace AddEntityResources(this AdhocWorkspace ws) {
        var projectId = ws.CurrentSolution.ProjectIds.First();
        // load from resources
        var resources = typeof(RoslynExtensions).Assembly.GetEntityResourceDocuments().ToList();
        for (var i = 0; i < resources.Count; i++) {
            var content = resources[i];
            if (string.IsNullOrEmpty(content)) continue;
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, content);
            var info = new FileInfo(filePath);
            var text = SourceText.From(content);
            var documentInfo = DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId),
                    info.Name, //$"EntityDependencyDoc{i++}.cs",
                    null,
                    SourceCodeKind.Regular,
                    TextLoader.From(TextAndVersion.Create(text, VersionStamp.Default)))
                .WithFilePath(info.FullName);
            ws.AddDocument(documentInfo);
        }

        return ws;
    }
}
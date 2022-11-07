using System.Diagnostics;
using System.Reflection;
using System.Text;
using EdmxRuler.Extensions;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Project = Microsoft.CodeAnalysis.Project;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace EdmxRuler.Applicator;

internal static class RoslynExtensions {
    private static VisualStudioInstance vsInstance;

    public static async Task<Document> RenamePropertyAsync(
        this IEnumerable<Document> documents,
        string className,
        string oldPropertyName,
        string newPropertyName,
        bool renameOverloads = false,
        bool renameInStrings = false,
        bool renameInComments = false,
        bool renameFile = false) {
        if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(oldPropertyName) ||
            string.IsNullOrEmpty(newPropertyName))
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

        return await LocateAndActOnProperty(documents, className, oldPropertyName, Action);
    }

    public static async Task<Document> ChangePropertyTypeAsync(
        this IEnumerable<Document> documents,
        string className,
        string propertyName,
        string newTypeName) {
        if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(propertyName) ||
            string.IsNullOrEmpty(newTypeName))
            return null;

        Task<Document> Action(Document document, SyntaxNode root, PropertyDeclarationSyntax propSyntax,
            ISymbol propSymbol) {
            // change type
            var newType = SyntaxFactory.ParseTypeName($"{newTypeName} ");
            var updatedProp = propSyntax.WithType(newType);
            var newRoot = root!.ReplaceNode(propSyntax, updatedProp);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return Task.FromResult(newDocument);
        }

        return await LocateAndActOnProperty(documents, className, propertyName, Action);
    }

    private static async Task<Document> LocateAndActOnProperty(this IEnumerable<Document> documents, string className,
        string oldPropertyName, Func<Document, SyntaxNode, PropertyDeclarationSyntax, ISymbol, Task<Document>> action) {
        if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(oldPropertyName) || action == null) return null;

        foreach (var document in documents) {
            var root = await document.GetSyntaxRootAsync();
            var classSyntax = root?.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>()
                .FirstOrDefault(o => o.Identifier.ToString() == className);
            if (classSyntax == null) continue; // not found in this doc

            // now find property
            var propSyntax = classSyntax.DescendantNodesAndSelf().OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(o => o.Identifier.Text == oldPropertyName);
            if (propSyntax == null) continue; // not found in this class

            // found it
            var model = await document.GetSemanticModelAsync();
            var propSymbol = model?.GetDeclaredSymbol(propSyntax) ?? throw new Exception("Property symbol not found");

            // perform action and return new document
            var newDocument = await action(document, root, propSyntax, propSymbol);
            Debug.Assert(newDocument != null);
            return newDocument;
        }

        return null;
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
                if (diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                    throw new Exception(diagnostic.Message);

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
        IEnumerable<MetadataReference> projReferences = null) {
        var ws = new AdhocWorkspace();
        var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var references = new List<MetadataReference>() { mscorlib };
        if (projReferences != null) references.AddRange(projReferences);

        var projInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            "MyProject",
            "MyAssembly",
            "C#",
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

        // load from resources
        var resources = typeof(RoslynExtensions).Assembly.GetEntityResourceDocuments().ToList();
        for (var i = 0; i < resources.Count; i++) {
            var content = resources[i];
            if (string.IsNullOrEmpty(content)) continue;

            var text = SourceText.From(content);
            var documentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(projectId),
                $"EntityDependencyDoc{i++}.cs",
                null,
                SourceCodeKind.Regular,
                TextLoader.From(TextAndVersion.Create(text, VersionStamp.Default)));
            ws.AddDocument(documentInfo);
        }

        return ws;
    }
}
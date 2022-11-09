using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EdmxRuler.Extensions;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using CSharpSyntaxNode = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxNode;
using Document = Microsoft.CodeAnalysis.Document;
using Project = Microsoft.CodeAnalysis.Project;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace EdmxRuler.Applicator;

public struct PropertyActionResult {
    public Project Project { get; set; }

    //public Document Document { get; set; }
    //public bool ClassLocated => ClassSymbol != null;
    public bool PropertyLocated => PropertySyntax != null;

    //public INamedTypeSymbol ClassSymbol { get; set; }
    public PropertyDeclarationSyntax PropertySyntax { get; set; }
}

public struct ClassActionResult {
    public Project Project { get; set; }
    public bool ClassLocated => ClassSymbol != null; //{ get; set; }
    public INamedTypeSymbol ClassSymbol { get; set; }
}

public enum LocationResult {
    Found, NotFound
}

internal static class RoslynExtensions {
    private static VisualStudioInstance vsInstance;
#if DEBUG
    public static uint FindClassesByNameTime;
    public static uint RenameClassAsyncTime;
    public static uint RenamePropertyAsyncTime;
    public static uint ChangePropertyTypeAsyncTime;
#endif

    public static async Task<ClassActionResult> RenameClassAsync(
        this Project project,
        string namespaceName,
        string className,
        string newClassName,
        bool renameOverloads = false,
        bool renameInStrings = false,
        bool renameInComments = false,
        bool renameFile = false) {
        if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(newClassName) ||
            className == newClassName) return new ClassActionResult { Project = project };

        var classes = await FindClassesByName(project, namespaceName, className);
        foreach (var classSymbol in classes) {
            // perform action and return new project
            // rename all references to the property
            if (classSymbol.Name == newClassName) {
                // name already matches target.
                return new ClassActionResult { Project = project, ClassSymbol = classSymbol };
            }

            var start = DateTimeExtensions.GetTime();
            var newSolution = await Renamer.RenameSymbolAsync(
                project.Solution,
                classSymbol,
                new SymbolRenameOptions(renameOverloads, renameInStrings, renameInComments, renameFile),
                newClassName);
            RenameClassAsyncTime += DateTimeExtensions.GetTime() - start;

            // sln has been revised. return new doc
            project = newSolution.Projects.Single(o => o.Id == project.Id);
            var ns = classSymbol.ContainingNamespace.ToDisplayString();
            var fullName = $"{ns}.{newClassName}";
            var newSymbol = await FindClassByName(project, fullName);
            Debug.Assert(fullName != null);
            return new ClassActionResult { Project = project, ClassSymbol = newSymbol };
        }

        return new ClassActionResult { Project = project };
    }

    public static async Task<bool> ClassExists(this Project project, string namespaceName, string className) {
        var classes = await FindClassesByName(project, namespaceName, className);
        return classes.Count > 0;
    }

    public static async Task<IList<Document>> FindClassDocuments(this Project project, string namespaceName,
        string className) {
        if (string.IsNullOrEmpty(className)) return null;

        var classSymbols = await FindClassesByName(project, namespaceName, className);
        var docs = classSymbols.SelectMany(o => o.GetDocuments(project)).DistinctBy(o => o.Id).ToArray();
        return docs;
    }


    public static async Task<PropertyActionResult> RenamePropertyAsync(this Project project,
        INamedTypeSymbol classSymbol,
        string oldPropertyName, string newPropertyName, bool renameOverloads = false, bool renameInStrings = false,
        bool renameInComments = false, bool renameFile = false) {
        if (project == null) throw new ArgumentNullException(nameof(project));
        if (classSymbol == null) throw new ArgumentNullException(nameof(classSymbol));
        if (oldPropertyName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(oldPropertyName));
        if (newPropertyName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(newPropertyName));


        var (_, propSyntax) = await LocateProperty(classSymbol, oldPropertyName);
        if (propSyntax == null) return new PropertyActionResult { Project = project };

        if (oldPropertyName == newPropertyName) {
            // name already matches target.
            return new PropertyActionResult { Project = project, PropertySyntax = propSyntax };
        }

        // rename all references to the property
        var document = project.GetDocument(propSyntax.SyntaxTree);
        if (document == null) {
            // this indicates that given classSymbol does not match the project. 
            throw new Exception($"Class {classSymbol.GetFullName()} document could not be found");
        }

        var model = await document.GetSemanticModelAsync();
        var propSymbol = model?.GetDeclaredSymbol(propSyntax) ?? throw new Exception("Property symbol not found");

        if (propSymbol.Name == newPropertyName) {
            // name already matches target.
            return new PropertyActionResult { Project = project, PropertySyntax = propSyntax };
        }

        var start = DateTimeExtensions.GetTime();
        var newSolution = await Renamer.RenameSymbolAsync(
            project.Solution,
            propSymbol,
            new SymbolRenameOptions(renameOverloads, renameInStrings, renameInComments, renameFile),
            newPropertyName);
        RenamePropertyAsyncTime += DateTimeExtensions.GetTime() - start;


        // sln has been revised. return new doc
        var newDocument = newSolution.GetDocument(document.Id);
        // project = newSolution.Projects.Single(o => o.Id == project.Id);
        // var newClassSymbol = await CurrentFrom(classSymbol, project);
        Debug.Assert(newDocument != null, nameof(newDocument) + " != null");
        return new PropertyActionResult { Project = newDocument.Project, PropertySyntax = propSyntax };
    }

    public static async Task<PropertyActionResult> ChangePropertyTypeAsync(this Project project,
        INamedTypeSymbol classSymbol,
        string propertyName, string newTypeName) {
        if (project == null) throw new ArgumentNullException(nameof(project));
        if (classSymbol == null) throw new ArgumentNullException(nameof(classSymbol));
        if (propertyName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(propertyName));
        if (newTypeName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(newTypeName));

        var (_, propSyntax) = await LocateProperty(classSymbol, propertyName);
        if (propSyntax == null) return new PropertyActionResult { Project = project };

        // change type
        var currentTypeText = propSyntax.Type?.ToString()?.Trim();
        var nullable = string.Empty;
        if (currentTypeText.HasCharacters() &&
            (currentTypeText.StartsWith("Nullable<") || currentTypeText.EndsWith("?"))) {
            nullable = "?";
        }

        var start = DateTimeExtensions.GetTime();
        var newType = SyntaxFactory.ParseTypeName($"{newTypeName}{nullable} ");
        var updatedProp = propSyntax.WithType(newType);
        var document = project.GetDocument(propSyntax.SyntaxTree);
        var root = await document.GetSyntaxRootAsync();
        var newRoot = root!.ReplaceNode(propSyntax, updatedProp);
        var newDocument = document.WithSyntaxRoot(newRoot);
        ChangePropertyTypeAsyncTime += DateTimeExtensions.GetTime() - start;
        //var model = await newDocument.GetSemanticModelAsync();
        //var propSymbol = model.GetDeclaredSymbol(propSyntax);
        //var newDocument = newDocument.Project.GetDocument(document.Id);
        // project = newDocument.Project;
        // ChangePropertyTypeAsyncTime += DateTimeExtensions.GetTime() - start;
        //
        // var newClassSymbol = await CurrentFrom(classSymbol, project);
        return new PropertyActionResult { Project = newDocument.Project, PropertySyntax = propSyntax };
    }

    public static async Task<bool> PropertyExists(this Project project, string fullClassName, string propertyName,
        INamedTypeSymbol classSymbols = null) {
        if (string.IsNullOrEmpty(fullClassName) || string.IsNullOrEmpty(propertyName)) return false;
        classSymbols ??= await FindClassByName(project, fullClassName);
        return await PropertyExists(classSymbols, propertyName);
    }

    public static async Task<bool> PropertyExists(this INamedTypeSymbol classSymbol, string propertyName) {
        if (string.IsNullOrEmpty(propertyName)) return false;
        var tuple = await LocateProperty(classSymbol, propertyName);
        return tuple.propSyntax != null;
    }

    /// <summary> Get the documents containing the given symbol. could be more than one if the class is partial. </summary>
    public static IEnumerable<Document> GetDocuments(this ISymbol symbol, Project project) {
        if (symbol == null) yield break;
        var trees = symbol.DeclaringSyntaxReferences
            .Select(o => o.SyntaxTree)
            .Where(o => o.HasCompilationUnitRoot);
        var hashSet = new HashSet<DocumentId>();
        foreach (var tree in trees) {
            var doc = project.GetDocument(tree);
            if (doc == null) {
                // not a current tree. try another way
                if (tree.FilePath.HasCharacters()) {
                    doc = project.Documents.FirstOrDefault(o => o.FilePath == tree.FilePath);
                }
            }

            if (doc != null && hashSet.Add(doc.Id)) {
                yield return doc;
            }
        }
    }

    // /// <summary> Get the documents containing the given symbol. could be more than one if the class is partial. </summary>
    // public static IEnumerable<Document> GetDocuments(this ISymbol symbol, Solution solution) {
    //     if (symbol == null) yield break;
    //     var trees = symbol.DeclaringSyntaxReferences
    //         .Select(o => o.SyntaxTree)
    //         .Where(o => o.HasCompilationUnitRoot);
    //     var hashSet = new HashSet<DocumentId>();
    //     foreach (var tree in trees) {
    //         var doc = solution.GetDocument(tree);
    //         if (doc != null && hashSet.Add(doc.Id)) {
    //             yield return doc;
    //         }
    //     }
    // }

    public static async Task<IList<INamedTypeSymbol>> FindClassesByName(this Project project,
        string namespaceName, string className) {
        var start = DateTimeExtensions.GetTime();
        if (string.IsNullOrEmpty(className)) return Array.Empty<INamedTypeSymbol>();
        if (namespaceName.HasCharacters()) {
            // use the faster full name
            var fullName = $"{namespaceName}.{className}";
            var oneResult = await FindClassByName(project, fullName);
            return oneResult == null ? Array.Empty<INamedTypeSymbol>() : new[] { oneResult };
        }

        var result = await Microsoft.CodeAnalysis.FindSymbols.SymbolFinder.FindDeclarationsAsync(project, className,
            false,
            SymbolFilter.Type, CancellationToken.None);
        var results = result?.Where(o => o.Kind == SymbolKind.NamedType)
            .OfType<INamedTypeSymbol>()
            .Where(o => o.TypeKind == TypeKind.Class && !o.IsAnonymousType && !o.IsValueType &&
                        o.IsNamespaceMatch(namespaceName))
            .ToArray();
        FindClassesByNameTime += DateTimeExtensions.GetTime() - start;
        return results ?? Array.Empty<INamedTypeSymbol>();
    }


    public static async Task<INamedTypeSymbol> FindClassByName(this Project project, string fullName) {
        var start = DateTimeExtensions.GetTime();
        // var parts = fullName.SplitNamespaceAndName();
        // return (await FindClassesByName(project, parts.namespaceName, parts.name)).FirstOrDefault();
        var compilation = await project.GetCompilationAsync();
        var type = compilation.GetTypeByMetadataName(fullName);
        FindClassesByNameTime += DateTimeExtensions.GetTime() - start;
        return type;
    }

    private static async
        Task<(ITypeSymbol classSymbol, SyntaxNode classSyntax, PropertyDeclarationSyntax propSyntax)>
        LocateProperty(
            this Project project,
            string fullClassName,
            string propertyName) {
        if (project == null || string.IsNullOrEmpty(fullClassName) || string.IsNullOrEmpty(propertyName))
            return default;
        var classSymbol = await FindClassByName(project, fullClassName);
        var tuple = await LocateProperty(classSymbol, propertyName);
        return (classSymbol, tuple.classSyntax, tuple.propSyntax);
    }

    private static async
        Task<(SyntaxNode classSyntax, PropertyDeclarationSyntax propSyntax)> LocateProperty(
            this INamedTypeSymbol classSymbol, string propertyName) {
        if (classSymbol == null || string.IsNullOrEmpty(propertyName)) return default;

        // now find property
        var trees = classSymbol.DeclaringSyntaxReferences
            .Where(o => o.SyntaxTree.HasCompilationUnitRoot);
        foreach (var classSyntaxRef in trees) {
            var classSyntax = await classSyntaxRef.GetSyntaxAsync();
            var propSyntax = classSyntax.DescendantNodesAndSelf().OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(o => o.Identifier.Text == propertyName);
            if (propSyntax is null) continue; // not found in this class tree

            // found the property
            return (classSyntax, propSyntax);
        }

        return default;
    }


    /// <summary> return tru e if the namespace is empty (effectively dont care to match) or the symbol's containing namespace is a match </summary>
    private static bool IsNamespaceMatch(this ISymbol classSymbol, string namespaceName) {
        if (namespaceName.IsNullOrEmpty()) return true;
        var ns = classSymbol.ContainingNamespace;
        var symbolDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
        var fullyQualifiedName = ns.ToDisplayString(symbolDisplayFormat);
        return fullyQualifiedName == namespaceName;
    }

    public static async Task<int> SaveDocumentsAsync(this Project project, IEnumerable<DocumentId> documentIds) {
        var saveCount = 0;
        foreach (var documentId in documentIds) {
            var document = project.GetDocument(documentId);
            var path = document?.FilePath;
            if (path == null) continue;
            var text = await GetDocumentText(document);
            await File.WriteAllTextAsync(path, text, Encoding.UTF8);
            saveCount++;
        }

        return saveCount;
    }

    public static async Task<int> SaveDocumentsAsync(this IEnumerable<Document> documents,
        bool compareForChangesFirst = true) {
        var saveCount = 0;
        foreach (var document in documents) {
            var path = document.FilePath;
            if (path == null) continue;
            var text = await GetDocumentText(document);
            if (compareForChangesFirst) {
                var orig = (await File.ReadAllTextAsync(path, Encoding.UTF8))?.Trim();
                if (text == orig) continue;
            }

            await File.WriteAllTextAsync(path, text, Encoding.UTF8);
            saveCount++;
        }

        return saveCount;
    }

    public static async Task<string> GetDocumentText(this Document document) {
        return string.Join(
            Environment.NewLine,
            (await document.GetTextAsync()).Lines.Select(o => o.ToString())).Trim();
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

                // latest = (VisualStudioInstance)Activator.CreateInstance(
                //     typeof(VisualStudioInstance),
                //     BindingFlags.NonPublic | BindingFlags.Instance,
                //     null,
                //     new object[] { latest.Name, latest.MSBuildPath + "\\", latest.Version, latest.DiscoveryType },
                //     null,
                //     null)!;

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
        //var host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
        var ws = new AdhocWorkspace();
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

    /// <summary> If the given node is not currently on the given root, get the updated version based on the node's identifier. </summary>
    public static string GetFullName(this ITypeSymbol symbol) {
        return $"{symbol.ContainingNamespace.ToDisplayString()}.{symbol.Name}";
    }

    /// <summary> If the given node is not currently on the given root, get the updated version based on the node's identifier. </summary>
    public static async Task<INamedTypeSymbol> CurrentFrom(this ITypeSymbol symbol, Project project) {
        var type = await FindClassByName(project, symbol.GetFullName());
        Debug.Assert(type != null);
        return type;
    }

    /// <summary> If the given node is not currently on the given root, get the updated version based on the node's identifier. </summary>
    public static ClassDeclarationSyntax
        CurrentFrom(this ClassDeclarationSyntax syntaxNode, CompilationUnitSyntax root) {
        return syntaxNode.CurrentFrom(n => n.Identifier, root);
    }

    /// <summary> If the given node is not currently on the given root, get the updated version based on the node's identifier. </summary>
    public static ClassDeclarationSyntax CurrentFrom(this ClassDeclarationSyntax syntaxNode, SyntaxNode parent) {
        return syntaxNode.CurrentFrom(n => n.Identifier, parent);
    }

    /// <summary> If the given node is not currently on the given root, get the updated version based on the node's identifier. </summary>
    private static T CurrentFrom<T>(this T syntaxNode, Func<T, SyntaxToken> identifier, CompilationUnitSyntax root,
        int skipCount = 0) where T : SyntaxNode {
        if (syntaxNode is null) return null;
        if (syntaxNode.HasRoot(root)) return syntaxNode;

        // ensure we have a current reference to the node
        var id = identifier(syntaxNode);
        return root.DescendantNodes().OfType<T>().Where(o => identifier(o).IsEquivalentTo(id)).Skip(skipCount).First();
    }

    /// <summary> If the given node is not currently on the given root, get the updated version based on the node's identifier. </summary>
    private static T CurrentFrom<T>(this T syntaxNode, Func<T, SyntaxToken> identifier, SyntaxNode parent)
        where T : SyntaxNode {
        if (syntaxNode is null) return null;
        if (parent.HasNode(syntaxNode)) return syntaxNode;

        // ensure we have a current reference to the node
        var id = identifier(syntaxNode);
        if (id.HasLeadingTrivia || id.HasTrailingTrivia) id = id.WithoutTrivia();
        return parent.DescendantNodes().OfType<T>().First(o => {
            var id2 = identifier(o);
            if (id2.HasLeadingTrivia || id2.HasTrailingTrivia) id2 = id2.WithoutTrivia();
            return id2.IsEquivalentTo(id);
        });
    }

    /// <summary> return true if the root of the given node matches the given root. </summary>
    public static bool HasRoot(this SyntaxNode syntaxNode, CompilationUnitSyntax root) {
        return syntaxNode.GetParent<CompilationUnitSyntax>() == root;
    }

    /// <summary> return true if the root contains the given node. </summary>
    public static bool HasNode<T>(this T parent, SyntaxNode syntaxNode) where T : SyntaxNode {
        return syntaxNode.EnumerateParents<T>().Any(o => o == parent);
    }

    /// <summary> traverse the parents of this node searching for a node of the given type T </summary>
    public static T GetParent<T>(this SyntaxNode syntaxNode) where T : SyntaxNode {
        var p = syntaxNode?.Parent;
        while (p != null && !(p is T))
            try { p = p.Parent; } catch { p = null; }

        return p as T;
    }

    /// <summary> traverse the parents of this node searching for a node of the given type T </summary>
    public static IEnumerable<T> EnumerateParents<T>(this SyntaxNode syntaxNode) where T : SyntaxNode {
        var p = syntaxNode?.Parent;
        if (p is T tp) yield return tp;
        while (p != null) {
            try { p = p.Parent; } catch { p = null; }

            if (p is T tp2) yield return tp2;
        }
    }
}
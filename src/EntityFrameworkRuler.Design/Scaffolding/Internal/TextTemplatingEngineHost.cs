using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.VisualStudio.TextTemplating;

namespace EntityFrameworkRuler.Design.Scaffolding.Internal;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public class TextTemplatingEngineHost : ITextTemplatingSessionHost, ITextTemplatingEngineHost, IServiceProvider {
    private static readonly List<string> noWarn = new() { "CS1701", "CS1702" };

    private readonly IServiceProvider serviceProvider;
    private ITextTemplatingSession session;
    private CompilerErrorCollection errors;
    private string extension;
    private Encoding outputEncoding;
    private bool fromOutputDirective;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public TextTemplatingEngineHost(IServiceProvider serviceProvider = null) {
        this.serviceProvider = serviceProvider;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    [AllowNull]
    public virtual ITextTemplatingSession Session {
        get => session ??= CreateSession();
        set => session = value;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual IList<string> StandardAssemblyReferences { get; } = new[] {
        typeof(ITextTemplatingEngineHost).Assembly.Location, typeof(CompilerErrorCollection).Assembly.Location
    };

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual IList<string> StandardImports { get; } = new[] { "System" };

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual string TemplateFile { get; set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual string Extension
        => extension ?? ".cs";

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual CompilerErrorCollection Errors
        => errors ??= new CompilerErrorCollection();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual Encoding OutputEncoding
        => outputEncoding ?? Encoding.UTF8;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual void Initialize() {
        session?.Clear();
        errors = null;
        extension = null;
        outputEncoding = null;
        fromOutputDirective = false;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual ITextTemplatingSession CreateSession()
        => new TextTemplatingSession();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual object GetHostOption(string optionName)
        => null;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual bool LoadIncludeText(string requestFileName, out string content, out string location) {
        // TODO: Expand variables?
        location = ResolvePath(requestFileName);
        var exists = File.Exists(location);
        content = exists
            ? File.ReadAllText(location)
            : string.Empty;

        return exists;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual void LogErrors(CompilerErrorCollection errors)
        => Errors.AddRange(errors.Cast<CompilerError>().Where(e => !noWarn.Contains(e.ErrorNumber)).ToArray());

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual AppDomain ProvideTemplatingAppDomain(string content)
        => AppDomain.CurrentDomain;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual string ResolveAssemblyReference(string assemblyReference) {
        var path = Microsoft.Extensions.DependencyModel.DependencyContext.Default?.CompileLibraries
            .FirstOrDefault(l => l.Assemblies.Any(a => Path.GetFileNameWithoutExtension(a) == assemblyReference))
            ?.ResolveReferencePaths()
            .First(p => Path.GetFileNameWithoutExtension(p) == assemblyReference);
        if (path is not null) {
            return path;
        }

        try {
            const string projectdirOutputpath = "$(ProjectDir)$(OutputPath)";
            if (assemblyReference.StartsWith(projectdirOutputpath)) assemblyReference = assemblyReference.Substring(projectdirOutputpath.Length);
            return System.Reflection.Assembly.Load(assemblyReference).Location;
        } catch { }

        // TODO: Expand variables?
        return assemblyReference;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual Type ResolveDirectiveProcessor(string processorName)
        => throw new FileNotFoundException($"Failed to resolve type for directive processor {processorName}");

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual string ResolveParameterValue(string directiveId, string processorName, string parameterName)
        => string.Empty;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual string ResolvePath(string path)
        => !Path.IsPathRooted(path) && Path.IsPathRooted(TemplateFile)
            ? Path.Combine(Path.GetDirectoryName(TemplateFile)!, path)
            : path;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual void SetFileExtension(string extension)
        => this.extension = extension;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual void SetOutputEncoding(Encoding encoding, bool fromOutputDirective) {
        if (this.fromOutputDirective) {
            return;
        }

        outputEncoding = encoding;
        this.fromOutputDirective = fromOutputDirective;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual object GetService(Type serviceType)
        => serviceProvider?.GetService(serviceType);
}
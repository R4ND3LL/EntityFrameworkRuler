using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.VisualStudio.TextTemplating;

namespace EntityFrameworkRuler.Design.Scaffolding.Internal;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public class TextTemplatingEngineHost : ITextTemplatingSessionHost, ITextTemplatingEngineHost, IServiceProvider {
    private static readonly List<string> _noWarn = new() { "CS1701", "CS1702" };

    private readonly IServiceProvider? _serviceProvider;
    private ITextTemplatingSession? _session;
    private CompilerErrorCollection? _errors;
    private string? _extension;
    private Encoding? _outputEncoding;
    private bool _fromOutputDirective;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public TextTemplatingEngineHost(IServiceProvider? serviceProvider = null) {
        _serviceProvider = serviceProvider;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    [AllowNull]
    public virtual ITextTemplatingSession Session {
        get => _session ??= CreateSession();
        set => _session = value;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual IList<string> StandardAssemblyReferences { get; } = new[] {
        typeof(ITextTemplatingEngineHost).Assembly.Location, typeof(CompilerErrorCollection).Assembly.Location
    };

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual IList<string> StandardImports { get; } = new[] { "System" };

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual string? TemplateFile { get; set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual string Extension
        => _extension ?? ".cs";

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual CompilerErrorCollection Errors
        => _errors ??= new CompilerErrorCollection();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual Encoding OutputEncoding
        => _outputEncoding ?? Encoding.UTF8;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual void Initialize() {
        _session?.Clear();
        _errors = null;
        _extension = null;
        _outputEncoding = null;
        _fromOutputDirective = false;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual ITextTemplatingSession CreateSession()
        => new TextTemplatingSession();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual object? GetHostOption(string optionName)
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
        => Errors.AddRange(errors.Cast<CompilerError>().Where(e => !_noWarn.Contains(e.ErrorNumber)).ToArray());

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
        => _extension = extension;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual void SetOutputEncoding(Encoding encoding, bool fromOutputDirective) {
        if (_fromOutputDirective) {
            return;
        }

        _outputEncoding = encoding;
        _fromOutputDirective = fromOutputDirective;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual object? GetService(Type serviceType)
        => _serviceProvider?.GetService(serviceType);
}
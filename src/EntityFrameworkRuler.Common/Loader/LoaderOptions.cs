// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Loader;

/// <summary> Options for loading rules </summary>
public class LoaderOptions {
    /// <summary> The target project path containing entity models. </summary>
    public string ProjectBasePath { get; set; }
}
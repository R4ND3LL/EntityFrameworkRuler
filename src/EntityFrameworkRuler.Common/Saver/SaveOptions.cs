// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Saver;

/// <summary> Options for loading rules </summary>
public class SaveOptions {
    /// <summary> The target project path containing entity models. </summary>
    public string ProjectBasePath { get; set; }
}
namespace EntityFrameworkRuler.Applicator;

public sealed class ApplicatorOptions {
    /// <summary> The target project path containing entity models. </summary>
    public string ProjectBasePath { get; set; }

    /// <summary> Form an adhoc in-memory project out of the target entity model files instead of loading project directly. </summary>
    public bool AdhocOnly { get; set; }
}
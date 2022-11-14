using EntityFrameworkRuler.Loader;

namespace EntityFrameworkRuler.Applicator;

public sealed class ApplicatorOptions : LoaderOptions {
    /// <summary> Form an adhoc in-memory project out of the target entity model files instead of loading project directly. </summary>
    public bool AdhocOnly { get; set; }
}
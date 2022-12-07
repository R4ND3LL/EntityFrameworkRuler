using System.Diagnostics;

namespace EntityFrameworkRuler.Common;

/// <summary>
///    This interface is explicitly implemented by type to hide properties that are not intended to be used in application code
///    but can be used in extension methods written by database providers etc.
/// </summary>
/// <typeparam name="T"> The type of the property being hidden. </typeparam>
public interface IInternals<out T> {
    /// <summary>     Gets the value of the property being hidden. </summary>
    T Instance { get; }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public static class InternalsExtensions {

    /// <summary> Gets the value from a property that is being hidden </summary>
    [DebuggerStepThrough]
    public static T GetInternals<T>(this IInternals<T> accessor)
        => accessor.Instance;
}
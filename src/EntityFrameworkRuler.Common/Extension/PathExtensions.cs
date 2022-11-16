using System.Linq;
using System.Threading.Tasks;
using EntityFrameworkRuler.Applicator.CsProjParser;
using EntityFrameworkRuler.Common;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Extension;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public static class PathExtensions {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static Task<string[]> FindEdmxFilesNearProjectAsync(this string projectBasePath) {
        return FindEdmxFilesUnderPathAsync(FindSolutionParentPath(projectBasePath));
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static string[] FindEdmxFilesNearProject(this string projectBasePath) {
        return FindEdmxFilesUnderPath(FindSolutionParentPath(projectBasePath));
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static Task<string[]> FindEdmxFilesUnderPathAsync(this string solutionBasePath) {
        return Task.Factory.StartNew(() => Directory.GetFiles(solutionBasePath, "*.edmx", SearchOption.AllDirectories));
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static string[] FindEdmxFilesUnderPath(this string solutionBasePath) {
        return Directory.GetFiles(solutionBasePath, "*.edmx", SearchOption.AllDirectories);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static string FindSolutionParentPath(this string projectBasePath) {
        if (projectBasePath.IsNullOrWhiteSpace()) return null;
        var di = new DirectoryInfo(projectBasePath);
        while (di?.GetFiles("*.sln", SearchOption.TopDirectoryOnly).Length == 0) di = di.Parent;
        return di?.Exists != true ? null : di.FullName;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static string FindProjectParentPath(this string projectSubPath) {
        if (projectSubPath.IsNullOrWhiteSpace()) return null;
        var di = new DirectoryInfo(projectSubPath);
        while (di?.GetFiles("*.csproj", SearchOption.TopDirectoryOnly).Length == 0) di = di.Parent;
        return di?.Exists != true ? null : di.FullName;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static FileInfo[] ResolveCsProjFiles(this string projectBasePath) => ResolveCsProjFiles(ref projectBasePath);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static FileInfo[] ResolveCsProjFiles(ref string projectBasePath) {
        FileInfo[] csProjFiles;
        if (projectBasePath.EndsWithIgnoreCase(".csproj")) {
            csProjFiles = new[] { new FileInfo(projectBasePath) };
            projectBasePath = csProjFiles[0].Directory?.FullName;
        } else {
            var dir = new DirectoryInfo(projectBasePath!);
            csProjFiles = dir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly)
                .OrderByDescending(o => o.Name?.Length ?? 0)
                .ToArray();
        }

        return csProjFiles;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    internal static CsProject InspectProject(this string projectBasePath, LoggedResponse loggedResponse = null) {
        try {
            var csProjFiles = ResolveCsProjFiles(ref projectBasePath);
            if (!csProjFiles.IsNullOrEmpty())
                foreach (var csProjFile in csProjFiles) {
                    CsProject csProj;
                    try {
                        var text = File.ReadAllText(csProjFile.FullName);
                        csProj = CsProjSerializer.Deserialize(text);
                        csProj.FilePath = csProjFile.FullName;
                    } catch (Exception ex) {
                        loggedResponse?.LogError($"Unable to parse csproj: {ex.Message}");
                        continue;
                    }

                    return csProj;
                }
        } catch (Exception ex) {
            loggedResponse?.LogError($"Unable to read csproj: {ex.Message}");
        }

        return new();
    }
}
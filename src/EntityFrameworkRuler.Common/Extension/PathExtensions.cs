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
    /// <summary> Ensure the path exists by creating the directory if missing. </summary>
    public static bool EnsurePathExists(this string path) {
        try {
            var filename = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(filename) && Path.HasExtension(filename)) path = path[..(path.Length - filename.Length - 1)];

            if (Directory.Exists(path)) return true;
            var di = Directory.CreateDirectory(path);
            return di.Exists;
        } catch {
            return false;
        }
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static Task<FileInfo[]> FindEdmxFilesNearProjectAsync(this string projectBasePath) {
        return FindEdmxFilesUnderPathAsync(FindSolutionParentPath(projectBasePath));
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static IEnumerable<FileInfo> FindEdmxFilesNearProject(this string projectBasePath) {
        return FindEdmxFilesUnderPath(FindSolutionParentPath(projectBasePath));
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static Task<FileInfo[]> FindEdmxFilesUnderPathAsync(this string path, bool recurseSubdirectories = true,
        int maxRecursionDepth = 5) {
        return Task.Factory.StartNew(() => FindEdmxFilesUnderPath(path, recurseSubdirectories, maxRecursionDepth).ToArray());
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static IEnumerable<FileInfo> FindEdmxFilesUnderPath(this string path, bool recurseSubdirectories = true,
        int maxRecursionDepth = 5) {
        return FindFilesUnderPath(path, "edmx", recurseSubdirectories, maxRecursionDepth);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static string FindSolutionParentPath(this string projectBasePath) {
        if (projectBasePath.IsNullOrWhiteSpace()) return null;
        var di = new DirectoryInfo(projectBasePath);
        while (di != null && di.FindFile("*.sln", recurseSubdirectories: false) == null) di = di.Parent;
        return di?.Exists != true ? null : di.FullName;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static string FindProjectParentPath(this string projectSubPath) {
        if (projectSubPath.IsNullOrWhiteSpace()) return null;
        var di = new DirectoryInfo(projectSubPath);
        while (di != null && di.FindFile("*.csproj", recurseSubdirectories: false) == null) di = di.Parent;
        return di?.Exists != true ? null : di.FullName;
    }


    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static IEnumerable<FileInfo> FindCsProjFiles(this string projectBasePath, bool recurseSubdirectories = true, int maxRecursionDepth = 1) {
        return FindFilesUnderPath(projectBasePath, "csproj", recurseSubdirectories, maxRecursionDepth);
    }
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static IEnumerable<FileInfo> FindFilesUnderPath(this string path, string ext, bool recurseSubdirectories = true, int maxRecursionDepth = 1) {
        if (path.EndsWithIgnoreCase($".{ext}")) {
            var fileInfo = new FileInfo(path);
            return new[] { fileInfo };
        } else {
            var dir = new DirectoryInfo(path!);
            return dir.FindFiles($"*.{ext}", recurseSubdirectories: recurseSubdirectories, maxRecursionDepth: maxRecursionDepth);
        }
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    internal static CsProject InspectProject(this string projectBasePath, LoggedResponse loggedResponse = null) {
        try {
            var csProjFiles = FindCsProjFiles(projectBasePath);
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

    /// <summary> Get the project folder under the current directory.  This operation is cached. </summary>
    internal static DirectoryInfo FindProjectDirUnderCurrentCached() {
        var file = FindProjectFileUnderCurrentCached();
        return file?.Directory;
    }

    /// <summary> Get the first project file under the current directory.  This operation is cached. </summary>
    internal static FileInfo FindProjectFileUnderCurrentCached() {
        var dir = Directory.GetCurrentDirectory();
        if (dir.IsNullOrWhiteSpace()) return null;
        var csproj = dir.FindProjectFileCached();
        return csproj;
    }

    /// <summary> Locate the csproj file under the provided directory.  This operation is cached. </summary>
    internal static FileInfo FindProjectFileCached(this string dir) {
        if (dir.IsNullOrWhiteSpace()) return null;
        if (dir.EndsWithIgnoreCase(".csproj")) {
            var fileInfo = new FileInfo(dir);
            return fileInfo;
        }

        var info = new DirectoryInfo(dir);
        return FindProjectFileCached(info);
    }

    /// <summary> Locate the csproj file under the provided directory.  This operation is cached. </summary>
    internal static FileInfo FindEdmxFileCached(this string dir) {
        if (dir.IsNullOrWhiteSpace()) return null;
        if (dir.EndsWithIgnoreCase(".edmx")) {
            var fileInfo = new FileInfo(dir);
            return fileInfo;
        }

        var info = new DirectoryInfo(dir);
        return FindEdmxFileCached(info);
    }

    /// <summary> Locate the csproj file under the provided directory.  This operation is cached. </summary>
    internal static FileInfo FindSolutionFileCached(this string dir) {
        if (dir.IsNullOrWhiteSpace()) return null;
        if (dir.EndsWithIgnoreCase(".sln")) {
            var fileInfo = new FileInfo(dir);
            return fileInfo;
        }

        var info = new DirectoryInfo(dir);
        return FindSolutionFileCached(info);
    }

    /// <summary> Locate the csproj file under the provided directory.  This operation is cached. </summary>
    internal static FileInfo FindProjectFileCached(this DirectoryInfo info) => FindFileCached(info, "*.csproj");

    /// <summary> Locate the edmx file under the provided directory.  This operation is cached. </summary>
    internal static FileInfo FindEdmxFileCached(this DirectoryInfo info) => FindFileCached(info, "*.edmx");

    /// <summary> Locate the sln file under the provided directory.  This operation is cached. </summary>
    internal static FileInfo FindSolutionFileCached(this DirectoryInfo info) => FindFileCached(info, "*.sln");

    /// <summary> Locate the desired file under the provided directory.  This operation is cached. </summary>
    internal static FileInfo FindFileCached(this string path, string searchPattern) =>
        FindFileCached(new DirectoryInfo(path), searchPattern);

    private static readonly Dictionary<(string, string), FileInfo> findCsProjFileCache = new();

    /// <summary> Locate the desired file under the provided directory.  This operation is cached. </summary>
    internal static FileInfo FindFileCached(this DirectoryInfo info, string searchPattern) {
        return findCsProjFileCache.GetOrAddNew((info.FullName, searchPattern), Factory);

        FileInfo Factory((string, string) arg) {
            return info.FindFile(arg.Item2);
        }
    }


    internal static FileInfo FindFile(this string path, string searchPattern, bool recurseSubdirectories = true,
        int maxRecursionDepth = 1, Predicate<FileInfo> predicate = null) =>
        FindFile(new DirectoryInfo(path), searchPattern, recurseSubdirectories, maxRecursionDepth, predicate);

    internal static FileInfo FindFile(this DirectoryInfo info, string searchPattern, bool recurseSubdirectories = true,
        int maxRecursionDepth = 1, Predicate<FileInfo> predicate = null) {
        try {
            return FindFiles(info, searchPattern, recurseSubdirectories, maxRecursionDepth)
                .FirstOrDefault(o => predicate == null || predicate(o));
        } catch {
            return null;
        }
    }

    internal static IEnumerable<FileInfo> FindFiles(this string path, string searchPattern, bool recurseSubdirectories = true,
        int maxRecursionDepth = 1) =>
        FindFiles(new DirectoryInfo(path), searchPattern, recurseSubdirectories, maxRecursionDepth);

    internal static IEnumerable<FileInfo> FindFiles(this DirectoryInfo info, string searchPattern, bool recurseSubdirectories = true,
        int maxRecursionDepth = 1) {
        if (info?.Exists != true) return Enumerable.Empty<FileInfo>();

        return info.EnumerateFiles(searchPattern,
            new EnumerationOptions() {
                IgnoreInaccessible = true,
                RecurseSubdirectories = recurseSubdirectories,
                MatchType = MatchType.Simple,
#if NET6
                MaxRecursionDepth = maxRecursionDepth,
#endif
                MatchCasing = MatchCasing.CaseInsensitive
            });
    }
}
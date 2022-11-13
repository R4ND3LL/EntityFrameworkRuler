using System.IO;
using System.Threading.Tasks;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal

namespace EdmxRuler.Extensions;

public static class PathExtensions {
    public static Task<string[]> FindEdmxFilesNearProjectAsync(this string projectBasePath) {
        return FindEdmxFilesInSolutionAsync(FindSolutionParentPath(projectBasePath));
    }
    public static Task<string[]> FindEdmxFilesInSolutionAsync(this string solutionBasePath) {
        return Task.Factory.StartNew(() => Directory.GetFiles(solutionBasePath, "*.edmx", SearchOption.AllDirectories));
    }
    public static string FindSolutionParentPath(this string projectBasePath) {
        var di = new DirectoryInfo(projectBasePath);
        while (di?.GetFiles("*.sln", SearchOption.TopDirectoryOnly).Length == 0) {
            di = di.Parent;
        }

        if (di?.Exists != true) return projectBasePath;
        return di.FullName;
    }
}
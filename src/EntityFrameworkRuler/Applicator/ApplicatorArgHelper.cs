using System.Linq;

namespace EntityFrameworkRuler.Applicator;

public static class ApplicatorArgHelper {
    internal static ApplicatorOptions GetDefaultOptions() =>
        TryParseArgs(Array.Empty<string>(), out var o) ? o : o ?? new ApplicatorOptions();

    internal static bool TryParseArgs(string[] argsArray, out ApplicatorOptions applicatorOptions) {
        applicatorOptions = new();
        var args = argsArray.ToList().RemoveSwitchArgs(out var switchArgs);

        if (switchArgs.Count > 0) {
            foreach (var switchArg in switchArgs) {
                switch (switchArg) {
                    case "adhoc":
                    case "adhocOnly":
                        applicatorOptions.AdhocOnly = true;
                        break;
                    default:
                        return false; // invalid arg
                }
            }
        }

        if (args == null || args.Count == 0 || (args.Count == 1 && args[0] == ".")) {
            // auto inspect current folder for both csproj and edmx
            var projectDir = PathExtensions.FindProjectDirUnderCurrentCached();
            if (projectDir?.FullName == null) {
                applicatorOptions = null;
                return false;
            }

            applicatorOptions.ProjectBasePath = projectDir.FullName;
            return true;
        }

        var csProjFile = args.FirstOrDefault(o => o?.EndsWithIgnoreCase(".csproj") == true);

        if (csProjFile != null && File.Exists(csProjFile)) {
            applicatorOptions.ProjectBasePath = new FileInfo(csProjFile).Directory?.FullName;
            return applicatorOptions.ProjectBasePath != null;
        }

        applicatorOptions.ProjectBasePath = args.FirstOrDefault(o => o?.EndsWithIgnoreCase(".csproj") == false
                                                                     && Directory.Exists(o));
        if (applicatorOptions == null || applicatorOptions.ProjectBasePath.IsNullOrEmpty()) return false;

        var projectDir2 = new DirectoryInfo(applicatorOptions.ProjectBasePath!);
        if (!projectDir2.Exists) return false;

        var projectFiles2 = projectDir2.FindProjectFileCached();
        if (projectFiles2 != null) return true;

        applicatorOptions = null;
        return false;
    }
}
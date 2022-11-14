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
            applicatorOptions.ProjectBasePath = Directory.GetCurrentDirectory();
            var projectFiles =
                Directory.GetFiles(applicatorOptions.ProjectBasePath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (projectFiles.Length != 1) {
                applicatorOptions = null;
                return false;
            }

            return true;
        }

        var csProjFile =
            args.FirstOrDefault(o => o?.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) == true);

        if (csProjFile != null && File.Exists(csProjFile)) {
            applicatorOptions.ProjectBasePath = new FileInfo(csProjFile).Directory?.FullName;
            return applicatorOptions.ProjectBasePath != null;
        }

        applicatorOptions.ProjectBasePath = args.FirstOrDefault(o =>
            o?.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) == false
            && Directory.Exists(o));
        if (applicatorOptions == null) return false;

        if (!Directory.Exists(applicatorOptions.ProjectBasePath)) return false;

        var projectFiles2 =
            Directory.GetFiles(applicatorOptions.ProjectBasePath, "*.csproj", SearchOption.TopDirectoryOnly);
        if (projectFiles2.Length == 0) {
            applicatorOptions = null;
            return false;
        }

        return true;
    }
}
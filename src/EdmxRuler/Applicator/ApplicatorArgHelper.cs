using System;
using System.IO;
using System.Linq;
using EdmxRuler.Extensions;

namespace EdmxRuler.Applicator;

public static class ApplicatorArgHelper {
    internal static bool TryParseArgs(string[] argsArray, out ApplicatorArgs applicatorArgs) {
        applicatorArgs = new ApplicatorArgs();
        var args = argsArray.ToList().RemoveSwitchArgs(out var switchArgs);

        if (switchArgs.Count > 0) {
            foreach (var switchArg in switchArgs) {
                switch (switchArg) {
                    case "adhoc":
                    case "adhocOnly":
                        applicatorArgs.AdhocOnly = true;
                        break;
                    default:
                        return false; // invalid arg
                }
            }
        }

        if (args == null || args.Count == 0 || (args.Count == 1 && args[0] == ".")) {
            // auto inspect current folder for both csproj and edmx
            applicatorArgs.ProjectBasePath = Directory.GetCurrentDirectory();
            var projectFiles =
                Directory.GetFiles(applicatorArgs.ProjectBasePath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (projectFiles.Length != 1) {
                applicatorArgs = null;
                return false;
            }

            return true;
        }

        var csProjFile =
            args.FirstOrDefault(o => o?.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) == true);

        if (csProjFile != null && File.Exists(csProjFile)) {
            applicatorArgs.ProjectBasePath = new FileInfo(csProjFile).Directory?.FullName;
            return applicatorArgs.ProjectBasePath != null;
        }

        applicatorArgs.ProjectBasePath = args.FirstOrDefault(o =>
            o?.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) == false
            && Directory.Exists(o));
        if (applicatorArgs == null) return false;

        if (!Directory.Exists(applicatorArgs.ProjectBasePath)) return false;

        var projectFiles2 =
            Directory.GetFiles(applicatorArgs.ProjectBasePath, "*.csproj", SearchOption.TopDirectoryOnly);
        if (projectFiles2.Length == 0) {
            applicatorArgs = null;
            return false;
        }

        return true;
    }
}
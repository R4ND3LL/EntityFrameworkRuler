using System;
using System.IO;
using System.Linq;
using EdmxRuler.Extensions;

namespace EdmxRuler.Generator;

public static class GeneratorArgHelper {
    internal static bool TryParseArgs(string[] args, out string edmxPath, out string projectBasePath) {
        edmxPath = null;
        projectBasePath = null;
        if (args.IsNullOrEmpty() || (args.Length == 1 && args[0] == ".")) {
            // auto inspect current folder for both csproj and edmx
            projectBasePath = Directory.GetCurrentDirectory();
            var projectFiles = Directory.GetFiles(projectBasePath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (projectFiles.Length != 1) {
                projectBasePath = null;
                return false;
            }

            var edmxFiles = Directory.GetFiles(projectBasePath, "*.edmx");
            if (edmxFiles.Length != 1) return false;

            edmxPath = edmxFiles[0];
            return true;
        }

        edmxPath = args.FirstOrDefault(o => o?.EndsWith(".edmx", StringComparison.OrdinalIgnoreCase) == true);
        if (edmxPath == null)
            // inspect arg paths for edmx
            foreach (var arg in args) {
                if (arg.IsNullOrWhiteSpace() || !Directory.Exists(arg)) continue;

                var edmxFiles = Directory.GetFiles(arg, "*.edmx", SearchOption.TopDirectoryOnly);
                if (edmxFiles.Length == 0) continue;

                if (edmxFiles.Length > 1) return false;

                edmxPath = edmxFiles[0];
                break;
            }

        if (edmxPath.IsNullOrEmpty() || !File.Exists(edmxPath)) return false;

        projectBasePath =
            args.FirstOrDefault(o => o?.EndsWith(".edmx", StringComparison.OrdinalIgnoreCase) == false);
        if (projectBasePath.IsNullOrWhiteSpace() || projectBasePath == ".")
            projectBasePath = Directory.GetCurrentDirectory();

        if (!Directory.Exists(projectBasePath)) {
            if (File.Exists(projectBasePath)) {
                projectBasePath = new FileInfo(projectBasePath).Directory?.FullName;
                if (projectBasePath == null || !Directory.Exists(projectBasePath)) return false;
            } else
                return false;
        }

        var projectFiles2 = Directory.GetFiles(projectBasePath, "*.csproj", SearchOption.TopDirectoryOnly);
        if (projectFiles2.Length == 0) {
            projectBasePath = null;
            return false;
        }

        return true;
    }
}
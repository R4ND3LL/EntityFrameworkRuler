using System;
using System.IO;
using System.Linq;
using EdmxRuler.Extensions;

namespace EdmxRuler.Generator;

public static class GeneratorArgHelper {
    internal static bool TryParseArgs(string[] argsArray, out GeneratorArgs generatorArgs) {
        generatorArgs = new GeneratorArgs();

        var args = argsArray.ToList().RemoveSwitchArgs(out var switchArgs);

        if (switchArgs.Count > 0) {
            foreach (var switchArg in switchArgs) {
                // ReSharper disable StringLiteralTypo
                switch (switchArg) {
                    case "nometa":
                    case "nometadata":
                        generatorArgs.NoMetadata = true;
                        break;
                    default:
                        return false; // invalid arg
                }
                // ReSharper restore StringLiteralTypo
            }
        }

        if (args.IsNullOrEmpty() || (args.Count == 1 && args[0] == ".")) {
            // auto inspect current folder for both csproj and edmx
            generatorArgs.ProjectBasePath = Directory.GetCurrentDirectory();
            var projectFiles =
                Directory.GetFiles(generatorArgs.ProjectBasePath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (projectFiles.Length != 1) {
                generatorArgs.ProjectBasePath = null;
                return false;
            }

            var edmxFiles = Directory.GetFiles(generatorArgs.ProjectBasePath, "*.edmx");
            if (edmxFiles.Length != 1) return false;

            generatorArgs.EdmxFilePath = edmxFiles[0];
            return true;
        }

        generatorArgs.EdmxFilePath =
            args.FirstOrDefault(o => o?.EndsWith(".edmx", StringComparison.OrdinalIgnoreCase) == true);
        if (generatorArgs.EdmxFilePath == null)
            // inspect arg paths for edmx
            foreach (var arg in args) {
                if (arg.IsNullOrWhiteSpace() || !Directory.Exists(arg)) continue;

                var edmxFiles = Directory.GetFiles(arg, "*.edmx", SearchOption.TopDirectoryOnly);
                if (edmxFiles.Length == 0) continue;

                if (edmxFiles.Length > 1) return false;

                generatorArgs.EdmxFilePath = edmxFiles[0];
                break;
            }

        if (generatorArgs.EdmxFilePath.IsNullOrEmpty() || !File.Exists(generatorArgs.EdmxFilePath)) return false;

        generatorArgs.ProjectBasePath =
            args.FirstOrDefault(o => o?.EndsWith(".edmx", StringComparison.OrdinalIgnoreCase) == false);
        if (generatorArgs.ProjectBasePath.IsNullOrWhiteSpace() || generatorArgs.ProjectBasePath == ".")
            generatorArgs.ProjectBasePath = Directory.GetCurrentDirectory();

        if (!Directory.Exists(generatorArgs.ProjectBasePath)) {
            if (File.Exists(generatorArgs.ProjectBasePath)) {
                generatorArgs.ProjectBasePath = new FileInfo(generatorArgs.ProjectBasePath).Directory?.FullName;
                if (generatorArgs.ProjectBasePath == null || !Directory.Exists(generatorArgs.ProjectBasePath))
                    return false;
            } else
                return false;
        }

        var projectFiles2 =
            Directory.GetFiles(generatorArgs.ProjectBasePath, "*.csproj", SearchOption.TopDirectoryOnly);
        if (projectFiles2.Length == 0) {
            generatorArgs.ProjectBasePath = null;
            return false;
        }

        return true;
    }
}
using System.Linq;

namespace EntityFrameworkRuler.Generator;

public static class GeneratorArgHelper {
    internal static GeneratorOptions GetDefaultOptions() =>
        TryParseArgs(Array.Empty<string>(), out var o) ? o : o ?? new GeneratorOptions();

    internal static bool TryParseArgs(string[] argsArray, out GeneratorOptions generatorOptions) {
        generatorOptions = new();

        var args = argsArray.ToList().RemoveSwitchArgs(out var switchArgs);

        if (switchArgs.Count > 0) {
            foreach (var switchArg in switchArgs) {
                // ReSharper disable StringLiteralTypo
                switch (switchArg) {
                    case "nometa":
                    case "nometadata":
                        generatorOptions.NoMetadata = true;
                        break;
                    default:
                        return false; // invalid arg
                }
                // ReSharper restore StringLiteralTypo
            }
        }

        if (args.IsNullOrEmpty() || (args.Count == 1 && args[0] == ".")) {
            // auto inspect current folder for both csproj and edmx
            var projectDir = PathExtensions.FindProjectDirUnderCurrentCached();
            if (projectDir?.FullName == null) {
                generatorOptions.ProjectBasePath = null;
                return false;
            }

            generatorOptions.ProjectBasePath = projectDir.FullName;

            var edmxFile = projectDir.FindFile("*.edmx", maxRecursionDepth: 3);
            if (edmxFile == null) return false;

            generatorOptions.EdmxFilePath = edmxFile.FullName;
            return true;
        }

        generatorOptions.EdmxFilePath =
            args.FirstOrDefault(o => o?.EndsWithIgnoreCase(".edmx") == true);
        if (generatorOptions.EdmxFilePath == null)
            // inspect arg paths for edmx
            foreach (var arg in args) {
                if (arg.IsNullOrWhiteSpace() || !Directory.Exists(arg)) continue;

                var edmxFiles = Directory.GetFiles(arg, "*.edmx", SearchOption.TopDirectoryOnly);
                if (edmxFiles.Length == 0) continue;

                if (edmxFiles.Length > 1) return false;

                generatorOptions.EdmxFilePath = edmxFiles[0];
                break;
            }

        if (generatorOptions.EdmxFilePath.IsNullOrEmpty() || !File.Exists(generatorOptions.EdmxFilePath)) return false;

        generatorOptions.ProjectBasePath =
            args.FirstOrDefault(o => o?.EndsWithIgnoreCase(".edmx") == false);
        if (generatorOptions.ProjectBasePath.IsNullOrWhiteSpace() || generatorOptions.ProjectBasePath == ".")
            generatorOptions.ProjectBasePath = Directory.GetCurrentDirectory();

        if (!Directory.Exists(generatorOptions.ProjectBasePath)) {
            if (File.Exists(generatorOptions.ProjectBasePath)) {
                generatorOptions.ProjectBasePath = new FileInfo(generatorOptions.ProjectBasePath).Directory?.FullName;
                if (generatorOptions.ProjectBasePath == null || !Directory.Exists(generatorOptions.ProjectBasePath))
                    return false;
            } else
                return false;
        }

        var projectFiles2 =
            Directory.GetFiles(generatorOptions.ProjectBasePath, "*.csproj", SearchOption.TopDirectoryOnly);
        if (projectFiles2.Length == 0) {
            generatorOptions.ProjectBasePath = null;
            return false;
        }

        return true;
    }
}
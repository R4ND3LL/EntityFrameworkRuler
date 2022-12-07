using System.Linq;
using EntityFrameworkRuler.Generator;
using EntityFrameworkRuler.Saver;

// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Helper;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
internal static class GeneratorArgHelper {
    internal static GenerateAndSaveOptions GetDefaultOptions() =>
        TryParseArgs(Array.Empty<string>(), out var o) ? o : o ?? new GenerateAndSaveOptions();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static bool TryParseArgs(string[] argsArray, out GenerateAndSaveOptions op) {
        op = new();

        var args = argsArray.ToList().RemoveSwitchArgs(out var switchArgs);

        if (switchArgs.Count > 0) {
            foreach (var switchArg in switchArgs) {
                // ReSharper disable StringLiteralTypo
                switch (switchArg) {
                    case "usedatabasenames":
                    case "use-database-names":
                    case "usedbnames":
                        op.GeneratorOptions.UseDatabaseNames = true;
                        break;
                    case "nopluralize":
                    case "no-pluralize":
                    case "np":
                        op.GeneratorOptions.NoPluralize = true;
                        break;
                    case "includeunknowns":
                    case "include-unknowns":
                    case "allowunknowns":
                        op.GeneratorOptions.IncludeUnknowns = true;
                        break;
                    case "compact":
                    case "compactrules":
                    case "compact-rules":
                        op.GeneratorOptions.CompactRules = true;
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
                op.SaveOptions.ProjectBasePath = null;
                return false;
            }

            op.SaveOptions.ProjectBasePath = projectDir.FullName;

            var edmxFile = projectDir.FindFile("*.edmx", maxRecursionDepth: 3);
            if (edmxFile == null) return false;

            op.GeneratorOptions.EdmxFilePath = edmxFile.FullName;
            return true;
        }

        op.GeneratorOptions.EdmxFilePath =
            args.FirstOrDefault(o => o?.EndsWithIgnoreCase(".edmx") == true);
        if (op.GeneratorOptions.EdmxFilePath == null)
            // inspect arg paths for edmx
            foreach (var arg in args) {
                if (arg.IsNullOrWhiteSpace() || !Directory.Exists(arg)) continue;

                var edmxFiles = Directory.GetFiles(arg, "*.edmx", SearchOption.TopDirectoryOnly);
                if (edmxFiles.Length == 0) continue;

                if (edmxFiles.Length > 1) return false;

                op.GeneratorOptions.EdmxFilePath = edmxFiles[0];
                break;
            }

        if (op.GeneratorOptions.EdmxFilePath.IsNullOrEmpty() || !File.Exists(op.GeneratorOptions.EdmxFilePath)) return false;

        op.SaveOptions.ProjectBasePath =
            args.FirstOrDefault(o => o?.EndsWithIgnoreCase(".edmx") == false);
        if (op.SaveOptions.ProjectBasePath.IsNullOrWhiteSpace() || op.SaveOptions.ProjectBasePath == ".")
            op.SaveOptions.ProjectBasePath = Directory.GetCurrentDirectory();

        if (!Directory.Exists(op.SaveOptions.ProjectBasePath)) {
            if (File.Exists(op.SaveOptions.ProjectBasePath)) {
                op.SaveOptions.ProjectBasePath = new FileInfo(op.SaveOptions.ProjectBasePath).Directory?.FullName;
                if (op.SaveOptions.ProjectBasePath == null || !Directory.Exists(op.SaveOptions.ProjectBasePath))
                    return false;
            } else
                return false;
        }

        var projectFiles2 =
            Directory.GetFiles(op.SaveOptions.ProjectBasePath, "*.csproj", SearchOption.TopDirectoryOnly);
        if (projectFiles2.Length == 0)
            projectFiles2 =
                Directory.GetFiles(op.SaveOptions.ProjectBasePath, "*.vbproj", SearchOption.TopDirectoryOnly);

        if (projectFiles2.Length == 0) {
            op.SaveOptions.ProjectBasePath = null;
            return false;
        }

        return true;
    }
}

/// <summary> Generate and save options </summary>
public sealed class GenerateAndSaveOptions {
    /// <summary> Generator options </summary>
    public GeneratorOptions GeneratorOptions { get; } = new();

    /// <summary> Save options </summary>
    public SaveOptions SaveOptions { get; } = new();
}
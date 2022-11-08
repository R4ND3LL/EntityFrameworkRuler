using System.Reflection;
using EdmxRuler.Applicator;
using EdmxRuler.Extensions;
using EdmxRuler.Generator;

namespace EdmxRuler;

internal static class Program {
    private static async Task<int> Main(string[] args) {
        try {
            if (args.IsNullOrEmpty() || args[0].IsNullOrWhiteSpace()) return await ShowHelpInfo();

            var option = GetSwitchArg(args[0]);
            switch (option) {
                case 'g': {
                    // generate rules
                    if (!GeneratorArgHelper.TryParseArgs(args.Skip(1).ToArray(), out var edmxPath,
                            out var projectBasePath))
                        return await ShowHelpInfo();

                    var start = DateTimeExtensions.GetTime();
                    var generator = new RuleGenerator(edmxPath);
                    var rules = generator.TryGenerateRules();
                    await generator.TrySaveRules(projectBasePath);
                    var elapsed = DateTimeExtensions.GetTime() - start;
                    if (generator.Errors.Count == 0) {
                        await Console.Out
                            .WriteLineAsync($"Successfully generated {rules.Count} rule files in {elapsed}ms")
                            .ConfigureAwait(false);
                        return 0;
                    }

                    foreach (var error in generator.Errors)
                        await Console.Out.WriteLineAsync($"Edmx generator error encountered: {error}")
                            .ConfigureAwait(false);

                    return generator.Errors.Count;
                }
                case 'a': {
                    // apply rules
                    if (!ApplicatorArgHelper.TryParseArgs(args.Skip(1).ToArray(), out var projectBasePath))
                        return await ShowHelpInfo();

                    var applicator = new RuleApplicator(projectBasePath);
                    var response = await applicator.ApplyRulesInProjectPath();

                    var errorCount = 0;
                    foreach (var error in response.GetErrors()) {
                        errorCount++;
                        await Console.Out.WriteLineAsync($"Rule applicator error encountered: {error}")
                            .ConfigureAwait(false);
                    }

                    foreach (var info in response.GetInformation()) {
                        await Console.Out.WriteLineAsync($"Info: {info}")
                            .ConfigureAwait(false);
                    }

                    return errorCount;
                }
                default:
                    return await ShowHelpInfo();
            }
        } catch (Exception ex) {
            await Console.Out.WriteLineAsync($"Unexpected error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
    }

    private static char GetSwitchArg(string arg) {
        if (arg.IsNullOrWhiteSpace() || arg.Length < 2) return default;
        var firstChar = arg[0];
        return firstChar is '-' or '/' ? char.ToLower(arg[1]) : default;
    }

    private static async Task<int> ShowHelpInfo() {
        var versionString = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            .ToString();
        await Console.Out.WriteLineAsync($"EdmxRuler v{versionString}");
        await Console.Out.WriteLineAsync("-----------------------");

        await Console.Out.WriteLineAsync("\nRule Generation Usage:");
        await Console.Out.WriteLineAsync("  EdmxRuler -g <edmxfilepath> <efCoreProjectBasePath>");
        await Console.Out.WriteLineAsync("  EdmxRuler -g <pathContainingEdmxAndCsProj>");
        await Console.Out.WriteLineAsync("  EdmxRuler -g . (assuming current directory contains edmx and csproj)");

        await Console.Out.WriteLineAsync("\nRule Applicator Usage:");
        await Console.Out.WriteLineAsync("  EdmxRuler -a <pathContainingRulesAndCsProj>");
        await Console.Out.WriteLineAsync("  EdmxRuler -a . (assuming current directory contains edmx and csproj)");

        return 1;
    }
}
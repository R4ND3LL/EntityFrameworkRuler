using System.Reflection;
using EdmxRuler.Applicator;
using EdmxRuler.Common;
using EdmxRuler.Extensions;
using EdmxRuler.Generator;

namespace EntityFrameworkRuler;

internal static class Program {
    private static async Task<int> Main(string[] args) {
        try {
            if (args.IsNullOrEmpty() || args[0].IsNullOrWhiteSpace()) return await ShowHelpInfo();

            var option = args[0].GetSwitchArgChar();
            switch (option) {
                case 'g': {
                    // generate rules
                    if (!GeneratorArgHelper.TryParseArgs(args.Skip(1).ToArray(), out var generatorArgs))
                        return await ShowHelpInfo();

                    await Console.Out.WriteLineAsync($" - edmx path: {generatorArgs.EdmxFilePath}")
                        .ConfigureAwait(false);
                    await Console.Out.WriteLineAsync($" - project base path: {generatorArgs.ProjectBasePath}")
                        .ConfigureAwait(false);
                    await Console.Out.WriteLineAsync($"").ConfigureAwait(false);

                    var start = DateTimeExtensions.GetTime();
                    var generator = new RuleGenerator(generatorArgs);
                    generator.OnLog += MessageLogged;
                    var response = generator.TryGenerateRules();
                    var rules = response.Rules;
                    await generator.TrySaveRules(rules, generatorArgs.ProjectBasePath);
                    var elapsed = DateTimeExtensions.GetTime() - start;
                    var errorCount = response.Errors.Count();
                    if (errorCount == 0) {
                        await Console.Out
                            .WriteLineAsync($"Successfully generated {rules.Count} rule files in {elapsed}ms")
                            .ConfigureAwait(false);
                        return 0;
                    }

                    return errorCount;
                }
                case 'a': {
                    // apply rules
                    if (!ApplicatorArgHelper.TryParseArgs(args.Skip(1).ToArray(), out var applicatorArgs))
                        return await ShowHelpInfo();

                    await Console.Out.WriteLineAsync($" - project base path: {applicatorArgs.ProjectBasePath}")
                        .ConfigureAwait(false);
                    await Console.Out.WriteLineAsync($"").ConfigureAwait(false);
                    var start = DateTimeExtensions.GetTime();
                    var applicator = new RuleApplicator(applicatorArgs);
                    applicator.OnLog += MessageLogged;
                    var response = await applicator.ApplyRulesInProjectPath();
                    var elapsed = DateTimeExtensions.GetTime() - start;
                    var errorCount = response.GetErrors().Count();
                    if (errorCount == 0) {
                        await Console.Out
                            .WriteLineAsync($"Successfully applied rules in {elapsed}ms")
                            .ConfigureAwait(false);
                        return 0;
                    }

                    return errorCount;
                }

                default:
                    return await ShowHelpInfo();
            }
        } catch
            (Exception ex) {
            await Console.Out.WriteLineAsync($"Unexpected error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
    }

    private static bool logInfo = true;
    private static bool logWarning = true;
    private static bool logError = true;

    private static void MessageLogged(object sender, LogMessage logMessage) {
        switch (logMessage.Type) {
            case LogType.Information:
                if (!logInfo) return;
                Console.Out.Write("Info: ");
                break;
            case LogType.Warning:
                if (!logWarning) return;
                Console.Out.Write("Warning: ");
                break;
            case LogType.Error:
                if (!logError) return;
                Console.Out.Write("Error: ");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logMessage.Type), logMessage.Type, null);
        }

        Console.Out.WriteLine(logMessage.Message);
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
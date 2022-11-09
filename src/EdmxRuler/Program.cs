using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EdmxRuler.Applicator;
using EdmxRuler.Common;
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

                    await Console.Out.WriteLineAsync($" - edmx path: {edmxPath}").ConfigureAwait(false);
                    await Console.Out.WriteLineAsync($" - project base path: {projectBasePath}").ConfigureAwait(false);
                    await Console.Out.WriteLineAsync($"").ConfigureAwait(false);

                    var start = DateTimeExtensions.GetTime();
                    var generator = new RuleGenerator(edmxPath);
                    generator.OnLog += MessageLogged;
                    var response = generator.TryGenerateRules();
                    var rules = response.Rules;
                    await generator.TrySaveRules(rules, projectBasePath);
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
                    if (!ApplicatorArgHelper.TryParseArgs(args.Skip(1).ToArray(), out var projectBasePath))
                        return await ShowHelpInfo();

                    await Console.Out.WriteLineAsync($" - project base path: {projectBasePath}").ConfigureAwait(false);
                    await Console.Out.WriteLineAsync($"").ConfigureAwait(false);
                    var start = DateTimeExtensions.GetTime();
                    var applicator = new RuleApplicator(projectBasePath);
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
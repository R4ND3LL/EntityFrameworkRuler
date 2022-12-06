using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EntityFrameworkRuler.Applicator;
using EntityFrameworkRuler.Generator;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkRuler;

internal static class Program {
    private static async Task<int> Main(string[] args) {
        try {
            if (args.IsNullOrEmpty() || args[0].IsNullOrWhiteSpace()) return await ShowHelpInfo();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddRulerCli();


            var option = args[0].GetSwitchArgChar();
            switch (option) {
                case 'g': {
                    // generate rules
                    if (!GeneratorArgHelper.TryParseArgs(args.Skip(1).ToArray(), out var genAndSaveOptions))
                        return await ShowHelpInfo();

                    await Console.Out.WriteLineAsync($" - edmx path: {genAndSaveOptions.GeneratorOptions.EdmxFilePath}")
                        .ConfigureAwait(false);
                    await Console.Out.WriteLineAsync($" - project base path: {genAndSaveOptions.SaveOptions.ProjectBasePath}")
                        .ConfigureAwait(false);
                    await Console.Out.WriteLineAsync($"").ConfigureAwait(false);

                    var start = DateTimeExtensions.GetTime();
                    var serviceProvider = serviceCollection.BuildServiceProvider();
                    var generator = serviceProvider.GetRequiredService<IRuleGenerator>();
                    var response = await generator.GenerateRulesAsync(genAndSaveOptions.GeneratorOptions);
                    genAndSaveOptions.SaveOptions.Rules.AddRange(response.Rules);
                    await generator.SaveRules(genAndSaveOptions.SaveOptions);
                    var elapsed = DateTimeExtensions.GetTime() - start;
                    var errorCount = response.Errors.Count();
                    if (errorCount == 0) {
                        await Console.Out
                            .WriteLineAsync($"Successfully generated {genAndSaveOptions.SaveOptions.Rules.Count} rule files in {elapsed}ms")
                            .ConfigureAwait(false);
                        return 0;
                    }

                    return errorCount;
                }
                case 'a': {
                    // apply rules
                    if (!ApplicatorArgHelper.TryParseArgs(args.Skip(1).ToArray(), out var loadAndApplyOptions))
                        return await ShowHelpInfo();

                    await Console.Out.WriteLineAsync($" - project base path: {loadAndApplyOptions.ProjectBasePath}")
                        .ConfigureAwait(false);
                    await Console.Out.WriteLineAsync($"").ConfigureAwait(false);
                    var start = DateTimeExtensions.GetTime();
                    var serviceProvider = serviceCollection.BuildServiceProvider();
                    var applicator = serviceProvider.GetRequiredService<IRuleApplicator>();
                    var response = await applicator.ApplyRulesInProjectPath(loadAndApplyOptions);
                    var elapsed = DateTimeExtensions.GetTime() - start;
                    var errorCount = response.Errors.Count();
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


    private static async Task<int> ShowHelpInfo() {
        var versionString = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            .ToString();
        await Console.Out.WriteLineAsync($"Entity Framework Ruler  v{versionString}");
        await Console.Out.WriteLineAsync("-----------------------");

        await Console.Out.WriteLineAsync("\nRule Generation Usage:");
        await Console.Out.WriteLineAsync("  efruler -g <edmxfilepath> <efCoreProjectBasePath>");
        await Console.Out.WriteLineAsync("  efruler -g <pathContainingEdmxAndCsProj>");
        await Console.Out.WriteLineAsync("  efruler -g . (assuming current directory contains edmx and csproj)");

        await Console.Out.WriteLineAsync("\nRule Applicator Usage:");
        await Console.Out.WriteLineAsync("  efruler -a <pathContainingRulesAndCsProj>");
        await Console.Out.WriteLineAsync("  efruler -a . (assuming current directory contains edmx and csproj)");

        return 1;
    }
}
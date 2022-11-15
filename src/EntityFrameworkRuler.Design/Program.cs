using System.Reflection;
using EntityFrameworkRuler.Common;

namespace EntityFrameworkRuler.Design;

internal static class Program {
    private static async Task<int> Main(string[] args) {
        try {
            if (args.IsNullOrEmpty() || args[0].IsNullOrWhiteSpace()) return await ShowHelpInfo();

            return 1;
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
        await Console.Out.WriteLineAsync($"Entity Framework Ruler v{versionString}");
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
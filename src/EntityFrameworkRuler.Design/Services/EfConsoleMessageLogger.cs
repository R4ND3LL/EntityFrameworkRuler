using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Design.Extensions;
using Microsoft.EntityFrameworkCore.Design.Internal;

namespace EntityFrameworkRuler.Design.Services;

/// <summary> Log messages to EF IOperationReporter </summary>
// ReSharper disable once ClassCanBeSealed.Global
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class EfConsoleMessageLogger : MessageLoggerBase {
    private readonly IOperationReporter reporter;

    /// <summary> Creates EfConsoleMessageLogger </summary>
    public EfConsoleMessageLogger(IOperationReporter reporter) {
        this.reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        // allow verbose here. logs are pushed to the reporter, let the reporter decide if it is printed to console.
        MinimumLevel = LogType.Verbose;
    }

    /// <inheritdoc />
    public override void Write(LogMessage logMessage) {
        switch (logMessage.Type) {
            case LogType.Verbose:
#if DEBUG
                reporter.WriteInformation(logMessage.Message);
#else
                reporter.WriteVerbose(logMessage.Message);
#endif
                break;
            case LogType.Information:
                reporter.WriteInformation(logMessage.Message);
                break;
            case LogType.Warning:
                reporter.WriteWarning(logMessage.Message);
                break;
            case LogType.Error:
                reporter.WriteError(logMessage.Message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logMessage.Type), logMessage.Type, null);
        }

        //DebugLog(logMessage.Message);
    }

    [Conditional("DEBUG")]
    internal static void DebugLog(string msg) => Debug.WriteLine(msg);
}
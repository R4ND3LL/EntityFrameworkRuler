// ReSharper disable UnusedMember.Global

namespace EntityFrameworkRuler.Common;

/// <summary> Logger for Ruler responses </summary>
public interface IMessageLogger {
    /// <summary> Minimum allowed level to log </summary>
    LogType MinimumLevel { get; set; }

    /// <summary> Write a message to the log with no level check. </summary>
    void Write(LogMessage logMessage);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    void WriteError(string message);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    void WriteWarning(string message);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    void WriteInformation(string message);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    void WriteVerbose(string message);
}

/// <summary> Log messages to console </summary>
// ReSharper disable once ClassCanBeSealed.Global
public class ConsoleMessageLogger : MessageLoggerBase, IMessageLogger {
    /// <inheritdoc />
    public override void Write(LogMessage logMessage) {
        switch (logMessage.Type) {
            case LogType.Verbose:
                //Console.Out.Write("Verbose: ");
                break;
            case LogType.Information:
                //Console.Out.Write("Info: ");
                break;
            case LogType.Warning:
                Console.Out.Write("Warning: ");
                break;
            case LogType.Error:
                Console.Out.Write("Error: ");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logMessage.Type), logMessage.Type, null);
        }

        Console.Out.WriteLine(logMessage.Message);
    }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public abstract class MessageLoggerBase : IMessageLogger {
    /// <inheritdoc />
    public LogType MinimumLevel { get; set; } = LogType.Information;

    /// <inheritdoc />
    public abstract void Write(LogMessage logMessage);

    /// <inheritdoc />
    public void WriteError(string message) {
        if (MinimumLevel <= LogType.Error) Write(LogMessage.Error(message));
    }

    /// <inheritdoc />
    public void WriteWarning(string message) {
        if (MinimumLevel <= LogType.Warning) Write(LogMessage.Warning(message));
    }

    /// <inheritdoc />
    public void WriteInformation(string message) {
        if (MinimumLevel <= LogType.Information) Write(LogMessage.Information(message));
    }

    /// <inheritdoc />
    public void WriteVerbose(string message) {
        if (MinimumLevel <= LogType.Verbose) Write(LogMessage.Verbose(message));
    }
}

/// <summary> Do not log messages </summary>
public sealed class NullMessageLogger : MessageLoggerBase {
    private static NullMessageLogger instance;

    /// <summary> Get an instance of the null logger </summary>
    public static NullMessageLogger Instance => instance ??= new NullMessageLogger();

    private NullMessageLogger() {
        MinimumLevel = LogType.Error;
    }

    /// <inheritdoc />
    public override void Write(LogMessage logMessage) { }
}
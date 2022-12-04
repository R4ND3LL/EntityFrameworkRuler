namespace EntityFrameworkRuler.Common;

/// <summary> Logger for Ruler responses </summary>
public interface IMessageLogger {
    /// <summary> Minimum allowed level to log </summary>
    LogType MinimumLevel { get; set; }

    /// <summary> Write a message to the log </summary>
    void Write(LogMessage logMessage);
}

/// <summary> Log messages to console </summary>
// ReSharper disable once ClassCanBeSealed.Global
public class ConsoleMessageLogger : IMessageLogger {
    /// <inheritdoc />
    public LogType MinimumLevel { get; set; } = LogType.Information;
    /// <inheritdoc />
    public void Write(LogMessage logMessage) {
        switch (logMessage.Type) {
            case LogType.Information:
                Console.Out.Write("Info: ");
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
/// <summary> Do not log messages </summary>
public sealed class NullMessageLogger : IMessageLogger {
    private static NullMessageLogger instance;
    /// <summary> Get an instance of the null logger </summary>
    public static NullMessageLogger Instance => instance ??= new NullMessageLogger();

    /// <inheritdoc />
    public LogType MinimumLevel { get; set; } = LogType.Error;

    /// <inheritdoc />
    public void Write(LogMessage logMessage) { }
}
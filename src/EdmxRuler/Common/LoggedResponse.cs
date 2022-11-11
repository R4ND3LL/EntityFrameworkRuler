using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace EdmxRuler.Common;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public class LoggedResponse {
    public List<LogMessage> Messages { get; } = new();
    public IEnumerable<string> Errors => Messages.Where(o => o.Type == LogType.Error).Select(o => o.Message);
    public IEnumerable<string> Information => Messages.Where(o => o.Type == LogType.Information).Select(o => o.Message);
    public IEnumerable<string> Warnings => Messages.Where(o => o.Type == LogType.Warning).Select(o => o.Message);

    internal void LogInformation(string msg) {
        Messages.Add(Raise(LogMessage.Information(msg)));
    }

    internal void LogWarning(string msg) {
        Messages.Add(Raise(LogMessage.Warning(msg)));
    }

    internal void LogError(string msg) {
        Messages.Add(Raise(LogMessage.Error(msg)));
    }

    internal void Merge(LoggedResponse src) {
        Messages.AddRange(src.Messages);
    }

    internal event LogMessageHandler OnLog;

    private LogMessage Raise(LogMessage msg) {
        OnLog?.Invoke(this, msg);
        return msg;
    }
}

public delegate void LogMessageHandler(object sender, LogMessage logMessage);

public enum LogType {
    Information = 0,
    Warning,
    Error
}

public struct LogMessage {
    internal static LogMessage Information(string message) => new LogMessage(LogType.Information, message);
    internal static LogMessage Warning(string message) => new LogMessage(LogType.Warning, message);
    internal static LogMessage Error(string message) => new LogMessage(LogType.Error, message);

    // ReSharper disable once MemberCanBePrivate.Global
    public LogMessage(LogType type, string message) {
        Type = type;
        Message = message;
    }

    // ReSharper disable once MemberCanBeInternal
    public LogType Type { get; set; }

    // ReSharper disable once MemberCanBeInternal
    public string Message { get; set; }
    public static implicit operator string(LogMessage logMessage) => logMessage.ToString();
    public override string ToString() => $"{Type}: {Message}";
}
﻿using System.Diagnostics.CodeAnalysis;
using System.Linq;

// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Common;

/// <summary> Response with log messages </summary>
public interface ILoggedResponse : IResponse {
    /// <summary> Errors within the message list </summary>
    IEnumerable<string> Errors { get; }

    /// <summary> Information within the message list </summary>
    IEnumerable<string> Information { get; }

    /// <summary> Warnings within the message list </summary>
    IEnumerable<string> Warnings { get; }

    /// <summary> Get messages </summary>
    IEnumerable<LogMessage> GetMessages();

    /// <summary> Gets whether this response contains any errors </summary>
    bool HasErrors { get; }

    /// <summary> Logger service to pipe messages to at runtime. </summary>
    IMessageLogger Logger { get; set; }
}

/// <summary> Response with log messages </summary>
public interface IResponse {
    /// <summary> Gets whether the operation completed successfully. </summary>
    bool Success { get; }
}

/// <summary> Response with log messages </summary>
[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public abstract class LoggedResponse : ILoggedResponse, IInternals<ILoggedResponseInternal>, ILoggedResponseInternal {
    /// <summary> Creates a logged response </summary>
    protected LoggedResponse(IMessageLogger logger) {
        Logger = logger;
    }

    /// <summary> Log messages </summary>
    public List<LogMessage> Messages { get; } = new();

    /// <summary> Errors within the message list </summary>
    public IEnumerable<string> Errors => Messages.Where(o => o.Type == LogType.Error).Select(o => o.Message);

    /// <summary> Information within the message list </summary>
    public IEnumerable<string> Information => Messages.Where(o => o.Type == LogType.Information).Select(o => o.Message);

    /// <summary> Warnings within the message list </summary>
    public IEnumerable<string> Warnings => Messages.Where(o => o.Type == LogType.Warning).Select(o => o.Message);

    /// <summary> Get messages </summary>
    IEnumerable<LogMessage> ILoggedResponse.GetMessages() => Messages;

    /// <summary> Gets whether this response contains any errors </summary>
    public bool HasErrors => Errors.Any();

    /// <inheritdoc />
    public IMessageLogger Logger { get; set; }

    /// <inheritdoc />
    public virtual bool Success => !HasErrors;

    ILoggedResponseInternal IInternals<ILoggedResponseInternal>.Instance => this;


    void ILoggedResponseInternal.LogVerbosely(string msg) {
        Messages.Add(Raise(LogMessage.Verbose(msg)));
    }

    void ILoggedResponseInternal.LogInformation(string msg) {
        Messages.Add(Raise(LogMessage.Information(msg)));
    }

    void ILoggedResponseInternal.LogWarning(string msg) {
        Messages.Add(Raise(LogMessage.Warning(msg)));
    }

    void ILoggedResponseInternal.LogError(string msg) {
        Messages.Add(Raise(LogMessage.Error(msg)));
    }

    void ILoggedResponseInternal.Merge(LoggedResponse src) {
        Messages.AddRange(src.Messages);
    }
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public event LogMessageHandler Log;

    /// <summary> Raise event for logged message </summary>
    protected virtual LogMessage Raise(LogMessage msg) {
        Log?.Invoke(this, msg);
        if (Logger is { } l && l.MinimumLevel <= msg.Type) l.Write(msg);
        return msg;
    }
}
/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public interface ILoggedResponseInternal : ILoggedResponse {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    void LogVerbosely(string msg);
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    void LogInformation(string msg);
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    void LogWarning(string msg);
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    void LogError(string msg);
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    void Merge(LoggedResponse src);
}

/// <summary> Log message handler signature </summary>
public delegate void LogMessageHandler(object sender, LogMessage logMessage);

/// <summary> The log type </summary>
public enum LogType {
    /// <summary> Verbose </summary>
    Verbose = 0,

    /// <summary> Information </summary>
    Information = 1,

    /// <summary> Warning </summary>
    Warning,

    /// <summary> Error </summary>
    Error
}

/// <summary> A simple log message object </summary>
public struct LogMessage {
    internal static LogMessage Verbose(string message) => new(LogType.Verbose, message);
    internal static LogMessage Information(string message) => new(LogType.Information, message);
    internal static LogMessage Warning(string message) => new(LogType.Warning, message);
    internal static LogMessage Error(string message) => new(LogType.Error, message);

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary> Creates a simple log message object </summary>
    public LogMessage(LogType type, string message) {
        Type = type;
        Message = message;
    }

    // ReSharper disable once MemberCanBeInternal
    /// <summary> The severity of the log message </summary>
    public LogType Type { get; set; }

    // ReSharper disable once MemberCanBeInternal
    /// <summary> The message </summary>
    public string Message { get; set; }

    /// <summary> implicitly convert message to string </summary>
    public static implicit operator string(LogMessage logMessage) => logMessage.ToString();

    /// <summary> To string </summary>
    public override string ToString() => $"{Type}: {Message}";
}
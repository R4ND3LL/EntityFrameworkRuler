namespace EntityFrameworkRuler.Common;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public abstract class RuleProcessor : IRuleProcessor {
    /// <inheritdoc />
    public event LogMessageHandler OnLog;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected void ResponseOnLog(object sender, LogMessage logMessage) {
        OnLog?.Invoke(this, logMessage);
    }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public interface IRuleProcessor {
    /// <summary> Hook the log message event </summary>
    event LogMessageHandler OnLog;
}
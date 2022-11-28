namespace EntityFrameworkRuler.Common;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public abstract class RuleHandler : IRuleHandler {
    /// <inheritdoc />
    public event LogMessageHandler Log;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected void OnResponseLog(object sender, LogMessage logMessage) {
        Log?.Invoke(this, logMessage);
    }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public interface IRuleHandler {
    /// <summary> Hook the log message event </summary>
    event LogMessageHandler Log;
}
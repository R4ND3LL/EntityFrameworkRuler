namespace EntityFrameworkRuler.Common;

public abstract class RuleProcessor {
    public event LogMessageHandler OnLog;

    protected void ResponseOnLog(object sender, LogMessage logMessage) {
        OnLog?.Invoke(this, logMessage);
    }
}
public interface IRuleProcessor {
    event LogMessageHandler OnLog;
}
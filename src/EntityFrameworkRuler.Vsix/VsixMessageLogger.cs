using System.Diagnostics;
using System.Text;
using EntityFrameworkRuler.Common;

namespace EntityFrameworkRuler;

internal sealed class VsixMessageLogger : MessageLoggerBase {
    private const string paneTitle = "Extensions";
    private static readonly Guid extensionsPaneGuid = new("1780E60C-EE25-482B-AC77-CBA91891C420");
    private static OutputWindowPane pane;

    /// <inheritdoc />
    public override void Write(LogMessage logMessage) {
        var messageBuilder = new StringBuilder();
        switch (logMessage.Type) {
            case LogType.Verbose:
                //messageBuilder.Append("Info: ");
                break;
            case LogType.Information:
                //messageBuilder.Append("Info: ");
                break;
            case LogType.Warning:
                messageBuilder.Append("Warning: ");
                break;
            case LogType.Error:
                messageBuilder.Append("Error: ");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logMessage.Type), logMessage.Type, null);
        }

        messageBuilder.Append(logMessage.Message);
        var msg = messageBuilder.ToString();

        ThreadHelper.JoinableTaskFactory.Run(async () => {
            // Switch to main thread
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (logMessage.Type == LogType.Error) await VS.StatusBar.ShowMessageAsync("Something went wrong");
            await LogAsync(msg);
        });
    }


    /// <summary> Log the message to the Output Window asynchronously. </summary>
    internal static async Task LogAsync(string message) {
        try {
            if (pane == null)
                await EnsurePaneAsync();

            if (pane != null) {
                await pane.WriteLineAsync(message);
#if DEBUG
                Debug.WriteLine(message);
#endif
            } else
                Debug.WriteLine(message);
        } catch (Exception ex) {
            Debug.WriteLine(message);
            Debug.WriteLine(ex);
        }
    }

    private static async Task EnsurePaneAsync() {
        if (pane == null) {
            // Try and get the Extensions pane and if it doesn't exist then create it.
            pane = await VS.Windows.GetOutputWindowPaneAsync(extensionsPaneGuid);

            if (pane == null)
                try {
                    pane = await VS.Windows.CreateOutputWindowPaneAsync(paneTitle);
                } catch (Exception ex) {
                    Debug.WriteLine(ex);
                }
        }

        Debug.Assert(pane != null);
    }
}
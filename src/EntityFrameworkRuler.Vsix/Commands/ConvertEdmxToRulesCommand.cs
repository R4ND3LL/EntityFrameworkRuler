using EntityFrameworkRuler.ToolWindows;

namespace EntityFrameworkRuler.Commands {
    [Command(PackageIds.ConvertEdmxToRulesCommand)]
    internal sealed class ConvertEdmxToRulesCommand : BaseCommand<ConvertEdmxToRulesCommand> {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e) {
            //VS.Settings..
            var dialog = new RulesFromEdmxDialog();
            dialog.ShowDialog();
            //await VS.MessageBox.ShowWarningAsync("Convert Edmx To Rules", "Button clicked");
        }
    }
}

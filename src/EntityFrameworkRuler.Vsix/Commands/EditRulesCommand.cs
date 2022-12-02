using EntityFrameworkRuler.ToolWindows;
using Microsoft.VisualStudio.Shell.Interop;

namespace EntityFrameworkRuler.Commands {
    [Command(PackageIds.EditRulesCommand)]
    internal sealed class EditRulesCommand : BaseCommand<EditRulesCommand> {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e) {
            
            var dialog = new RuleEditorDialog();
            dialog.ShowDialog();
            //await VS.MessageBox.ShowWarningAsync("Edit Rules", "Button clicked");
        }
    }
}

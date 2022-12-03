using EntityFrameworkRuler.Editor.Dialogs;
using EntityFrameworkRuler.ToolWindows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell.Interop;

namespace EntityFrameworkRuler.Commands {
    [Command(PackageIds.EditRulesCommand)]
    internal sealed class EditRulesCommand : BaseCommand<EditRulesCommand> {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e) {
            var dialog = EntityFrameworkRulerPackage.ServiceProvider.GetRequiredService<IRuleEditorDialog>();
            dialog.ShowDialog();
            //await VS.MessageBox.ShowWarningAsync("Edit Rules", "Button clicked");
        }
        protected override void BeforeQueryStatus(EventArgs e) {
            base.BeforeQueryStatus(e);
        }

    }
}

using System.ComponentModel.Design;
using EntityFrameworkRuler.Editor.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using RulesFromEdmxDialog = EntityFrameworkRuler.ToolWindows.RulesFromEdmxDialog;

namespace EntityFrameworkRuler.Commands {
    [Command(PackageIds.ConvertEdmxToRulesCommand)]
    internal sealed class ConvertEdmxToRulesCommand : BaseCommand<ConvertEdmxToRulesCommand> {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e) {
            var dialog = EntityFrameworkRulerPackage.ServiceProvider.GetRequiredService<IRulesFromEdmxDialog>();
            dialog.ShowDialog();
            //await VS.MessageBox.ShowWarningAsync("Convert Edmx To Rules", "Button clicked");
        }
        protected override void BeforeQueryStatus(EventArgs e) {
            base.BeforeQueryStatus(e);
            //var menuCommand = sender as MenuCommand;
            //if (menuCommand == null || (await VS.Solutions.GetActiveItemsAsync()).Count() != 1) {
            //    return;
            //}

            //menuCommand.Visible = false;

            //var project = await VS.Solutions.GetActiveProjectAsync();

            //if (project == null) {
            //    return;
            //}

            //var item = await VS.Solutions.GetActiveItemAsync();

            //if (item == null) {
            //    return;
            //}

            //menuCommand.Visible = IsConfigFile(item.Text) && project.IsCSharpProject();
        }
        private static bool IsConfigFile(string itemName) {
            return itemName != null &&
                   itemName.EndsWith("rules.json", StringComparison.OrdinalIgnoreCase);
        }
    }
}

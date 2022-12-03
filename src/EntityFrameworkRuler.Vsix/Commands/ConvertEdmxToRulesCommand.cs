using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using EntityFrameworkRuler.Editor.Dialogs;
using EntityFrameworkRuler.Extension;
using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.DependencyInjection;
using RulesFromEdmxDialog = EntityFrameworkRuler.ToolWindows.RulesFromEdmxDialog;

namespace EntityFrameworkRuler.Commands {
    [Command(PackageIds.ConvertEdmxToRulesCommand)]
    internal sealed class ConvertEdmxToRulesCommand : BaseCommand<ConvertEdmxToRulesCommand> {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e) {
            try {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var item = await VS.Solutions.GetActiveItemAsync();
                if (item == null || item.Type.NotIn(SolutionItemType.PhysicalFile)) return;
                if (!IsEdmxFile(item.Text)) return;
                var edmxPath = item.FullPath;

                var dialog = EntityFrameworkRulerPackage.ServiceProvider.GetRequiredService<IRulesFromEdmxDialog>();
                dialog.ViewModel.SetContext(edmxPath);
                dialog.ShowDialog();
            } catch (Exception ex) {
                await ex.LogAsync();
            }
            //await VS.MessageBox.ShowWarningAsync("Convert Edmx To Rules", "Button clicked");
        }
        protected override async void BeforeQueryStatus(EventArgs e) {
            try {
                Command.Visible = CanShow();
            } catch (Exception ex) {
                Command.Visible = false;
            }
        }
        private IServiceProvider ServiceProvider => Package;
        public static readonly HashSet<string> SupportedFiles = new(new[] { ".edmx" }, StringComparer.OrdinalIgnoreCase);

        private bool CanShow() {
            if (!ThreadHelper.CheckAccess()) return false;
            var dte = ServiceProvider.GetService(typeof(DTE)) as DTE2;
            var item = dte?.SelectedItems?.Item(1)?.ProjectItem;
            if (item == null) return false;
            var fileExtension = Path.GetExtension(item.Name);
            // Show the button only if a supported file is selected
            return SupportedFiles.Contains(fileExtension);
        }
        //private async Task<bool> CanShowForRuleFile2() {
        //    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        //    var item = await VS.Solutions.GetActiveItemAsync();
        //    if (item == null || item.Type.NotIn(SolutionItemType.PhysicalFile)) return false;
        //    return IsEdmxFile(item.Text);
        //}
        private static bool IsEdmxFile(string itemName) {
            return itemName?.EndsWithIgnoreCase(".edmx") == true;
        }
    }
}

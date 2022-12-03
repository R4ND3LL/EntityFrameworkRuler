using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EntityFrameworkRuler.Editor.Dialogs;
using EntityFrameworkRuler.Extension;
using EntityFrameworkRuler.ToolWindows;
using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell.Interop;

namespace EntityFrameworkRuler.Commands {
    [Command(PackageIds.EditRulesCommand)]
    internal sealed class EditRulesCommand : BaseCommand<EditRulesCommand> {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e) {
            try {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var item = await VS.Solutions.GetActiveItemAsync();
                if (item == null || item.Type.NotIn(SolutionItemType.PhysicalFile)) return;
                if (!IsRuleFile(item.Text)) return;
                var rulesPath = item.FullPath;
                var project = item.FindParent(SolutionItemType.Project);
                var projectPath = project?.FullPath;
                if (projectPath?.Length > 0) projectPath = Path.GetDirectoryName(project?.FullPath);
                var dialog = EntityFrameworkRulerPackage.ServiceProvider.GetRequiredService<IRuleEditorDialog>();
                dialog.ViewModel.SetContext(rulesPath, projectPath);
                dialog.ShowDialog();
            } catch (Exception ex) {
                await ex.LogAsync();
            }
            //await VS.MessageBox.ShowWarningAsync("Edit Rules", "Button clicked");
        }
        protected override async void BeforeQueryStatus(EventArgs e) {
            try {
                Command.Visible = CanShow();
            } catch (Exception ex) {
                Command.Visible = false;
                ex.Log();
            }
        }
        private IServiceProvider ServiceProvider => Package;
        public static readonly HashSet<string> SupportedFiles = new(new[] { ".json" }, StringComparer.OrdinalIgnoreCase);

        private bool CanShow() {
            if (!ThreadHelper.CheckAccess()) return false;
            var dte = ServiceProvider.GetService(typeof(DTE)) as DTE2;
            var item = dte?.SelectedItems?.Item(1)?.ProjectItem;
            if (item == null) return false;
            var fileExtension = Path.GetExtension(item.Name);
            // Show the button only if a supported file is selected
            return SupportedFiles.Contains(fileExtension);
        }
        //private async Task<bool> CanShowForRuleFile() {
        //    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        //    var item = await VS.Solutions.GetActiveItemAsync();
        //    if (item == null) return false;
        //    return IsRuleFile(item.Text);
        //}
        private static bool IsRuleFile(string itemName) {
            return itemName?.EndsWithIgnoreCase("rules.json") == true;
        }
    }
}

using EntityFrameworkRuler.Editor.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkRuler.Commands; 

[Command(PackageIds.EditRulesCommand)]
internal sealed class EditRulesCommand : RulerBaseCommand<EditRulesCommand> {
    public EditRulesCommand() {
        SupportedFiles.Add(".json");
    }

    protected override Task ExecuteAsyncCore(OleMenuCmdEventArgs oleMenuCmdEventArgs, SolutionItem item) {
        var rulesPath = item.FullPath;
        var project = item.FindParent(SolutionItemType.Project);
        var projectPath = project?.FullPath;
        if (projectPath?.Length > 0) projectPath = Path.GetDirectoryName(projectPath);
        var dialog = EntityFrameworkRulerPackage.ServiceProvider.GetRequiredService<IRuleEditorDialog>();
        dialog.ShowInTaskbar = true;
        dialog.ViewModel.SetContext(rulesPath, projectPath);
        dialog.Show();
        return Task.CompletedTask;
    }
}
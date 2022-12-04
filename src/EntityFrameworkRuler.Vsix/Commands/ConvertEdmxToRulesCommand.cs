using EntityFrameworkRuler.Editor.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkRuler.Commands; 

[Command(PackageIds.ConvertEdmxToRulesCommand)]
internal sealed class ConvertEdmxToRulesCommand : RulerBaseCommand<ConvertEdmxToRulesCommand> {
    public ConvertEdmxToRulesCommand() {
        SupportedFiles.Add(".edmx");
    }

    protected override Task ExecuteAsyncCore(OleMenuCmdEventArgs oleMenuCmdEventArgs, SolutionItem item) {
        var edmxPath = item.FullPath;
        var dialog = EntityFrameworkRulerPackage.ServiceProvider.GetRequiredService<IRulesFromEdmxDialog>();
        dialog.ViewModel.SetContext(edmxPath);
        dialog.ShowDialog();
        return Task.CompletedTask;
    }
}
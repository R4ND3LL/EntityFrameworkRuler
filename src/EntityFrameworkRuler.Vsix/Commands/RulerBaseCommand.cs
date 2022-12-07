using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using EntityFrameworkRuler.Extension;
using EnvDTE;
using EnvDTE80;

namespace EntityFrameworkRuler.Commands;

internal abstract class RulerBaseCommand<T> : BaseCommand<T> where T : class, new() {
    /// <summary> VS service provider. NOT the one used for services of this extension. </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    protected IServiceProvider VsServiceProvider => base.Package;
    protected IServiceProvider ServiceProvider => EntityFrameworkRulerPackage.ServiceProvider;
    protected new EntityFrameworkRulerPackage Package => (EntityFrameworkRulerPackage)base.Package;

    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly HashSet<string> SupportedFiles = new(StringComparer.OrdinalIgnoreCase);

    protected sealed override async Task ExecuteAsync(OleMenuCmdEventArgs e) {
        try {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await Package.InitializeRulerAsync();

            SolutionItem item = null;
            if (SupportedFiles.Count > 0) {
                item = await VS.Solutions.GetActiveItemAsync();
                if (item == null || item.Type.NotIn(SolutionItemType.PhysicalFile)) return;
                var fileExtension = Path.GetExtension(item.Name);
                if (!SupportedFiles.Contains(fileExtension)) {
                    item = null; // item rejected due to invalid type. continue to open the form anyway
                }
            }
            await ExecuteCoreAsync(e, item);
        } catch (Exception ex) {
            await ex.LogAsync();
            await VS.MessageBox.ShowErrorAsync("Failed to open form: " + ex.Message);
        }
    }

    protected abstract Task ExecuteCoreAsync(OleMenuCmdEventArgs oleMenuCmdEventArgs, SolutionItem solutionItem);

    protected override void BeforeQueryStatus(EventArgs e) {
        try {
            Command.Visible = CanShow();
        } catch (Exception ex) {
            Command.Visible = false;
            ex.Log();
        }
#if DEBUG
        Debug.WriteLine($"{GetType().Name} Visible={Command.Visible}");
#endif
    }

    [SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread")]
    private bool CanShow() {
        if (!ThreadHelper.CheckAccess()) return false;
        var dte = VsServiceProvider.GetService(typeof(DTE)) as DTE2;
        var item = dte?.SelectedItems?.Item(1)?.ProjectItem;
        if (item == null) return false;
        var fileExtension = Path.GetExtension(item.Name);
        // Show the button only if a supported file is selected
        return SupportedFiles.Contains(fileExtension);
    }
}
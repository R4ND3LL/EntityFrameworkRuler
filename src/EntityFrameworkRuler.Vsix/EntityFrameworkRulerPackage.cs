global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using System.Linq;
using EntityFrameworkRuler.Editor.Controls;
using EntityFrameworkRuler.Editor.Extensions;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using System.Threading;
using EntityFrameworkRuler.Extensions;
using EntityFrameworkRuler.Editor.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio;

namespace EntityFrameworkRuler {
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.EntityFrameworkRulerString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    //[ProvideAutoLoad(PackageGuids.UIContextGuidString, PackageAutoLoadFlags.BackgroundLoad)]
    //[ProvideUIContextRule(PackageGuids.UIContextGuidString,
    //    name: "Supported Files",
    //    expression: "Json | Edmx",
    //    termNames: new[] { "Json", "Edmx" },
    //    termValues: new[] { "HierSingleSelectionName:.json$", "HierSingleSelectionName:.edmx$" })]
    [ProvideBindingPath]
    public sealed class EntityFrameworkRulerPackage : ToolkitPackage {
        internal static IServiceProvider ServiceProvider { get; private set; }
        private readonly Type[] dependencies;

        public EntityFrameworkRulerPackage() {
            dependencies = new Type[] {
                typeof(System.ComponentModel.DisplayNameAttribute),
                typeof(System.ComponentModel.DataAnnotations.MaxLengthAttribute),
            };
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
            try {
#if DEBUG
                Enumerable.Range(0, 10).ForAll(o => Debug.WriteLine($"EntityFrameworkRulerPackage INITIALIZING"));
#endif
                VsixExtensions.VsixAssemblyResolver.RedirectAssembly();
                if (!System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Launch();
                await this.RegisterCommandsAsync();

                await GetThemeInfo();

                ServiceProvider ??= CreateServiceProvider();
            } catch (Exception ex) {
                await ex.LogAsync();
            }
        }

        private async Task GetThemeInfo() {
            try {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var shell5 = GetService(typeof(SVsUIShell)) as IVsUIShell5;
                Debug.Assert(shell5 != null, "failed to get IVsUIShell5");

                var map = new Dictionary<ThemeResourceKey, ResourceKeys> {
                    { EnvironmentColors.ToolWindowBackgroundColorKey, ResourceKeys.WindowBackground },
                    { EnvironmentColors.ToolWindowTextColorKey, ResourceKeys.InputText },
                };

                var backColor = shell5.GetThemedWPFColor(EnvironmentColors.DarkColorKey);
                var foreColor = shell5.GetThemedWPFColor(EnvironmentColors.PanelTextColorKey);
                var isLight = backColor.GetBrightness() > foreColor.GetBrightness();

                AppearanceManager.Current.SelectedTheme = isLight ? ThemeNames.Light : ThemeNames.Dark;
                //return;
                foreach (var kvp in map) {
                    var a = shell5.GetThemedWPFColor(kvp.Key);
                    var b = VSColorTheme.GetThemedColor(kvp.Key).ToMediaColor();
                    Debug.Assert(a == b);
                    var isSet = AppearanceManager.Current.TrySetResourceValue(kvp.Value, a.ToBrush());
                    Debug.Assert(isSet);
                }

            } catch (Exception ex) {
                Debug.WriteLine($"GetThemeInfo error: {ex.Message}");
                await ex.LogAsync();
            }
        }


        private IServiceProvider CreateServiceProvider() {
            var services = new ServiceCollection()
                .AddRulerCommon()
                .AddTransient<RuleEditorViewModel, RuleEditorViewModel>()
                .AddTransient<RulesFromEdmxViewModel, RulesFromEdmxViewModel>()
                .AddTransient<IRuleEditorDialog, EntityFrameworkRuler.ToolWindows.RuleEditorDialog>()
                .AddTransient<IRulesFromEdmxDialog, EntityFrameworkRuler.ToolWindows.RulesFromEdmxDialog>();
            return services.BuildServiceProvider();
        }
    }
}
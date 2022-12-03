global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using EntityFrameworkRuler.Editor.Controls;
using EntityFrameworkRuler.Editor.Extensions;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using EntityFrameworkRuler.Extensions;
using System.Collections.Generic;
using EntityFrameworkRuler.Editor.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkRuler {
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.EntityFrameworkRulerString)]
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
            VsixExtensions.VsixAssemblyResolver.RedirectAssembly();
            if (!System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debugger.Launch();
            await this.RegisterCommandsAsync();

            await GetThemeInfo();

            ServiceProvider ??= CreateServiceProvider();
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
            }
        }


        private IServiceProvider CreateServiceProvider() {
            var services = new ServiceCollection();
            services.AddRulerCommon();
            services.AddTransient<IRuleEditorDialog, EntityFrameworkRuler.ToolWindows.RuleEditorDialog>();
            services.AddTransient<IRulesFromEdmxDialog, EntityFrameworkRuler.ToolWindows.RulesFromEdmxDialog>();
            return services.BuildServiceProvider();
        }
    }
}
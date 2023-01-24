global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EntityFrameworkRuler.Editor.Controls;
using EntityFrameworkRuler.Editor.Extensions;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using System.Threading;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Extensions;
using EntityFrameworkRuler.Editor.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio;
using System.Reflection;
using EntityFrameworkRuler.Applicator;
using EntityFrameworkRuler.Extension;
using EntityFrameworkRuler.Generator;
using EntityFrameworkRuler.Generator.EdmxModel;
using EntityFrameworkRuler.Generator.Services;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Saver;

namespace EntityFrameworkRuler;

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
            Enumerable.Range(0, 5).ForAll(o => Debug.WriteLine($"EntityFrameworkRulerPackage INITIALIZING"));
#endif
            await this.RegisterCommandsAsync();
#if DEBUG
            var commonAssembly = typeof(LoadOptions).Assembly;
            var ver = commonAssembly?.GetName()?.Version;
            await VsixMessageLogger.LogAsync($"Common assembly v{ver} loaded");
#endif
        } catch (Exception ex) {
            await ex.LogAsync();
            await VS.MessageBox.ShowErrorAsync("Failed to initialize: " + ex.Message);
        }
    }

    internal async Task InitializeRulerAsync() {
        try {
#if DEBUG
            Enumerable.Range(0, 5).ForAll(o => Debug.WriteLine($"EntityFrameworkRulerPackage INITIALIZING ACTUAL RULER RESOURCES"));
#endif
            //var n = DependencyInjection.AddRuler(null);
            //Debug.Assert(n == null);

            VsixAssemblyResolver.RedirectAssembly();
            if (!themeInitialized) await GetThemeInfoAsync();
            ServiceProvider ??= CreateServiceProvider();
        } catch (Exception ex) {
            await ex.LogAsync();
            await VS.MessageBox.ShowErrorAsync("Failed to initialize Ruler: " + ex.Message);
        }
    }

    private static bool themeInitialized = false;

    private async Task GetThemeInfoAsync() {
        if (themeInitialized) return;
        try {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            lock (this) {
                if (themeInitialized) return;
                themeInitialized = true;
#pragma warning disable VSTHRD103
                var shell5 = GetService(typeof(SVsUIShell)) as IVsUIShell5;
#pragma warning restore VSTHRD103
                Debug.Assert(shell5 != null, "failed to get IVsUIShell5");
                if (shell5 != null) {
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
                        var b = VSColorTheme.GetThemedColor(kvp.Key).ToMediaColor();
                        var isSet = AppearanceManager.Current.TrySetResourceValue(kvp.Value, b.ToBrush());
                        Debug.Assert(isSet);
                    }
                }
            }
        } catch (Exception ex) {
            Debug.WriteLine($"GetThemeInfo error: {ex.Message}");
            await ex.LogAsync();
        }
    }


    private static IServiceProvider CreateServiceProvider() {
        var services = new ServiceCollection()
            .AddSingleton<IRuleSerializer, JsonRuleSerializer>()
            .AddTransient<IRulerNamingService, RulerNamingService>()
            .AddSingleton<IRulerPluralizer, HumanizerPluralizer>()
            .AddSingleton<IMessageLogger, VsixMessageLogger>()
            .AddTransient<IEdmxParser, EdmxParser>()
            .AddTransient<IRuleSaver, RuleSaver>()
            .AddTransient<IRuleLoader, RuleLoader>()
            .AddTransient<IRuleGenerator, RuleGenerator>()
            .AddTransient<IRuleApplicator, RuleApplicator>()
            .AddTransient<RuleEditorViewModel, RuleEditorViewModel>()
            .AddTransient<RulesFromEdmxViewModel, RulesFromEdmxViewModel>()
            .AddTransient<IRuleEditorDialog, EntityFrameworkRuler.ToolWindows.RuleEditorDialog>()
            .AddTransient<IRulesFromEdmxDialog, EntityFrameworkRuler.ToolWindows.RulesFromEdmxDialog>();
        return services.BuildServiceProvider();
    }
}

/// <summary> This is a workaround for the runtime error where Annotations assembly can't be loaded when deserializing the json model </summary>
internal static class VsixAssemblyResolver {
    static VsixAssemblyResolver() {
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    }

    private static readonly HashSet<string> skip = new(new[] { "EntityFrameworkRuler.Common.XmlSerializers" });
    public static void RedirectAssembly() { }

    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
        var requestedAssembly = new AssemblyName(args.Name);
        Assembly assembly = null;
        AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
        try {
            if (skip.Contains(requestedAssembly.Name)) return null;
            assembly = Assembly.Load(requestedAssembly.Name);
        } catch (Exception ex) {
            Debug.WriteLine("AssemblyResolve error: " + ex.Message);
            ex.Log("AssemblyResolve error");
        }

        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        return assembly;
    }
}
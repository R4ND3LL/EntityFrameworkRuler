using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Editor.Controls;
using EntityFrameworkRuler.Editor.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkRuler.Editor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public sealed partial class App : Application {
    protected override void OnStartup(StartupEventArgs e) {
        var sp = CreateServiceProvider();
        base.OnStartup(e);
        var window = sp.GetRequiredService<IRuleEditorDialog>();

#if DEBUG
        var sln = Directory.GetCurrentDirectory().FindSolutionParentPath();
        if (sln != null) {
            sln = Path.Combine(sln, "Tests\\\\NorthwindTestDesign\\");
            if (Directory.Exists(sln)) window.ViewModel.SetContext(null, sln);
        }
#endif

        window.Show();
    }

    private IServiceProvider CreateServiceProvider() {
        var services = new ServiceCollection()
            .AddRuler()
            .AddTransient<RuleEditorViewModel, RuleEditorViewModel>()
            .AddTransient<RulesFromEdmxViewModel, RulesFromEdmxViewModel>()
            .AddTransient<IRuleEditorDialog, RuleEditorDialog>()
            .AddTransient<IRulesFromEdmxDialog, RulesFromEdmxDialog>();
        return services.BuildServiceProvider();
    }
}
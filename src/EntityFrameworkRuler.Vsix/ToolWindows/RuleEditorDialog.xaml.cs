using System.IO;
using EntityFrameworkRuler.Editor.Controls;
using EntityFrameworkRuler.Extension;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Saver;
using Microsoft.VisualStudio.PlatformUI;
// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.ToolWindows {
    public sealed partial class RuleEditorDialog : DialogWindow {
        public RuleEditorViewModel ViewModel { get; }

        public RuleEditorDialog() : this(null) {
        }

        public RuleEditorDialog(ThemeNames? theme) : this(null, null, null) {
            if (theme.HasValue) Theme = theme.Value;
            else if (!Theme.HasValue) Theme = ThemeNames.Light;
        }

        public RuleEditorDialog(IRuleLoader loader, IRuleSaver saver, string ruleFilePath, string targetProjectPath = null) {
            InitializeComponent();
#if DEBUG
            if (targetProjectPath.IsNullOrEmpty()) {
                var sln = Directory.GetCurrentDirectory().FindSolutionParentPath();
                if (sln != null) {
                    sln = Path.Combine(sln, "Tests\\NorthwindTestProject\\");
                    if (Directory.Exists(sln)) targetProjectPath = sln;
                }
            }
#endif
            DataContext = ViewModel = new(loader, saver, ruleFilePath, targetProjectPath);
            if (!Theme.HasValue) Theme = ThemeNames.Light;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public ThemeNames? Theme {
            get => AppearanceManager.Current.SelectedTheme;
            set => AppearanceManager.Current.SelectedTheme = value;
        }
        protected override void OnDialogThemeChanged() {
            base.OnDialogThemeChanged();
        }

    }
}

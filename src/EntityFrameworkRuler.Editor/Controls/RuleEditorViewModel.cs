using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntityFrameworkRuler.Editor.Dialogs;
using EntityFrameworkRuler.Editor.Models;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Rules;
using EntityFrameworkRuler.Saver;

namespace EntityFrameworkRuler.Editor.Controls;

public sealed partial class RuleEditorViewModel : ObservableObject {
    private readonly IRuleLoader loader;
    private readonly IRuleSaver saver;

    public RuleEditorViewModel(IRuleLoader loader, IRuleSaver saver, string ruleFilePath = null, string targetProjectPath = null) {
        this.loader = loader ?? new RuleLoader();
        this.saver = saver ?? new RuleSaver();
        SuggestedRuleFiles = new();
        if (ruleFilePath.HasNonWhiteSpace() && ruleFilePath.EndsWithIgnoreCase(".json")) {
            SuggestedRuleFiles.Add(new(new(ruleFilePath.Trim())));
            SelectedRuleFile = SuggestedRuleFiles[0];
        } else if (targetProjectPath.IsNullOrWhiteSpace() && ruleFilePath.HasNonWhiteSpace()) {
            targetProjectPath = ruleFilePath;
        }

        if (targetProjectPath.HasNonWhiteSpace()) {
            //LoadOptions = new(targetProjectPath);
            if (SuggestedRuleFiles.Count == 0) FindRuleFilesNear(targetProjectPath);
        }
    }

    [ObservableProperty] private ObservableCollection<ObservableFileInfo> suggestedRuleFiles;
    [ObservableProperty] private ObservableFileInfo selectedRuleFile;
    [ObservableProperty] private DbContextRule dbContextRule;
    [ObservableProperty] private RuleNodeViewModel rootModel;

    public IEnumerable<RuleNodeViewModel> Root {
        get {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (RootModel != null) yield return RootModel;
        }
    }

    partial void OnSelectedRuleFileChanged(ObservableFileInfo value) {
        _ = InitializeRootModel(value);
    }

    partial void OnRootModelChanged(RuleNodeViewModel value) {
        OnPropertyChanged(nameof(Root));
        RootModel.IsSelected = true;
    }

    private async Task InitializeRootModel(ObservableFileInfo value) {
        try {
            if (value?.FileInfo == null || !value.FileInfo.Exists) {
                DbContextRule = null;
                return;
            }

            var selected = RootModel?.Selection?.Node;
            var selectPath = (selected?.EnumerateParents())?.Reverse().Select(o => o.Name).ToArray();

            var ops = new LoadOptions(value.Path);
            var response = await loader.LoadRulesInProjectPath(ops).ConfigureAwait(true);
            if (response.Errors.Any()) {
                MessageBox.Show(response.Errors.Join(Environment.NewLine), "Something went wrong", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            DbContextRule = response.Rules?.OfType<DbContextRule>().FirstOrDefault(o => o.Schemas?.Count > 0);
            if (DbContextRule == null) {
                RootModel = null;
                return;
            }

            RootModel = new(DbContextRule, null, new(), true);

            if (selectPath?.Length > 0) {
                // try to maintain selection
                var items = Root;
                RuleNodeViewModel item = null;
                foreach (var p in selectPath) {
                    var temp = items.FirstOrDefault(o => o.Name == p);
                    if (temp == null) {
                        break;
                    }

                    item = temp;
                    items = temp.Children.Cast<RuleNodeViewModel>();
                }

                if (item != null) {
                    item.ExpandParents();
                    item.IsSelected = true;
                }
            }
        } catch (Exception ex) {
            MessageBox.Show(ex.Message, "Something went wrong", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private async void FindRuleFilesNear(string path) {
        try {
            if (path.EndsWithIgnoreCase(".csproj") || path.EndsWithIgnoreCase(".edmx") || path.EndsWithIgnoreCase(".json"))
                path = new FileInfo(path).Directory?.FullName;
            if (path.IsNullOrWhiteSpace()) return;
            var ops = new LoadOptions(path);
            var mask = ops.DbContextRulesFile.Replace("<ContextName>", "*", StringComparison.OrdinalIgnoreCase);

            var files = await Task.Factory.StartNew(() => path
                    .FindFiles(mask, true, 2))
                .ConfigureAwait(true);
            files.Select(o => new ObservableFileInfo(o)).ForAll(o => SuggestedRuleFiles.Add(o));
            if (SuggestedRuleFiles?.Count == 1) SelectedRuleFile = SuggestedRuleFiles[0];
        } catch {
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task Undo() {
        if (SelectedRuleFile == null) return Task.CompletedTask;
        return InitializeRootModel(SelectedRuleFile);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task Save() {
        try {
            if (SelectedRuleFile == null || RootModel == null) return;
            var root = RootModel;
            var model = root.Item;
            var errors = root.Validate(true);
            if (errors.Count > 0) {
                if (errors.Count > 1) {
                    MessageBox.Show($"Fix {errors.Count} validation errors first.", "Validation Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                } else {
                    MessageBox.Show(errors[0].Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return;
            }

            var file = SelectedRuleFile.FileInfo;
            var path = file.Directory?.FullName;
            var response = await saver.SaveRules(projectBasePath: path, file.FullName, (IRuleModelRoot)model);
            if (response.Errors.Any()) {
                MessageBox.Show(response.Errors.Join(Environment.NewLine), "Something went wrong", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            } else {
                // success
                var savedPath = response.SavedRules.FirstOrDefault();
                Debug.Assert(savedPath == file.FullName);
            }
        } catch (Exception ex) {
            MessageBox.Show(ex.Message, "Something went wrong", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ClearSearch() {
        // ReSharper disable once ConstantConditionalAccessQualifier
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (RootModel?.Filter?.Term == null) return;
        RootModel.Filter.Term = null;
    }

    [RelayCommand]
    private void OpenRule() {
        // Configure open file dialog box
        var dialog = new Microsoft.Win32.OpenFileDialog {
            FileName = "Document", // Default file name
            DefaultExt = ".json", // Default file extension
            Filter = "DbContext Rules (.json)|*.json",
            Title = "Select an json file containing DB context rules" // Filter files by extension
        };
        // Show open file dialog box
        var result = dialog.ShowDialog();

        // Process open file dialog box results
        if (result != true) return;
        // Open document
        var filename = dialog.FileName;

        var observableFileInfo = new ObservableFileInfo(new(filename));
        if (SuggestedRuleFiles.All(o => !string.Equals(o.Path, observableFileInfo.Path, StringComparison.OrdinalIgnoreCase)))
            SuggestedRuleFiles.Add(observableFileInfo);
        SelectedRuleFile = observableFileInfo;
    }

    [RelayCommand]
    private void ConvertEdmx() {
        var rule = SelectedRuleFile ?? SuggestedRuleFiles.FirstOrDefault(o => o.Path.HasNonWhiteSpace() && o.FileInfo.Exists);
        var projectPath = rule?.FileInfo?.Directory?.FullName;
        var edmxConverter = new RulesFromEdmxDialog(null, null, projectPath);
        try {
            edmxConverter.Owner = Application.Current.MainWindow;
            edmxConverter.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        } catch { }

        var result = edmxConverter.ShowDialog();
        if (result != true) return;
        if (edmxConverter.Tag is SaveRulesResponse response && response.SavedRules.Count > 0) {
            var path = response.SavedRules.FirstOrDefault(o => o.HasNonWhiteSpace());
            if (path.HasNonWhiteSpace()) {
                SelectedRuleFile = null; // force reload
                SelectedRuleFile = new(new(path));
            }
        }
    }
}
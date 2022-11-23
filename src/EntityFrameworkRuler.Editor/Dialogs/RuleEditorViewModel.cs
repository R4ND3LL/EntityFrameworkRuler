using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntityFrameworkRuler.Editor.Models;
using EntityFrameworkRuler.Generator;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Rules;
using EntityFrameworkRuler.Saver;

namespace EntityFrameworkRuler.Editor.Dialogs;

public sealed partial class RuleEditorViewModel : ObservableObject {
    public RuleEditorViewModel(string ruleFilePath = null, string targetProjectPath = null) {
        SuggestedRuleFiles = new ObservableCollection<ObservableFileInfo>();
        if (ruleFilePath.HasNonWhiteSpace() && ruleFilePath.EndsWithIgnoreCase(".json")) {
            SuggestedRuleFiles.Add(new ObservableFileInfo(new FileInfo(ruleFilePath.Trim())));
            SelectedRuleFile = SuggestedRuleFiles[0];
        } else if (targetProjectPath.IsNullOrWhiteSpace() && ruleFilePath.HasNonWhiteSpace()) {
            targetProjectPath = ruleFilePath;
        }

        if (targetProjectPath.HasNonWhiteSpace()) {
            if (SuggestedRuleFiles.Count == 0) FindRuleFilesNear(targetProjectPath);
        }
    }

    [ObservableProperty] private ObservableCollection<ObservableFileInfo> suggestedRuleFiles;

    [ObservableProperty] private RuleFileNameOptions fileNameOptions;
    [ObservableProperty] private ObservableFileInfo selectedRuleFile;
    [ObservableProperty] private DbContextRule dbContextRule;

    [ObservableProperty] private RuleNodeViewModel rootModel;
    //[ObservableProperty] private RuleNodeViewModel selectedNode;

    public IEnumerable<RuleNodeViewModel> Root {
        get {
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
            var selectPath = selected?.EnumerateParents().Reverse().Select(o => o.Name).ToArray();

            var sb = new StringBuilder();
            var hasError = false;
            var loader = new RuleLoader(new LoadOptions() { ProjectBasePath = value.Path });
            loader.OnLog += GeneratorOnLog;
            FileNameOptions ??= new RuleFileNameOptions();
            var response = await loader.LoadRulesInProjectPath(FileNameOptions).ConfigureAwait(true);
            response.OnLog -= GeneratorOnLog;
            if (response.Errors.Any()) {
                MessageBox.Show(response.Errors.Join(Environment.NewLine), "Something went wrong", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            DbContextRule = response.Rules?.OfType<DbContextRule>().FirstOrDefault(o => o.Schemas?.Count > 0);
            if (DbContextRule == null) {
                RootModel = null;
                return;
            }

            RootModel = new RuleNodeViewModel(DbContextRule, null, new Models.TreeFilter(), true);

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

            void GeneratorOnLog(object sender, Common.LogMessage logMessage) {
                if (!hasError && logMessage.Type == Common.LogType.Error) hasError = true;
                sb.AppendLine(logMessage.ToString());
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
            FileNameOptions ??= new RuleFileNameOptions();
            var mask = FileNameOptions.DbContextRulesFile.Replace("<ContextName>", "*", StringComparison.OrdinalIgnoreCase);

            var files = await Task.Factory.StartNew(() => "G:\\!DEV\\EdmxRuler\\src\\Tests\\NorthwindTestProject\\"
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
                    MessageBox.Show(errors[0].Msg, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return;
            }

            var sb = new StringBuilder();
            var hasError = false;
            var file = SelectedRuleFile.FileInfo;
            var path = file.Directory.FullName;
            var saver = new RuleSaver(new SaveOptions() { ProjectBasePath = path });
            saver.OnLog += GeneratorOnLog;
            var response = await saver.TrySaveRules((IRuleModelRoot)model, path,
                new RuleFileNameOptions() { DbContextRulesFile = file.FullName });
            saver.OnLog -= GeneratorOnLog;
            if (response.Errors.Any()) {
                MessageBox.Show(response.Errors.Join(Environment.NewLine), "Something went wrong", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            } else {
                // success
                var savedPath = response.SavedRules.FirstOrDefault();
                Debug.Assert(savedPath == file.FullName);
            }

            void GeneratorOnLog(object sender, Common.LogMessage logMessage) {
                if (!hasError && logMessage.Type == Common.LogType.Error) hasError = true;
                sb.AppendLine(logMessage.ToString());
            }
        } catch (Exception ex) {
            MessageBox.Show(ex.Message, "Something went wrong", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ClearSearch() {
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
        SelectedRuleFile = new ObservableFileInfo(new FileInfo(filename));
    }

    [RelayCommand]
    private void ConvertEdmx() {
        var rule = SelectedRuleFile ?? SuggestedRuleFiles.FirstOrDefault(o => o.Path.HasNonWhiteSpace() && o.FileInfo.Exists);
        var projectPath = rule?.FileInfo?.Directory?.FullName;
        var edmxConverter = new RulesFromEdmxDialog(null, projectPath);
        try {
            edmxConverter.Owner = App.Current.MainWindow;
            edmxConverter.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        } catch { }

        var result = edmxConverter.ShowDialog();
        if (result != true) return;
        if (edmxConverter.Tag is SaveRulesResponse response && response.SavedRules.Count > 0) {
            var path = response.SavedRules.FirstOrDefault(o => o.HasNonWhiteSpace());
            if (path.HasNonWhiteSpace()) {
                SelectedRuleFile = null; // force reload
                SelectedRuleFile = new ObservableFileInfo(new FileInfo(path));
            }
        }
    }
}
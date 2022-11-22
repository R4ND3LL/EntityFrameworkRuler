using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EntityFrameworkRuler.Editor.Models;

public sealed partial class ObservableFileInfo : ObservableObject {
    public ObservableFileInfo(FileInfo fileInfo) {
        this.FileInfo = fileInfo;
    }

    [ObservableProperty] private FileInfo fileInfo;
    [ObservableProperty] private string name;
    [ObservableProperty] private string path;

    partial void OnFileInfoChanged(FileInfo value) {
        name = value.Name;
        path = value.FullName;
    }

    public override string ToString() {
        return path;
    }
}

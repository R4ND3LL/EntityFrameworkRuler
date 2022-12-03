using System.Windows;
using EntityFrameworkRuler.Editor.Controls;

namespace EntityFrameworkRuler.Editor.Dialogs;

public interface IDialog {
    /// <summary>Opens a window and returns without waiting for the newly opened window to close.</summary>
    /// <exception cref="T:System.InvalidOperationException">
    /// <see cref="M:System.Windows.Window.Show" /> is called on a window that is closing (<see cref="E:System.Windows.Window.Closing" />) or has been closed (<see cref="E:System.Windows.Window.Closed" />).</exception>
    void Show();

    /// <summary>Opens a window and returns only when the newly opened window is closed.</summary>
    /// <exception cref="T:System.InvalidOperationException">
    /// <see cref="M:System.Windows.Window.ShowDialog" /> is called on a window that is closing (<see cref="E:System.Windows.Window.Closing" />) or has been closed (<see cref="E:System.Windows.Window.Closed" />).</exception>
    /// <returns>A <see cref="T:System.Nullable`1" /> value of type <see cref="T:System.Boolean" /> that specifies whether the activity was accepted (<see langword="true" />) or canceled (<see langword="false" />). The return value is the value of the <see cref="P:System.Windows.Window.DialogResult" /> property before a window closes.</returns>
    bool? ShowDialog();

    ThemeNames? Theme { get; set; }
    Window Owner { get; set; }
    WindowStartupLocation WindowStartupLocation { get; set; }
    object Tag { get; set; }

}

public interface IRuleEditorDialog : IDialog {
    RuleEditorViewModel ViewModel { get; }
}

public interface IRulesFromEdmxDialog : IDialog {
    RulesFromEdmxViewModel ViewModel { get; }
}
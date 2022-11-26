using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EntityFrameworkRuler.Editor.Controls;
/// <summary> Predefined application themes  </summary>
public enum ThemeNames {
    Light = 0,
    Dark = 1,
}
public sealed partial class AppearanceManager : ObservableObject {
    public static AppearanceManager Current { get; private set; } = new();
    private AppearanceManager() { }

    [ObservableProperty] private ThemeNames selectedTheme;
    [ObservableProperty] private bool isSettingTheme;

    public Brush Accent2 => TryGetResourceValue<Brush>(nameof(Accent2));
    public Brush BlackColor => TryGetResourceValue<Brush>(nameof(BlackColor));
    public Brush WhiteColor => TryGetResourceValue<Brush>(nameof(WhiteColor));
    public Brush WindowBackground => TryGetResourceValue<Brush>(nameof(WindowBackground));
    public Brush InputBackground => TryGetResourceValue<Brush>(nameof(InputBackground));
    public Brush InputText => TryGetResourceValue<Brush>(nameof(InputText));
    public Brush GrayBrush8 => TryGetResourceValue<Brush>(nameof(GrayBrush8));
    public FontFamily InputFontFamily => TryGetResourceValue<FontFamily>(nameof(InputFontFamily));


    partial void OnSelectedThemeChanged(ThemeNames value) {
        SetThemeSource(value);
    }

    /// <summary> main function for changing current theme </summary>
    private void SetThemeSource(ThemeNames themeLink) {
        var hasResources = HasUiResources();
        if (!hasResources) return;

        if (Application.Current?.Dispatcher?.CheckAccess() == false) {
            Application.Current.Dispatcher.InvokeAsync(() => SetThemeSource(themeLink));
            return;
        }

        IsSettingTheme = true;
        try {
            SelectedTheme = themeLink; // ensure property is updated otherwise reentry may occur.

            var oldThemeDict = GetThemeDictionary();
            var dictionaries = Application.Current.Resources.MergedDictionaries;

            var uri = themeLink switch {
                ThemeNames.Light => ThemeUri.LightUri,
                ThemeNames.Dark => ThemeUri.DarkUri,
                _ => throw new ArgumentOutOfRangeException(nameof(themeLink), themeLink, null)
            };

            var themeDict = new ResourceDictionary { Source = uri };

            // add new before removing old theme to avoid DynamicResource not found warnings
            dictionaries.Add(themeDict);

            // remove old theme
            if (oldThemeDict != null) dictionaries.Remove(oldThemeDict);

            OnPropertyChanged(nameof(SelectedTheme));
        } finally {
            IsSettingTheme = false;
        }
    }

    private bool HasUiResources() {
        var r = Application.Current?.Resources;
        if (r == null) return false;
        return true;
    }

    public T TryGetResourceValue<T>(string key) => TryGetResourceValue<T>(key, out var v) ? v : default;

    public bool TryGetResourceValue<T>(string key, out T value) {
        value = default;
        if (!HasUiResources()) return false;
        try {
            var o = Application.Current.TryFindResource(key); //If the requested resource is not found, a null reference is returned.
            var resourceExists = o != null;
            if (!resourceExists) return false;

            if (o is T t) value = t;
            else if (o != null) throw new($"{key} resource is not a {typeof(T).Name}. It is a {o.GetType().Name}");
            return true;
        } catch { return false; }
    }
    private ResourceDictionary GetThemeDictionary() {
        // determine the current theme by looking at the app resources and return the first dictionary having the resource key 'WindowBackground' defined.
        return (from dict in Application.Current.Resources.MergedDictionaries
                where dict.Contains("WindowBackground")
                select dict).FirstOrDefault();
    }
    private Uri GetThemeSource() {
        var dict = GetThemeDictionary();
        return dict?.Source;
        // could not determine the theme dictionary
    }
}
/// <summary>  </summary>
public class ThemeUri {
    public static Uri DarkUri = new("/EntityFrameworkRuler.Editor;component/Themes/Dark.xaml", UriKind.Relative);
    public static Uri LightUri = new("/EntityFrameworkRuler.Editor;component/Themes/Light.xaml", UriKind.Relative);
}
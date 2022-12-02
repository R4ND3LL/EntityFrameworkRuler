using System.Collections;
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
    static AppearanceManager() {
        if (Current == null) throw new("Not initialized");
    }
    public static AppearanceManager Current { get; private set; } = new();

    private AppearanceManager() {
        EnsureGenericAdded();
    }

    [ObservableProperty] private ThemeNames? selectedTheme;
    [ObservableProperty] private bool isSettingTheme;

    private Dictionary<string, IResourceAccessor> accessors;

    public Brush Accent2 => TryGetResourceValue<Brush>(nameof(Accent2));
    public Brush BlackColor => TryGetResourceValue<Brush>(nameof(BlackColor));
    public Brush WhiteColor => TryGetResourceValue<Brush>(nameof(WhiteColor));
    public Brush WindowBackground => TryGetResourceValue<Brush>(nameof(WindowBackground));
    public Brush InputBackground => TryGetResourceValue<Brush>(nameof(InputBackground));
    public Brush InputText => TryGetResourceValue<Brush>(nameof(InputText));
    public Brush GrayBrush8 => TryGetResourceValue<Brush>(nameof(GrayBrush8));
    public FontFamily InputFontFamily => TryGetResourceValue<FontFamily>(nameof(InputFontFamily));


    partial void OnSelectedThemeChanged(ThemeNames? value) {
        SetThemeSource(value);
    }

    /// <summary> main function for changing current theme </summary>
    private void SetThemeSource(ThemeNames? themeLink) {
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

            if (themeLink.HasValue) {
                var uri = themeLink switch {
                    ThemeNames.Light => ThemeUri.LightUri,
                    ThemeNames.Dark => ThemeUri.DarkUri,
                    _ => throw new ArgumentOutOfRangeException(nameof(themeLink), themeLink, null)
                };

                var themeDict = new ResourceDictionary { Source = uri };

                // add new before removing old theme to avoid DynamicResource not found warnings
                dictionaries.Add(themeDict);
            }
            // remove old theme
            if (oldThemeDict != null) dictionaries.Remove(oldThemeDict);

            OnPropertyChanged(nameof(SelectedTheme));
            accessors = null;
        } finally {
            IsSettingTheme = false;
        }
    }

    internal static bool HasUiResources() {
        var r = Application.Current?.Resources;
        if (r == null) return false;
        return true;
    }

    public T TryGetResourceValue<T>(ResourceKeys key) => TryGetResourceValue<T>(key.ToString(), out var v) ? v : default;
    public T TryGetResourceValue<T>(string key) => TryGetResourceValue<T>(key, out var v) ? v : default;

    public bool TryGetResourceValue<T>(ResourceKeys key, out T value) => TryGetResourceValue<T>(key.ToString(), out value);

    public bool TryGetResourceValue<T>(string key, out T value) {
        var a = GetAccessors();
        if (a == null) {
            value = default;
            return false;
        }

        if (!a.TryGetValue(key, out var accessor)) {
            return TryGetNonThemeResourceValue(key, out value);
        }

        if (!accessor.TryGetResourceValue(out var o)) {
            value = default;
            return false;
        }
        if (o is not T t) {
            value = default;
            return false;
        }

        value = t;
        return true;
    }
    public bool TryGetNonThemeResourceValue<T>(string key, out T value) {
        value = default;
        if (!HasUiResources()) return false;
        try {
            var o = Application.Current.TryFindResource(key); //If the requested resource is not found, a null reference is returned.
            if (o == null) return false;
            if (o is T t) value = t;
            else if (o != null) throw new($"{key} resource is not a {typeof(T).Name}. It is a {o.GetType().Name}");
            return true;
        } catch { return false; }
    }

    public bool TrySetResourceValue(ResourceKeys key, object value) => TrySetResourceValue(key.ToString(), value);

    public bool TrySetResourceValue(string key, object value) {
        var a = GetAccessors();
        if (a == null) return false;
        if (!a.TryGetValue(key, out var accessor)) return false;
        return accessor.SetResourceValue(value);
    }

    private void EnsureGenericAdded() {
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var generic = dictionaries.FirstOrDefault(o =>
            o.Source?.OriginalString == ThemeUri.GenericUri.OriginalString);
        if (generic != null) { return; }
        generic = new ResourceDictionary { Source = ThemeUri.GenericUri };
        dictionaries.Add(generic);
    }
    private ResourceDictionary GetThemeDictionary() {
        // determine the current theme by looking at the app resources and return the first dictionary having the resource key 'WindowBackground' defined.
        return (from dict in Application.Current.Resources.MergedDictionaries
                where dict.Contains("ThemeName")
                select dict).FirstOrDefault();
    }
    private Uri GetThemeSource() {
        var dict = GetThemeDictionary();
        return dict?.Source;
    }

    private Dictionary<string, IResourceAccessor> GetAccessors() {
        if (accessors != null) return accessors;
        var t = typeof(ResourceAccessor<>);
        var a = new Dictionary<string, IResourceAccessor>();
        var themeDict = GetThemeDictionary();
        if (themeDict != null)
            foreach (DictionaryEntry entry in themeDict) {
                if (entry.Key is not string key || entry.Value == null) continue;
                if (entry.Value is Color c)
                    a.Add(key, new ColorResourceAccessor(key, c));
                else if (entry.Value is Brush b)
                    a.Add(key, new BrushResourceAccessor(key, b));
                else {
                    var gt = t.MakeGenericType(entry.Value.GetType());
                    var accessor = (IResourceAccessor)Activator.CreateInstance(gt, key, entry.Value);
                    a.Add(key, accessor);
                }
            }
#if DEBUG
        var resourceKeysEnumItems = a.Keys.OrderBy(o => o).Join(", " + Environment.NewLine);
#endif
        return a;
    }

}
/// <summary>  </summary>
public class ThemeUri {
    public static Uri GenericUri = new("/EntityFrameworkRuler.Editor;component/Themes/Generic.xaml", UriKind.Relative);
    public static Uri DarkUri = new("/EntityFrameworkRuler.Editor;component/Themes/Dark.xaml", UriKind.Relative);
    public static Uri LightUri = new("/EntityFrameworkRuler.Editor;component/Themes/Light.xaml", UriKind.Relative);
}
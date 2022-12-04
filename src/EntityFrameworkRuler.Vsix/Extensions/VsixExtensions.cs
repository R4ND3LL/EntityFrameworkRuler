using System.Windows.Media; 

namespace EntityFrameworkRuler.Extensions;

public static class VsixExtensions {
    //public static System.Windows.Media.Color GetThemedWpfColor(this IVsUIShell5 vsUIShell, ThemeResourceKey themeResourceKey) {
    //    Validate.IsNotNull((object)vsUIShell, nameof(vsUIShell));
    //    Validate.IsNotNull((object)themeResourceKey, nameof(themeResourceKey));
    //    byte[] themedColorComponents = GetThemedColorComponents(vsUIShell, themeResourceKey);
    //    return System.Windows.Media.Color.FromArgb(themedColorComponents[3], themedColorComponents[0], themedColorComponents[1],
    //        themedColorComponents[2]);
    //}

    //private static byte[] GetThemedColorComponents(IVsUIShell5 vsUIShell, ThemeResourceKey themeResourceKey) {
    //    return BitConverter.GetBytes(vsUIShell.GetThemedColorRgba(themeResourceKey));
    //}

    //public static uint GetThemedColorRgba(
    //    this IVsUIShell5 vsUIShell,
    //    ThemeResourceKey themeResourceKey) {
    //    Validate.IsNotNull((object)vsUIShell, nameof(vsUIShell));
    //    Validate.IsNotNull((object)themeResourceKey, nameof(themeResourceKey));
    //    Guid category = themeResourceKey.Category;
    //    __THEMEDCOLORTYPE themedcolortype = (__THEMEDCOLORTYPE)1;
    //    if (themeResourceKey.KeyType == ThemeResourceKeyType.BackgroundColor ||
    //        themeResourceKey.KeyType == ThemeResourceKeyType.BackgroundBrush)
    //        themedcolortype = (__THEMEDCOLORTYPE)0;
    //    return vsUIShell.GetThemedColor(ref category, themeResourceKey.Name, (uint)themedcolortype);
    //}

    public static Color ToMediaColor(this System.Drawing.Color clr1) {
        return Color.FromArgb(clr1.A, clr1.R, clr1.G, clr1.B);
    }
}



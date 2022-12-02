using System.Windows.Media;
// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Editor.Extensions;

public static class ColorExtensions {
    public static Color ToColor(this Brush b) {
        if (b is LinearGradientBrush lgb && lgb.GradientStops.Count > 0) return lgb.GradientStops[0].Color;
        if (b is SolidColorBrush scb) return scb.Color;
        return Colors.Black;
    }
    public static SolidColorBrush ToBrush(this Color c, bool freeze = false) {
        var b = new SolidColorBrush(c);
        if (freeze) {
            b.Freeze();
            Debug.Assert(b.IsFrozen);
        }
        return b;
    }

    public static Color ImproveForeground(this Color fg, Color bg) {
        const byte targetDelta = 60;
        var brightnessFG = fg.GetBrightness();
        var brightnessBG = bg.GetBrightness();
        var delta = Math.Abs(brightnessFG - brightnessBG);
        if (delta > targetDelta) return fg;
        var mod = targetDelta - delta;
        if (brightnessBG > 150) {
            // light background.. darken the foreground
            fg = fg.ChangeBrightness(-mod);
        } else {
            // dark background.. lighten the foreground
            fg = fg.ChangeBrightness(mod);
        }
        return fg;
    }

    /// <summary>
    /// Determining Ideal Text Color Based on Specified Background Color
    /// </summary>
    public static bool IsDarkForegroundIdeal(Color bg) {
        const int nThreshold = 105;
        var bgDelta = Convert.ToInt32((bg.R * 0.299) + (bg.G * 0.587) + (bg.B * 0.114));
        var darkForegroundIdeal = 255 - bgDelta < nThreshold;
        return darkForegroundIdeal;
    }

    /// <summary> brightness over 150 (59%) </summary> 
    public static bool IsLight(this Color color) => color.GetBrightness() > 150;

    /// <summary> brightness under 150 (59%) </summary> 
    public static bool IsDark(this Color color) => !color.IsLight();

    /// <summary> return brightness as value from 0..1.  Value over 0.7 is considered light. </summary>        
    public static double GetBrightnessDouble(this Color c) { return c.GetBrightness() / 255D; }

    /// <summary> return brightness as value from 0..255.  Value over 150 is considered light. </summary>        
    public static int GetBrightness(this Color c) =>
        (int)Math.Sqrt((c.R * c.R * 0.241) + (c.G * c.G * 0.691) + (c.B * c.B * 0.068));

    /// <summary> alter brightness by given amount; expected in range -255..255. </summary>
    public static Color ChangeBrightness(this Color color, int modification) {
        int r = color.R;
        int g = color.G;
        int b = color.B;
        var defaultDelta = 150 * modification / 100;
        r += defaultDelta;
        if (r < 0)
            r = 0;
        if (r > 0xff)
            r = 0xff;
        g += defaultDelta;
        if (g < 0)
            g = 0;
        if (g > 0xff)
            g = 0xff;
        b += defaultDelta;
        if (b < 0)
            b = 0;
        if (b > 0xff)
            b = 0xff;
        return Color.FromArgb(0xff, (byte)r, (byte)g, (byte)b);
    }
    public static void RangeCheck(this short amt) {
        if (amt < -255 || amt > 255) throw new Exception("Argument out of range.  Must be within -255..255.");
    }
    public static void RangeCheck(this int amt) {
        if (amt < -255 || amt > 255) throw new Exception("Argument out of range.  Must be within -255..255.");
    }
    /// <summary> check double range 0..1 </summary>
    public static void RangeCheck(this double d) {
        if (d < 0 || d > 1) throw new Exception("Argument out of range.  Must be within 0..1.");
    }
    public static string ToHexString(this Color color) {
        var hexTrans = color.ToString();
        return hexTrans.Length == 7 ? hexTrans : $"#{hexTrans.Substring(3, 6)}";
    }
}
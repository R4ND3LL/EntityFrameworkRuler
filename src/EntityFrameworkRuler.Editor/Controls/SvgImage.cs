using System.Reflection;
using System.Windows;
using System.Windows.Media;
using SVGImage.SVG;

namespace EntityFrameworkRuler.Editor.Controls;

public class SvgImage : SVGImage.SVG.SVGImage {
    private static readonly FieldInfo renderField;
    static SvgImage() {
        var t = typeof(SVGImage.SVG.SVGImage);
        var fields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        renderField = fields.FirstOrDefault(o => o.FieldType == typeof(SVGRender));
    }

    public Brush OverrideBrush { get => (Brush)GetValue(OverrideBrushProperty); set => SetValue(OverrideBrushProperty, value); }
    public static readonly DependencyProperty OverrideBrushProperty = DependencyProperty.Register(nameof(OverrideBrush), typeof(Brush), typeof(SvgImage), new UIPropertyMetadata(default(Brush), (dObj, e) => ((SvgImage)dObj).OnOverrideBrushChanged((Brush)e.OldValue, (Brush)e.NewValue), (dObj, value) => ((SvgImage)dObj).CoerceOverrideBrush((Brush)value)));
    protected virtual Brush CoerceOverrideBrush(Brush value) { return value; }

    protected virtual void OnOverrideBrushChanged(Brush oldValue, Brush newValue) {
        if (newValue is null) {
            SetOverrideColor(null);
            return;
        }

        if (newValue is not SolidColorBrush solid) throw new("Brush should be solid");
        SetOverrideColor(solid.Color);
    }

    private void SetOverrideColor(Color? c) {
        OverrideColor = c;
        // fix bug in SVGImage where OverrideColor is not applied correctly to the render object
        var render = GetRender();
        if (render != null) {
            render.OverrideColor = c;
            InvalidateVisual();
            ReRenderSvg();
        }
    }

    private SVGRender GetRender() => (SVGRender)renderField?.GetValue(this);


}
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EntityFrameworkRuler.Editor.Converters;

public static class UiConverters {

    public static VisibilityInverseConverter VisibilityInverse { get; } = new();

    public static BoolToVisibilityConverter BoolToVisibility { get; } = BoolToVisibilityConverter.Instance;
    public static BoolToOpacityConverter BoolToOpacity { get; } = BoolToOpacityConverter.Instance;
    public static BoolToVisibilityConverter NotBoolToVisibility { get; } = new() { InvertBool = true };
    public static NullToVisibleConverter NullToVisible { get; } = new();
    public static NullToVisibleConverter NotNullToVisible { get; } = new() { Invert = true };
    public static FirstOrDefaultConverter FirstOrDefault { get; } = new();


    /// <summary> Detect when converter value is DisconnectedObject </summary>
    public static bool IsDependencyPropertyUnsetValue(this object o) {
        if (o == null) return false;
        if (o == DependencyProperty.UnsetValue || o == BindingOperations.DisconnectedSource) return true;
        var t = o.GetType();
        return t.Namespace == "MS.Internal" && t.Name == "NamedObject";
    }
    public static bool ConvertToBool(this object value, bool defaultValue = false) {
        // screen string 0/1 to boolean calls 
        if (value is string strVal) {
            if (bool.TryParse(strVal, out var b)) return b;
            if (int.TryParse(strVal, out var intVal)) value = intVal != 0;
        }

        return value is bool b2 ? b2 : defaultValue;
    }
}
public sealed class NullToVisibleConverter : IValueConverter {
    public bool Invert { get; set; }
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value.IsDependencyPropertyUnsetValue()) value = null;
        var isNull = value == null || (value is string s && s.IsNullOrEmpty());
        if (Invert) return isNull ? Visibility.Collapsed : Visibility.Visible;
        return isNull ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}
public sealed class BoolToOpacityConverter : IValueConverter, IMultiValueConverter {
    #region Singleton Implementation

    public static BoolToOpacityConverter Instance { get; } = new();
    public static BoolToOpacityConverter NotInstance { get; } = new() { InvertBool = true };
    public static double VisibleValue = 0.8D;
    #endregion

    public BoolToOpacityConverter() { InvertBool = false; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        var b = !value.IsDependencyPropertyUnsetValue() && value.ConvertToBool();
        if (InvertBool) b = !b;
        return b ? VisibleValue : 0D;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is bool b) {
            if (InvertBool) b = !b;
            var v = b ? VisibleValue : 0D;
            return v;
        }

        return null;
    }

    public bool InvertBool { get; set; }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
        for (var i = 0; i < values.Length; i++) {
            var o = values[i];
            if (o.IsDependencyPropertyUnsetValue()) o = false;
            var b = o.ConvertToBool();
            if (InvertBool) b = !b;
            if (!b) return 0D;
        }

        return VisibleValue;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
        return null;
    }
}

public sealed class BoolToVisibilityConverter : IValueConverter, IMultiValueConverter {
    #region Singleton Implementation

    public static BoolToVisibilityConverter Instance { get; } = new();
    public static BoolToVisibilityConverter NotInstance { get; } = new() { InvertBool = true };

    #endregion

    public BoolToVisibilityConverter() { InvertBool = false; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        var b = !value.IsDependencyPropertyUnsetValue() && value.ConvertToBool();
        if (InvertBool) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is bool b) {
            if (InvertBool) b = !b;
            var v = b ? Visibility.Visible : Visibility.Collapsed;
            return v;
        }

        return null;
    }

    public bool InvertBool { get; set; }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
        for (var i = 0; i < values.Length; i++) {
            var o = values[i];
            if (o.IsDependencyPropertyUnsetValue()) o = false;
            var b = o.ConvertToBool();
            if (InvertBool) b = !b;
            if (!b) return Visibility.Collapsed;
        }

        return Visibility.Visible;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
        return null;
    }
}

public sealed class FirstOrDefaultConverter : IValueConverter {
    #region Singleton Implementation

    public static FirstOrDefaultConverter Instance { get; } = new();

    #endregion

    public FirstOrDefaultConverter() { }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (!value.IsDependencyPropertyUnsetValue() && value is IEnumerable e) {
            return e.Cast<object>().FirstOrDefault();
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        return null;
    }
}

public sealed class VisibilityInverseConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (object)(Visibility)((Visibility)value == Visibility.Visible ? 2 : 0);

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) {
        throw new NotImplementedException("VisibilityInverseConverter.ConvertBack");
    }
}
[ValueConversion(typeof(long), typeof(bool), ParameterType = typeof(long))]
public class GreaterThanConverter : IValueConverter, IMultiValueConverter {
    #region Singleton Implementation

    public static GreaterThanConverter Instance { get; } = new GreaterThanConverter();

    #endregion


    public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        var p = ObjectToNumber(parameter, double.MaxValue);
        if (Math.Abs(p - double.MaxValue) < double.Epsilon) return false;
        var i = ObjectToNumber(value, double.MinValue);
        return i > p;
    }
    public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException("RotationsToVisibilityConverter.ConvertBack"); }
    public virtual object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
        var p = ObjectToNumber(parameter, double.MaxValue);
        if (Math.Abs(p - double.MaxValue) < double.Epsilon) return false;
        var i = values.Select(o => ObjectToNumber(o, double.MinValue)).Where(o => o > double.MinValue).ToArray();
        return i.Length > 0 && i[0] > p;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotImplementedException(); }

    public static double ObjectToNumber(object parameter, double defaultValue = 0) {
        var number = Double.TryParse(parameter?.ToString(), out var d) ? d : defaultValue;
        return number;
    }
}
/// <summary> converter than can manipulate a double value in several ways </summary>
public sealed class DoubleValueConverter : MarkupExtension, IValueConverter {
    /// <summary> lower limit </summary>
    public double MinValue { get; set; } = double.MinValue;
    /// <summary> upper limit </summary>
    public double MaxValue { get; set; } = double.MaxValue;
    /// <summary> fixed value that should be added to current value before range limit </summary>
    public double Offset { get; set; } = 0;
    /// <summary> multiply value by this before range limit </summary>
    public double Multiplier { get; set; } = 1;
    /// <summary> value used in place of Nan or Infinity </summary>
    public double DefaultValue { get; set; } = double.NaN;

    #region IValueConverter Members
    //public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {

    //    return (double)value + double.Parse((string)parameter);
    //}
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        double offset = 0;
        if (parameter != null) {
            var tempOffset = GreaterThanConverter.ObjectToNumber(parameter, 0);
            if (!double.IsInfinity(tempOffset) && !double.IsNaN(tempOffset)) offset = tempOffset;
        }
        offset += Offset;

        var v = GreaterThanConverter.ObjectToNumber(value, DefaultValue);
        if (Math.Abs(offset) > double.Epsilon) {
            // we have offset to apply
            if (!double.IsNaN(v) && !double.IsInfinity(v)) v += offset;
        }
        if (double.IsNaN(v) || double.IsInfinity(v)) {
            if (!double.IsNaN(DefaultValue) && !double.IsInfinity(DefaultValue)) return DefaultValue.RangeLimit(MinValue, MaxValue);
            if (MinValue > double.MinValue) return MinValue;
            return DefaultValue; // will be NaN
        }
        return (v * Multiplier).RangeLimit(MinValue, MaxValue);
    }



    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
        return null;
    }
    #endregion
    public override object ProvideValue(IServiceProvider serviceProvider) {
        return this;
    }
}
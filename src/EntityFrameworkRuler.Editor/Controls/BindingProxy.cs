﻿using System.Windows;

namespace EntityFrameworkRuler.Editor.Controls;

public sealed class BindingProxy : Freezable {
    #region Overrides of Freezable

    protected override Freezable CreateInstanceCore() {
        return new BindingProxy();
    }

    #endregion

    public object Data {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    // Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
}
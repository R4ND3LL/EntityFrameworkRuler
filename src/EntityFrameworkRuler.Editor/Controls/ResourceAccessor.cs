using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using EntityFrameworkRuler.Editor.Extensions;
using EntityFrameworkRuler.Generator.EdmxModel;
// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Editor.Controls;

public interface IResourceAccessor {
    string Key { get; }
    bool SetResourceValue(object v);
    object GetResourceValue();
    void BeginEdit();
    void EndEdit();
    void RaiseChangeEvent();
    bool TryGetResourceValue(out object value);
    HashSet<IResourceAccessor> Dependencies { get; set; }
}

internal static class ResourceAccessorExtensions {
    /// <summary> this resource depends on the given resource(s).. updates from dependencies will trigger a re-evaluation of this resource </summary>
    public static X DependsOn<X>(this X r, params IResourceAccessor[] deps) where X : IResourceAccessor {
        if (deps?.Length > 0) {
            if (r.Dependencies == null) r.Dependencies = new HashSet<IResourceAccessor>(deps);
            else r.Dependencies.AddRange(deps);
        }

        return r;
    }
}

public class ResourceAccessor<T> : NotifyPropertyChanged, IResourceAccessor {
    public delegate void OnResourceValueChangedHandler(ResourceAccessor<T> sender, T o, T n, bool resourceValueUpdated);

    public event OnResourceValueChangedHandler OnChanged;

    public string Key { get; }
    public T DefaultValue { get; protected internal set; }
    public bool NeedsThreadSafety { get; }
    public bool CanTryCreateResource { get; set; } = true;

    /// <summary>  </summary>
    public HashSet<IResourceAccessor> Dependencies { get; set; }

    public ResourceAccessor(ResourceKeys key, T defaultValue, OnResourceValueChangedHandler onChanged = null) : this(key.ToString(), defaultValue, onChanged) { }

    public ResourceAccessor(string key, T defaultValue) : this(key, defaultValue, null) { }

    public ResourceAccessor(string key, T defaultValue, OnResourceValueChangedHandler onChanged) {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
        Key = key;
        DefaultValue = defaultValue;
        if (onChanged != null) OnChanged += onChanged;
        NeedsThreadSafety = typeof(DependencyObject).IsAssignableFrom(typeof(T));
    }

    /// <summary> call this to flush the local cache so that the next fetch will get the fresh resource value </summary>
    public void BeginEdit() {
        if (isSuspended) throw new Exception("Edit already started");
        isSuspended = true;
        valueFetched = false;
        resourceExists = null;
    }

    public void EndEdit() {
        if (!isSuspended) throw new Exception("Call BeginEdit first");
        valueFetched = false;
        resourceExists = null;
        isSuspended = false;
    }

    public void RaiseChangeEvent() {
        OnValueChanged(default, ValueGetter(true));
    }

    private bool isSuspended;
    private bool valueFetched;
    private T resValue;

    /// <summary> Gets or sets the resource value. use this as the main accessor. </summary>
    public T Value {
        get => ValueGetter(true);
        set => SetProperty(ref resValue, value, OnValueChanged);
    }

    protected T ValueGetter(bool allowResourceWriting) {
        if (valueFetched) return resValue;
        if (TryGetResourceValue(out var v, !isSuspended && allowResourceWriting)) {
            if (isSuspended) return v; // return hot resource value without caching.
            resValue = v;
            valueFetched = true;
            return resValue;
        }

        return DefaultValue; // resource not accessible. return default instead
    }

    /// <summary>  </summary>
    protected virtual void OnValueChanged(T o, T n) {
        if (isSuspended) return;

        var updated = SetResourceValue(n, false); // try set resource value
        OnChanged?.Invoke(this, o, n, updated);
    }

    public EqualityComparer<T> Comparer { get; } = EqualityComparer<T>.Default;

    public bool SetResourceValue(T v, bool forceSet) {
        Debug.Assert(!isSuspended || forceSet);
        if (!AppearanceManager.HasUiResources() || resourceExists == false) return false; // do nothing!
        try {
            //Debug.Assert(!NeedsThreadSafety || ThreadHelper.IsMainThread);
            OnResourceWrite(ref v);
            Application.Current.Resources[Key] = v;
#if DEBUG
            // verify
            var temp = (T)Application.Current.TryFindResource(Key);
            Debug.Assert(v == null || v.Equals(temp));
#endif
            if (valueFetched && !v.Equals(resValue)) {
                Value = v;
            }
            return true;
        } catch {
            // possibly doesn't exist.  check now
            if (!resourceExists.HasValue)
                try {
                    var o = Application.Current.TryFindResource(Key); //If the requested resource is not found, a null reference is returned.
                    if (o == null) {
                        // try to insert the value
                        if (TryCreateResource(v)) return true; // we successfully added the new value                           
                        resourceExists = false; // set doesn't exist. will avoid further reads/writes.
                    }
                } catch { }

            return false;
        }
    }

    //private void UpdateResourceDictionary(T v) {
    //    Debug.Assert(ThreadHelper.IsMainThread);
    //    SetResourceValue(v);
    //}

    public T GetResourceValue() {
        return TryGetResourceValue(out var value) ? value : DefaultValue;
    }

    private bool? resourceExists;

    public bool TryGetResourceValue(out T value) { return TryGetResourceValue(out value, true); }

    public bool TryGetResourceValue(out T value, bool allowResourceWriting) {
        value = DefaultValue;
        if (!AppearanceManager.HasUiResources() || resourceExists == false) return false;
        try {
            var o = Application.Current.TryFindResource(Key); //If the requested resource is not found, a null reference is returned.
            resourceExists = o != null;
            if (!resourceExists.Value && DefaultValue != null && allowResourceWriting) {
                // try to insert the default value
                if (TryCreateResource(DefaultValue)) {
                    return true; // will return default value as this is what we just put into the resource dict
                }
            }

            if (o is T t) {
                value = t;
                OnResourceRead(ref value, allowResourceWriting);
            } else if (o != null) throw new Exception($"{Key} resource is not a {typeof(T).Name}. It is a {o.GetType().Name}");

            return true; // will return default value since it appears resource doesn't exist
        } catch { return false; }
    }

    protected bool TryCreateResource(T v) {
        try {
            if (v == null || !CanTryCreateResource) return false;
            Debug.Assert(!isSuspended);
            Application.Current.Resources.Add(Key, v);
            var got = Application.Current.Resources[Key];
            return got.Equals(v);
        } catch { return false; }
    }

    protected virtual void OnResourceRead(ref T v, bool allowResourceWriting) {
        if (CoerceResourceValue(ref v)) {
            // changed read value. we should push it back into the resource dictionary (if available)
            if (!AppearanceManager.HasUiResources() || resourceExists == false || !allowResourceWriting) return;
            SetResourceValue(v, false);
        }
    }

    protected virtual void OnResourceWrite(ref T v) {
        CoerceResourceValue(ref v);
    }

    protected virtual bool CoerceResourceValue(ref T v) {
        if (v is Freezable f) {
            if (!f.IsFrozen && f.CanFreeze) {
                f.Freeze();
                return true;
            }
        }

        return false;
    }

    bool IResourceAccessor.SetResourceValue(object v) { return SetResourceValue((T)v, false); }
    object IResourceAccessor.GetResourceValue() { return GetResourceValue(); }

    bool IResourceAccessor.TryGetResourceValue(out object value) {
        var res = TryGetResourceValue(out var v);
        value = v;
        return res;
    }

    public override string ToString() {
        var t = typeof(T);
        var tn = t.Namespace == "System" ? "sys:" + t.Name : t.Name;
        var v = ValueGetter(false);
        return $"<{tn} x:Key=\"{Key}\">{v.ToString()}</{tn}>";
    }
}

internal sealed class BrushResourceAccessor : ResourceAccessor<Brush> {
    public BrushResourceAccessor(ResourceKeys key, Brush defaultValue, OnResourceValueChangedHandler onChanged = null) : base(key, defaultValue, onChanged) { }
    public BrushResourceAccessor(string key, Brush defaultValue, OnResourceValueChangedHandler onChanged = null) : base(key, defaultValue, onChanged) { }

    protected override bool CoerceResourceValue(ref Brush v) {
        if (!v.IsFrozen && v.CanFreeze) {
            v.Freeze();
            return true;
        }

        return false;
    }
}

internal sealed class ColorResourceAccessor : ResourceAccessor<Color> {
    public ColorResourceAccessor(ResourceKeys key, Color defaultValue, OnResourceValueChangedHandler onChanged = null, double? mandatoryOpacity = null, double? mandatoryLightness = null) : this(key.ToString(), defaultValue, onChanged, mandatoryOpacity, mandatoryLightness) { }

    public ColorResourceAccessor(string key, Color defaultValue, OnResourceValueChangedHandler onChanged = null, double? mandatoryOpacity = null, double? mandatoryLightness = null) : base(key, defaultValue, onChanged) {
        if (mandatoryOpacity.HasValue) {
            MandatoryOpacityByte = Convert.ToByte(255 * mandatoryOpacity.Value);
            ColorExtensions.RangeCheck(MandatoryOpacityByte.Value);
        }

        if (mandatoryLightness.HasValue) {
            mandatoryLightness.Value.RangeCheck();
            MandatoryLightness = mandatoryLightness.Value;
        }
    }

    internal byte? MandatoryOpacityByte { get; private set; }
    internal double? MandatoryLightness { get; set; }

    protected override bool CoerceResourceValue(ref Color v) {
        var modified = false;
        if (MandatoryOpacityByte.HasValue && v.A != MandatoryOpacityByte) {
            v.A = MandatoryOpacityByte.Value;
            modified = true;
        }

        //            if (MandatoryLightness.HasValue) {
        //                var n = v.AdjustLightness(MandatoryLightness.Value);
        //                if (!n.IsColorMatch(v)) {
        //                    v = n;
        //                    modified = true;
        //                }
        //            }

        return modified;
    }

    public override string ToString() {
        var t = typeof(Color);
        var tn = t.Namespace == "System" ? "sys:" + t.Name : t.Name;
        var v = ValueGetter(false);
        var vs = v.ToHexString();
        return $"<{tn} x:Key=\"{Key}\">#{vs}</{tn}>";
    }
}
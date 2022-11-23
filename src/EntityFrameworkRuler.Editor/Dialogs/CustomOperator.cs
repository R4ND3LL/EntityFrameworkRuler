using System.Collections;
using System.ComponentModel;
using EntityFrameworkRuler.Rules;
using PropertyTools.Wpf;

namespace EntityFrameworkRuler.Editor.Dialogs;

public sealed class CustomOperator : PropertyGridOperator {
    public override IEnumerable<Tab> CreateModel(object instance, bool isEnumerable, IPropertyGridOptions options) {
        var items = base.CreateModel(instance, isEnumerable, options);
        var i = 0;
        if (items != null)
            foreach (var item in items) {
                if (item != null) {
                    item.Header = item.Header?.ToUpper();
                    if (i > 0 && item.Groups.Count == 1 && item.Groups[0].Properties.Count == 1) {
                        // fill tab and hide label
                        item.Groups[0].Header = null;
                        item.Groups[0].Properties[0].DisplayName = null;
                        item.Groups[0].Properties[0].FillTab = true;
                        item.Groups[0].Properties[0].HeaderPlacement = PropertyTools.DataAnnotations.HeaderPlacement.Collapsed;
                        //item.Groups[0].Properties[0].IsEditable = false;
                    }
                    yield return item;
                    i++;
                }
            }
    }
    protected override IEnumerable<PropertyItem> CreatePropertyItems(object instance, IPropertyGridOptions options) {
        var items = base.CreatePropertyItems(instance, options);

        foreach (var item in items) {
            if (item == null) continue;
            if (item.DisplayName == "Not Mapped" && instance is DbContextRule) continue;
            if (item.Properties?.Count > 0 && item.ActualPropertyType != typeof(string) && typeof(IList).IsAssignableFrom(item.ActualPropertyType)) {
                var collections = item.Properties.Cast<PropertyDescriptor>()
                    .Where(o => o.PropertyType == typeof(string) || typeof(IList).IsAssignableFrom(o.PropertyType)).ToArray();
                if (collections.Length > 0) {
                    item.Properties = new PropertyDescriptorCollection(collections);
                }
            }
            yield return item;
        }
    }
    protected override string GetDisplayName(PropertyDescriptor pd, Type declaringType) {
        return base.GetDisplayName(pd, declaringType);
    }
    protected override string GetCategory(PropertyDescriptor pd, Type declaringType) {
        return base.GetCategory(pd, declaringType);
    }

    public override PropertyItem CreatePropertyItem(PropertyDescriptor pd, PropertyDescriptorCollection propertyDescriptors, object instance) {
        var item = base.CreatePropertyItem(pd, propertyDescriptors, instance);
        if (item == null) return null;
        switch (pd.Name) {
            case "Multiplicity":
                item.ItemsSource = new[] { "1", "0..1", "*" };
                break;
            case nameof(NavigationRule.Name) when pd.PropertyType == typeof(HashSet<string>):
                break;
            case nameof(NavigationRule.Name) when pd.PropertyType == typeof(List<string>):
                break;
            default: {
                if (pd.PropertyType == typeof(RuleModelKind)) {
                    return null;
                }
                break;
            }
        }
        return item;
    }
    protected override string GetLocalizedString(string key, Type declaringType) {
        return key switch {
            "DbContextRule" => "DB Context",
            "SchemaRule" => "Schema",
            "TableRule" => "Table",
            "ColumnRule" => "Column",
            "NavigationRule" => "Navigation",
            _ => base.GetLocalizedString(key, declaringType)
        };
    }
}
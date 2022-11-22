using System.ComponentModel;

namespace EntityFrameworkRuler.Editor.Models;

public abstract class ImmutableBindableBase : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged {
        add { }
        remove { }
    }
}
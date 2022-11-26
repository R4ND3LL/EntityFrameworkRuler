using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Xaml.Behaviors;


namespace EntityFrameworkRuler.Editor.Behaviors {
    public sealed class DropDownButtonBehavior : Behavior<Button> {
        private long attachedCount;
        private bool isContextMenuOpen;

        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(AssociatedObject_Click), true);
        }

        private void AssociatedObject_Click(object sender, RoutedEventArgs e) {
            if (sender is not Button source || source.ContextMenu == null) return;
            // Only open the ContextMenu when it is not already open. If it is already open,
            // when the button is pressed the ContextMenu will lose focus and automatically close.
            if (isContextMenuOpen) return;
            source.ContextMenu.AddHandler(ContextMenu.ClosedEvent, new RoutedEventHandler(ContextMenu_Closed), true);
            Interlocked.Increment(ref attachedCount);
            // If there is a drop-down assigned to this button, then position and display it
            source.ContextMenu.PlacementTarget = source;
            source.ContextMenu.Placement = PlacementMode.Bottom;
            source.ContextMenu.IsOpen = true;
            isContextMenuOpen = true;
        }

        protected override void OnDetaching() {
            base.OnDetaching();
            AssociatedObject.RemoveHandler(ButtonBase.ClickEvent, new RoutedEventHandler(AssociatedObject_Click));
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e) {
            isContextMenuOpen = false;
            if (sender is not ContextMenu contextMenu) return;
            contextMenu.RemoveHandler(ContextMenu.ClosedEvent, new RoutedEventHandler(ContextMenu_Closed));
            Interlocked.Decrement(ref attachedCount);
        }
    }
}
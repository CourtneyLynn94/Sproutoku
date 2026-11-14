using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using VirtualKey = global::Windows.System.VirtualKey;

namespace Sproutoku.App;

// Windows: Esc works like Android's back gesture.
// An open sheet (Journal, Settings, How to play) closes; otherwise the page decides: the garden opens its Journal.
public partial class AppShell {
    partial void HookUpEscapeKey() {
        HandlerChanged += (_, _) => {
            if (Handler?.PlatformView is not UIElement view) return;

            var escape = new Microsoft.UI.Xaml.Input.KeyboardAccelerator { Key = VirtualKey.Escape };
            escape.Invoked += (sender, e) => {
                e.Handled = true;

                if (Navigation.ModalStack.Count > 0) {
                    _ = Navigation.PopModalAsync(); // Close the sheet on top.
                } else {
                    CurrentPage?.SendBackButtonPressed();
                }
            };

            view.KeyboardAccelerators.Add(escape);
            view.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden; // No "Esc" tooltip on the app.
        };
    }
}

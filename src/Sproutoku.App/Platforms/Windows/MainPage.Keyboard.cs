using Sproutoku.App.Garden;
using VirtualKey = global::Windows.System.VirtualKey;
using WinControl = Microsoft.UI.Xaml.Controls.Control;
using WinFocusState = Microsoft.UI.Xaml.FocusState;
using WinKeyEventArgs = Microsoft.UI.Xaml.Input.KeyRoutedEventArgs;

namespace Sproutoku.App;

// Keyboard play on Windows. The garden is a single drawn view, so its Windows control listens for keys itself.
public partial class MainPage {
    partial void HookUpKeyboard() {
        GardenView.HandlerChanged += (_, _) => {
            if (GardenView.Handler?.PlatformView is not WinControl view) return;

            view.IsTabStop = true;
            view.UseSystemFocusVisuals = false; //the garden draws its own focus ring
            view.KeyDown += OnGardenKeyDown;
            view.GotFocus += (_, _) => {
                if (view.FocusState == WinFocusState.Keyboard) {
                    ShowKeyboardFocus(true);
                }
            };
            view.LostFocus += (_, _) => ShowKeyboardFocus(false);
        };
    }

    // A click gives the garden focus too, so arrow keys work straight after clicking.
    partial void FocusGardenForKeyboard() {
        (GardenView.Handler?.PlatformView as WinControl)?.Focus(WinFocusState.Pointer);
    }

    private void OnGardenKeyDown(object sender, WinKeyEventArgs e) {
        GardenKeyboardKey? key = e.Key switch {
            VirtualKey.Up => GardenKeyboardKey.Up,
            VirtualKey.Down => GardenKeyboardKey.Down,
            VirtualKey.Left => GardenKeyboardKey.Left,
            VirtualKey.Right => GardenKeyboardKey.Right,
            VirtualKey.Space or VirtualKey.X => GardenKeyboardKey.Mark,
            VirtualKey.Enter => GardenKeyboardKey.Plant,
            _ => null,
        };

        if (key is null) return;

        e.Handled = true; //stops Windows also using the key, e.g. arrows moving focus elsewhere
        OnGardenKey(key.Value);
    }
}

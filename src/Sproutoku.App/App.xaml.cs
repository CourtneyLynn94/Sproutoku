using Microsoft.Extensions.DependencyInjection;

namespace Sproutoku.App;

public partial class App : Application {
    public App() {
        InitializeComponent();

        // 1. Mobile platforms require it on startup.
        MainPage = new AppShell();
    }

    protected override Window CreateWindow(IActivationState? activationState) {
        // 2. Let the base implementation securely handle window generation
        var window = base.CreateWindow(activationState);

        // 3. Apply sizing constraints exclusively to Windows and macOS
#if WINDOWS || MACCATALYST
        window.Width = 1280;
        window.Height = 820;

        window.MinimumWidth = 1280;
        window.MinimumHeight = 820;
        window.MaximumWidth = 1920;
        window.MaximumHeight = 1080;
#endif

        return window;
    }
}

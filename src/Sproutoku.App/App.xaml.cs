namespace Sproutoku.App;

public partial class App : Application {
    public App() {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState) {
        var window = new Window(new AppShell());

#if WINDOWS || MACCATALYST
        // Opens at a comfortable desktop size, but can shrink to phone width so the stacked layout can be tested on Windows.
        window.Width = 1280;
        window.Height = 820;
        window.MinimumWidth = 400;
        window.MinimumHeight = 600;
#endif

        return window;
    }
}

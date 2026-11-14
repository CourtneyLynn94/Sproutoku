using Sproutoku.App.Pages;

namespace Sproutoku.App;

public partial class AppShell : Shell {
    public AppShell() {
        InitializeComponent();

        // Pages opened by name. Shell builds them through dependency injection, so their services arrive as usual.
        Routing.RegisterRoute(Routes.Garden, typeof(GardenPage));
        Routing.RegisterRoute(Routes.Journal, typeof(JournalPage));
        Routing.RegisterRoute(Routes.Settings, typeof(SettingsPage));
        Routing.RegisterRoute(Routes.HowToPlay, typeof(HowToPlayPage));
    }
}

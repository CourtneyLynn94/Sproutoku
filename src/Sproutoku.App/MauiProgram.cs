using Microsoft.Extensions.Logging;
using Sproutoku.App.Pages;
using Sproutoku.App.Services;
using Sproutoku.Core.Game.Levels;

namespace Sproutoku.App;

public static class MauiProgram {
    public static MauiApp CreateMauiApp() {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Shared services: one of each for the whole app.
        builder.Services.AddSingleton<GameSettings>();
        builder.Services.AddSingleton<GameStorage>();
        builder.Services.AddSingleton<Haptics>();
        builder.Services.AddSingleton<LevelPreparer>();
        builder.Services.AddSingleton<Dialogs>();

        // Pages: a fresh one each time one opens. Shell builds MainPage through this too.
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<SettingsPage>();

        return builder.Build();
    }
}

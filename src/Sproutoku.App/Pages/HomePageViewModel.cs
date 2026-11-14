using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sproutoku.App.Services;

namespace Sproutoku.App.Pages;

/// <summary>
/// Home: the game's name, one big button into the garden, and the smaller ones.
/// The main button says "Start gardening" on a first launch and "Keep gardening" after that, with the garden it opens.
/// </summary>
public sealed partial class HomePageViewModel : ObservableObject {
    private readonly GameStorage _gameStorage;
    private readonly GardenPageViewModel _garden;

    public HomePageViewModel(GameStorage gameStorage, GardenPageViewModel garden) {
        _gameStorage = gameStorage;
        _garden = garden;
    }

    #region Main button
    /// <summary>
    /// Whether there's a saved garden to go back to.
    /// </summary>
    public bool HasProgress { get; private set; }

    /// <summary>
    /// The saved level, or 1 on a first launch.
    /// </summary>
    public int Level { get; private set; } = 1;

    public string MainButtonText => HasProgress ? "Keep gardening" : "Start gardening";

    /// <summary>
    /// Under the main button: the garden it opens. Shown only when there's progress.
    /// </summary>
    public string MainButtonDetail => $"Garden {Level}";

    // Opens the garden. If it's still growing, the garden page shows "Growing your garden…" until it's ready.
    [RelayCommand]
    private Task KeepGardeningAsync() => Shell.Current.GoToAsync(Routes.Garden);
    #endregion

    #region Other buttons
    [RelayCommand]
    private Task OpenHowToPlayAsync() => Shell.Current.GoToAsync(Routes.HowToPlay);

    [RelayCommand]
    private Task OpenSettingsAsync() => Shell.Current.GoToAsync(Routes.Settings);
    #endregion

    /// <summary>
    /// Home is showing (at launch, and coming back from the garden or a sheet):
    /// read where the player is, and grow their garden so it's ready when they tap.
    /// </summary>
    public async Task AppearingAsync() {
        var save = _gameStorage.Load();
        HasProgress = save is not null;
        Level = save?.Level ?? 1;
        OnPropertyChanged(string.Empty);

        await _garden.LoadAsync();

        // A saved garden that was already finished opens on the next level, so show the one the button really opens.
        Level = _garden.Level;
        OnPropertyChanged(nameof(Level));
        OnPropertyChanged(nameof(MainButtonDetail));
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sproutoku.App.Services;
using Sproutoku.Core.Game.Boards;
using Sproutoku.Core.Game.Levels;
using Sproutoku.Core.Game.PowerUps;
using Sproutoku.Core.Game.Session;
using System.Diagnostics;
using Cell = Sproutoku.Core.Gardens.Puzzles.Cell;

namespace Sproutoku.App.Pages;

/// <summary>
/// The garden screen's state and actions: everything about the game the page shows, and none of how it looks.
/// It owns the <see cref="GameSession"/>, saves after every change, and moves between gardens.
/// The page binds to its properties and commands, and passes in the cells the player taps, drags and keys.
/// Each region is one part of the screen, so it can move with that part when it becomes its own control.
/// </summary>
public sealed partial class GardenPageViewModel : ObservableObject {
    private readonly GameStorage _gameStorage;
    private readonly Haptics _haptics;
    private readonly LevelPreparer _levels;
    private readonly Dialogs _dialogs;

    public GardenPageViewModel(GameStorage gameStorage, Haptics haptics, LevelPreparer levels, Dialogs dialogs) {
        _gameStorage = gameStorage;
        _haptics = haptics;
        _levels = levels;
        _dialogs = dialogs;
    }

    /// <summary>
    /// The board or the open hint changed: redraw the garden and fade any spent sun.
    /// </summary>
    public event EventHandler? BoardChanged;

    /// <summary>
    /// A new garden is on show (a new level, Try again, a reset, or back from a test garden): rebuild the sun meter and forget hover and focus.
    /// </summary>
    public event EventHandler? GardenStarted;

    #region Loading and saving
    // The game itself: level, board and every rule. Null only for the moment the saved level is growing at startup.
    private GameSession? _session;
    private GameSession Session => _session ?? throw new InvalidOperationException("The game hasn't loaded yet.");

    /// <summary>
    /// Whether the game has loaded. Until then, the page ignores input.
    /// </summary>
    public bool IsLoaded => _session is not null;

    // The one load of the saved game, started by Home as soon as the app opens.
    private Task? _loading;

    /// <summary>
    /// Picks up where the player left off, or starts at Garden 1. The saved level may need growing, so it loads off the main thread.
    /// Safe to call from every screen: the game loads once, and later callers wait for that same load.
    /// </summary>
    public Task LoadAsync() => _loading ??= LoadSavedGameAsync();

    private async Task LoadSavedGameAsync() {
        Info = "Growing your garden…";
        var save = _gameStorage.Load();
        _session = await Task.Run(() => GameSession.Resume(save));
        ShowNewGarden(save is null ? GardenInfo() : "Welcome back");
    }

    private void Save() => _gameStorage.Save(Session.ToSave());

    // Something the page shows changed: every binding reads again, the tools recheck whether they work, and the garden redraws.
    private void Refresh() {
        OnPropertyChanged(string.Empty);
        UseWormCommand.NotifyCanExecuteChanged();
        BoardChanged?.Invoke(this, EventArgs.Empty);
    }
    #endregion

    #region Levels
    // A garden that's new on screen: show it and save. Grows the next level meanwhile, and lets go of levels already passed.
    private void ShowNewGarden(string info) {
        Info = info;
        Save();

        if (!Session.IsExtraGarden) {
            _levels.Prepare(Session.Level + 1);
            _levels.ForgetBelow(Session.Level);
        }

        GardenStarted?.Invoke(this, EventArgs.Empty);
        Refresh();
    }

    // Jumps to a level, fresh.
    // A level that isn't ready grows in the background with a gentle note meanwhile.
    // With eraseProgress, the saved progress is erased once the level is ready (see ResetAsync).
    private async Task JumpToLevelAsync(int level, bool eraseProgress = false) {
        if (!_levels.IsReady(level)) {
            Info = $"Growing Garden {level}…";
        }

#if DEBUG
        var timer = Stopwatch.StartNew();
#endif
        var levelGarden = await _levels.GetAsync(level);

        // Erased only now, right before the new garden is shown and saved, with nothing in between.
        // The old garden can still be played while the level grows, and any save it makes is erased here too.
        if (eraseProgress) {
            _gameStorage.ResetProgress();
        }

        Session.GoToLevel(levelGarden);
#if DEBUG
        ShowNewGarden($"{GardenInfo()} · {timer.ElapsedMilliseconds} ms");
#else
        ShowNewGarden(GardenInfo());
#endif
    }

    // Moves on to the next level. It's usually grown already; if not, a gentle note shows while it finishes.
    private async Task AdvanceAsync() {
        int next = Session.Level + 1;
        if (!_levels.IsReady(next)) {
            Info = "Growing your next garden…";
        }

        var levelGarden = await _levels.GetAsync(next);

        // Refused if a reset or skip changed the level while it grew; that garden is simply not used.
        if (Session.TryAdvance(levelGarden)) {
            ShowNewGarden(GardenInfo());
        }
    }

    // A complete progress reset. Asks first, because it can't be undone.
    private async Task ResetAsync() {
        bool erase = await _dialogs.ConfirmAsync(
            "Reset all progress?",
            "This erases every garden you've grown and everything you've earned, and starts you again at Garden 1. Your settings stay as they are. It can't be undone.",
            "Erase my progress",
            "Keep my progress");

        if (!erase) return;

        await JumpToLevelAsync(1, eraseProgress: true);
    }
    #endregion

    #region End of garden
    // Rough end-of-garden screens until the real sheets are designed.
    // The session decides whether moving on or starting over is allowed; this only asks.
    // async void: nothing awaits this UI flow (see AGENTS.md).
    private async void ShowEndOfGarden() {
        var board = Session.Board;

        // Let the last plant grow or the last sun fade before the screen appears.
        await Task.Delay(500);

        // The Journal still works during the wait. If a reset or skip changed the garden, this screen is stale.
        if (!ReferenceEquals(Session.Board, board)) return;

        if (board.IsSolved) {
            _haptics.GardenComplete();

            bool extra = Session.IsExtraGarden;
            string stats =
                $"{(extra ? "Test garden" : $"Garden {Session.Level}")}\n" +
                $"Suns left: {board.SunsLeft} of {board.StartingSuns}\n" +
                $"Mistakes: {board.Mistakes}\n" +
                $"Beds: {board.Size}";

            await _dialogs.AlertAsync("Garden complete", stats, extra ? $"Back to Garden {Session.Level}" : "Next garden");

            if (extra) {
                if (Session.TryAdvance()) {
                    ShowNewGarden("Back to your garden");

                    // A level finished before the test garden started gets its end screen again.
                    if (Session.Board.IsFinished) {
                        ShowEndOfGarden();
                    }
                }
            } else {
                await AdvanceAsync();
            }

            return;
        }

        _haptics.OutOfSun();
        await _dialogs.AlertAsync("Your garden needs more sun", "All your suns are spent. Give this garden a fresh start.", "Try again");

        if (Session.TryRestart()) {
            ShowNewGarden("A fresh start");
        }
    }
    #endregion

    #region Garden sign and info
    /// <summary>
    /// The name on the garden sign.
    /// </summary>
    public string GardenName => _session is null ? "" : Session.IsExtraGarden ? "Test garden" : $"Garden {Session.Level}";

    private string _info = "";

    /// <summary>
    /// The info line: the garden's size and tags, or a short note like "Growing your garden…".
    /// </summary>
    public string Info {
        get => _info;
        private set => SetProperty(ref _info, value);
    }

    // The garden's tags until the real ones exist (#91): Hard, Super hard, One sun. None for a test garden.
    private List<string> Tags() {
        if (Session.IsExtraGarden) return [];

        var recipe = Session.LevelGarden.Recipe;
        List<string> tags = [];
        if (recipe.Tier == GardenTier.Hard) tags.Add("Hard");
        if (recipe.Tier == GardenTier.SuperHard) tags.Add("Super hard");
        if (recipe.IsOneSun) tags.Add("One sun");
        return tags;
    }

    // The info line for a level: size and tags. Debug adds the grade, style and seed.
    private string GardenInfo() {
        var recipe = Session.LevelGarden.Recipe;
        List<string> parts = [$"{recipe.Size} × {recipe.Size}", .. Tags()];
#if DEBUG
        parts.Add(Session.LevelGarden.Garden.Grade.ToString());
        parts.Add(Session.LevelGarden.Garden.Style.ToString());
        parts.Add($"seed {Session.Seed}");
#endif
        return string.Join(" · ", parts);
    }
    #endregion

    #region Sun meter
    /// <summary>
    /// How many suns the garden started with: how many slots the sun meter has.
    /// </summary>
    public int StartingSuns => _session?.Board.StartingSuns ?? 0;

    /// <summary>
    /// How many suns are still bright.
    /// </summary>
    public int SunsLeft => _session?.Board.SunsLeft ?? 0;

    /// <summary>
    /// What a screen reader says for the sun meter.
    /// </summary>
    public string SunsDescription => $"{SunsLeft} of {StartingSuns} suns left";
    #endregion

    #region Garden view
    /// <summary>
    /// The board to draw, or null while loading.
    /// </summary>
    public Board? Board => _session?.Board;

    /// <summary>
    /// Developer view: ring every solution cell (debug Journal option). Drawing only.
    /// </summary>
    public bool ShowSolution { get; private set; }

    /// <summary>
    /// A tap on a cell: a ✕, or a plant if it's the second tap of a double tap.
    /// </summary>
    public void Tap(Cell cell) => ShowResult(Session.Tap(cell, Environment.TickCount64));

    /// <summary>
    /// Keyboard Space or X: toggles a ✕ straight away.
    /// </summary>
    public void ToggleMark(Cell cell) => ShowResult(Session.ToggleMark(cell));

    /// <summary>
    /// Keyboard Enter: tries to plant straight away.
    /// </summary>
    public void Plant(Cell cell) => ShowResult(Session.Plant(cell));

    /// <summary>
    /// A press has become a drag.
    /// </summary>
    public void BeginDrag() => Session.BeginDrag();

    /// <summary>
    /// One cell of a drag, with a tick when it changes. Returns true if it changed.
    /// </summary>
    public bool DragOver(Cell cell) {
        if (!Session.DragOver(cell)) return false;

        _haptics.Mark();
        Refresh();
        return true;
    }

    /// <summary>
    /// The drag is over; it's saved once.
    /// </summary>
    public void EndDrag() {
        Session.EndDrag();
        Save();
    }

    /// <summary>
    /// While a hint is focused, a press or key on the garden only unfocuses it. Returns true if it did, so the page drops that press or key.
    /// </summary>
    public bool UnfocusHintIfFocused() {
        if (_session?.IsHintFocused != true) return false;

        Session.UnfocusHint();
        Refresh();
        return true;
    }

    // What a tap, key or applied hint did: a feel, a save, and the end screen if the garden is over.
    private void ShowResult(TapResult result) {
        if (result.Outcome == TapOutcome.Ignored) return;

        PlayHaptic(result.Outcome);
        Save();
        Refresh();

        if (result.EndedGarden) {
            ShowEndOfGarden();
        }
    }

    // A click for ✕, two pops for a plant, a knock for a mistake.
    private void PlayHaptic(TapOutcome outcome) {
        switch (outcome) {
            case TapOutcome.MarkToggled:
                _haptics.Mark();
                break;
            case TapOutcome.Grew:
                _haptics.Grew();
                break;
            case TapOutcome.Wilted:
                _haptics.Wilted();
                break;
        }
    }
    #endregion

    #region Planting list
    /// <summary>
    /// Whether the planting list shows: always, except while the worm's note takes its place.
    /// </summary>
    public bool IsPlantingListShown => !IsHintOpen;

    /// <summary>
    /// "3 of 7 grown".
    /// </summary>
    public string PlantingCount => _session is null ? "" : $"{Session.Board.CountOf(CellState.Planted)} of {Session.Board.Size} grown";
    #endregion

    #region Worm's note
    /// <summary>
    /// The worm's hint, focused or not, or null.
    /// </summary>
    public WormHint? OpenHint => _session?.OpenHint;

    /// <summary>
    /// Whether the note is up.
    /// </summary>
    public bool IsHintOpen => OpenHint is not null;

    /// <summary>
    /// Whether the board is focused on the hint: dimmed, with preview marks. The note offers Apply and Hide.
    /// </summary>
    public bool IsHintFocused => _session?.IsHintFocused == true;

    /// <summary>
    /// Whether the note is up but the board is back to normal. The note offers Show and Close.
    /// </summary>
    public bool IsHintHidden => IsHintOpen && !IsHintFocused;

    /// <summary>
    /// What the worm says.
    /// </summary>
    public string HintMessage => OpenHint?.Message ?? "";

    // Makes what's left of the hint.
    [RelayCommand]
    private void ApplyHint() => ShowResult(Session.ApplyHint());

    [RelayCommand]
    private void HideHint() {
        Session.UnfocusHint();
        Refresh();
    }

    [RelayCommand]
    private void ShowHint() {
        Session.FocusHint();
        Refresh();
    }

    // Closing asks first: once it's closed, seeing a hint again takes another Worm.
    [RelayCommand]
    private async Task CloseHintAsync() {
        bool close = await _dialogs.ConfirmAsync(
            "Close the worm's note?",
            "To see the hint again, you'll need another Worm.",
            "Close",
            "Keep it");
        if (!close) return;

        Session.CloseHint();
        Refresh();
    }
    #endregion

    #region Tool shelf
    // Each tool shows its name and count (∞ with the dev option); a tool works when there's one left and the garden isn't over.
    // Seeds and Water do nothing yet (#45, #46), and the Rake arrives with #49.
    public string SeedsText => ToolText("Seeds", PowerUp.Seeds);
    public string WormText => ToolText("Worm", PowerUp.Worm);
    public string WaterText => ToolText("Water", PowerUp.Water);

    public string SeedsDescription => ToolDescription("Seeds", PowerUp.Seeds);
    public string WormDescription => ToolDescription("Worm", PowerUp.Worm);
    public string WaterDescription => ToolDescription("Water", PowerUp.Water);

    public bool CanUseSeeds => _session?.CanUse(PowerUp.Seeds) == true;
    public bool CanUseWater => _session?.CanUse(PowerUp.Water) == true;
    private bool CanUseWorm => _session?.CanUse(PowerUp.Worm) == true;

    // The next step, focused on the board. With a hint already open, this focuses it again for free.
    [RelayCommand(CanExecute = nameof(CanUseWorm))]
    private void UseWorm() {
        var hint = Session.UseWorm();
        if (hint is null) {
            Info = "Your worm has nothing new to show.";
            return;
        }

        Save(); // The spent Worm is saved, so reopening the app doesn't refill it.
        Refresh();
        SemanticScreenReader.Announce(hint.Message);
    }

    private string ToolText(string name, PowerUp powerUp) =>
        _session is null ? name : $"{name}\n{(Session.UnlimitedTools ? "∞" : Session.PowerUps[powerUp].ToString())}";

    private string ToolDescription(string name, PowerUp powerUp) =>
        _session is null ? name : $"{name}, {Session.PowerUps[powerUp]} left";
    #endregion

    #region Journal
    /// <summary>
    /// The Journal's garden facts, e.g. "Garden 42 · 7 beds · 2 of 3 suns left · Hard".
    /// </summary>
    public string JournalFacts {
        get {
            if (_session is null) return "";

            var board = Session.Board;
            List<string> parts = [
                GardenName,
                $"{board.Size} beds",
                $"{board.SunsLeft} of {board.StartingSuns} suns left",
                .. Tags(),
            ];
            return string.Join(" · ", parts);
        }
    }

    // Opens the Journal sheet: from the Journal button, or the back gesture on the garden. Ignored while the game is loading.
    [RelayCommand]
    private Task OpenJournalAsync() => _session is null ? Task.CompletedTask : Shell.Current.GoToAsync(Routes.Journal);

    [RelayCommand]
    private Task KeepGardeningAsync() => CloseJournalAsync();

    // How to play and Settings open over the Journal, so closing them comes back to it.
    [RelayCommand]
    private Task OpenHowToPlayAsync() => Shell.Current.GoToAsync(Routes.HowToPlay);

    [RelayCommand]
    private Task OpenSettingsAsync() => Shell.Current.GoToAsync(Routes.Settings);

    // Closes the Journal and leaves the garden for Home. The garden's progress is already saved.
    [RelayCommand]
    private Task GoHomeAsync() => Shell.Current.GoToAsync("../..");

    private static Task CloseJournalAsync() => Shell.Current.GoToAsync("..");

#if DEBUG
    [RelayCommand]
    private async Task GoToLevelDevAsync() {
        await CloseJournalAsync();
        await GoToLevelAsync();
    }

    [RelayCommand]
    private async Task SkipLevelDevAsync() {
        await CloseJournalAsync();
        await JumpToLevelAsync(Session.Level + 1);
    }

    [RelayCommand]
    private async Task ToggleUnlimitedToolsDevAsync() {
        Session.UnlimitedTools = !Session.UnlimitedTools;
        Refresh();
        await CloseJournalAsync();
    }

    [RelayCommand]
    private async Task ToggleSolutionDevAsync() {
        ShowSolution = !ShowSolution;
        Refresh();
        await CloseJournalAsync();
    }

    // Dev: jumps straight to any level by number. The garden is grown off the main thread, since big or Super hard ones can take a moment.
    private async Task GoToLevelAsync() {
        string? answer = await _dialogs.PromptAsync(
            "Go to level",
            $"You're on Garden {Session.Level}. Which level?",
            accept: "Go",
            cancel: "Cancel",
            placeholder: "Level number",
            maxLength: 7,
            keyboard: Keyboard.Numeric);

        if (answer is null) return; // Cancel

        if (!int.TryParse(answer, out int level) || level < 1) {
            Info = $"\"{answer}\" isn't a level number.";
            return;
        }

        await JumpToLevelAsync(level);
    }
#endif
    #endregion
}

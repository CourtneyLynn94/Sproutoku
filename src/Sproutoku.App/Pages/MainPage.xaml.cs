using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Sproutoku.Core.Game.Session;
using Sproutoku.Core.Game.Levels;
using Sproutoku.Core.Game.Boards;
using Sproutoku.Core.Game.PowerUps;
using Sproutoku.App.Controls.Garden;
using Sproutoku.App.Services;
using Cell = Sproutoku.Core.Gardens.Puzzles.Cell;

namespace Sproutoku.App.Pages;

public partial class MainPage : ContentPage {
    // At this width and up, the desktop layout puts the list, board and shelf side by side.
    private const double WideLayoutMinWidth = 960;
    private bool? _isWide;

#if WINDOWS || MACCATALYST
    // Keyboard support. Filled in by Platforms/Windows/MainPage.Keyboard.cs; on other platforms these do nothing.
    partial void HookUpKeyboard();
    partial void FocusGardenForKeyboard();
#endif

    private readonly GardenDrawable _garden = new() { ShouldDrawRegionBorders = false };

    // Shared services, from dependency injection (MauiProgram).
    private readonly GameSettings _settings;
    private readonly GameStorage _gameStorage;
    private readonly Haptics _haptics;
    private readonly LevelPreparer _levels;
    private readonly IServiceProvider _services;

    // The game itself: level, board and every rule. The page only shows it and passes input in.
    // Null only for the moment the saved level is growing at startup.
    private GameSession? _session;
    private GameSession Session => _session ?? throw new InvalidOperationException("The game hasn't loaded yet.");

    // The press in progress:
    // the cell it started on (null if it started off the board), the last point it was seen at, and whether it has turned into a drag.
    private Cell? _pressedCell;
    private PointF _lastPoint;
    private bool _isDragging;

    public MainPage(GameSettings settings, GameStorage gameStorage, Haptics haptics, LevelPreparer levels, IServiceProvider services) {
        _settings = settings;
        _gameStorage = gameStorage;
        _haptics = haptics;
        _levels = levels;
        _services = services;

        InitializeComponent();

        GardenView.Drawable = _garden;

        ApplyLayout(wide: false);
        SizeChanged += (_, _) => ApplyLayout(Width >= WideLayoutMinWidth);

        GardenView.StartInteraction += OnPress;
        GardenView.DragInteraction += OnDrag;
        GardenView.EndInteraction += OnRelease;
        GardenView.CancelInteraction += (_, _) => FinishPress();

#if WINDOWS || MACCATALYST
        // Desktop hover. Touch screens don't send hover events, so this does nothing on a phone.
        GardenView.MoveHoverInteraction += (_, e) => SetHover(_garden.CellAt(e.Touches[0]));
        GardenView.EndHoverInteraction += (_, _) => SetHover(null);

        // Desktop keyboard.
        HookUpKeyboard();
#endif

        // The saved level may need growing, so load off the main thread with a gentle note meanwhile.
        InfoLabel.Text = "Growing your garden…";
        _ = LoadGameAsync();
    }

    // Picks up where the player left off, or starts at Garden 1.
    private async Task LoadGameAsync() {
        var save = _gameStorage.Load();
        _session = await Task.Run(() => GameSession.Resume(save));
        ShowGarden(save is null ? GardenInfo() : "Welcome back");
    }

    // Draws the session's garden from scratch (new garden, Try again, reset) and saves.
    private void ShowGarden(string info) {
        CloseWormHint();

        _garden.Board = Session.Board;
        _garden.HoverCell = null;
        _garden.FocusCell = null;
        _garden.ShowFocus = false;

        BuildSunMeter(Session.Board.StartingSuns);
        UpdateSunMeter(animate: false);

        GardenSignLabel.Text = Session.IsExtraGarden ? "Test garden" : $"Garden {Session.Level}";
        InfoLabel.Text = info;
        UpdatePlantingList();
        UpdateToolShelf();
        GardenView.Invalidate();

        SaveProgress();

        // Grow the next level while this one is played, and let go of levels already passed.
        if (!Session.IsExtraGarden) {
            _levels.Prepare(Session.Level + 1);
            _levels.ForgetBelow(Session.Level);
        }
    }

    // Jumps to a level, fresh. A level that isn't ready grows in the background with a gentle note meanwhile.
    private async Task JumpToLevelAsync(int level) {
        if (!_levels.IsReady(level)) {
            InfoLabel.Text = $"Growing Garden {level}…";
        }

#if DEBUG
        var timer = Stopwatch.StartNew();
#endif
        var levelGarden = await _levels.GetAsync(level);
        Session.GoToLevel(levelGarden);
#if DEBUG
        ShowGarden($"{GardenInfo()} · {timer.ElapsedMilliseconds} ms");
#else
        ShowGarden(GardenInfo());
#endif
    }

    // The info line for a level: size, plus Hard, Super hard and One sun until the real tags exist. Debug adds the grade and seed.
    private string GardenInfo() {
        var recipe = Session.LevelGarden.Recipe;
        List<string> parts = [$"{recipe.Size} × {recipe.Size}"];

        if (recipe.Tier == GardenTier.Hard) parts.Add("Hard");
        if (recipe.Tier == GardenTier.SuperHard) parts.Add("Super hard");
        if (recipe.IsOneSun) parts.Add("One sun");
#if DEBUG
        parts.Add(Session.LevelGarden.Garden.Grade.ToString());
        parts.Add(Session.LevelGarden.Garden.Style.ToString());
        parts.Add($"seed {Session.Seed}");
#endif
        return string.Join(" · ", parts);
    }

    private void SaveProgress() => _gameStorage.Save(Session.ToSave());

    private void UpdatePlantingList() {
        PlantingCountLabel.Text = $"{Session.Board.CountOf(CellState.Planted)} of {Session.Board.Size} grown";
    }

    // A complete progress reset. Asks first, because it can't be undone.
    private async Task ConfirmResetAsync() {
        bool erase = await DisplayAlertAsync(
            "Reset all progress?",
            "This erases every garden you've grown and everything you've earned, and starts you again at Garden 1. Your settings stay as they are. It can't be undone.",
            "Erase my progress",
            "Keep my progress");

        if (!erase) return;

        _gameStorage.ResetProgress();
        await JumpToLevelAsync(1);
    }

    // Rough end-of-garden screens until the real sheets are designed.
    // The session decides whether moving on or starting over is allowed; the page only asks.
    private async void ShowEndOfGarden() {
        var board = Session.Board;

        // Reset hovered cell state
        _garden.HoverCell = null;
        GardenView.Invalidate();

        // Let the last plant grow or the last sun fade before the screen appears.
        await Task.Delay(500);

        // The Journal still works during the wait. If a reset or skip changed the garden, this screen is stale.
        if (!ReferenceEquals(Session.Board, board)) return;

        _haptics.GardenEnded();

        if (board.IsSolved) {
            bool extra = Session.IsExtraGarden;
            string stats =
                $"{(extra ? "Test garden" : $"Garden {Session.Level}")}\n" +
                $"Suns left: {board.SunsLeft} of {board.StartingSuns}\n" +
                $"Mistakes: {board.Mistakes}\n" +
                $"Beds: {board.Size}";

            await DisplayAlertAsync("Garden complete", stats, extra ? $"Back to Garden {Session.Level}" : "Next garden");

            if (extra) {
                if (Session.TryAdvance()) {
                    ShowGarden("Back to your garden");

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

        const string Title = "Your garden needs more sun";
        const string Message = "All your suns are spent. Give this garden a fresh start.";

        await DisplayAlertAsync(Title, Message, "Try again");
        if (Session.TryRestart()) {
            ShowGarden("A fresh start");
        }
    }

    // Moves on to the next level. It's usually grown already; if not, a gentle note shows while it finishes.
    private async Task AdvanceAsync() {
        int next = Session.Level + 1;
        if (!_levels.IsReady(next)) {
            InfoLabel.Text = "Growing your next garden…";
        }

        var levelGarden = await _levels.GetAsync(next);

        // Refused if a reset or skip changed the level while it grew; that garden is simply not used.
        if (Session.TryAdvance(levelGarden)) {
            ShowGarden(GardenInfo());
        }
    }

    #region Layout
    // Phone: stacked vertical with board, planting list, tool shelf.
    // Desktop: stacked horizontal with planting list, board, tool shelf.
    private void ApplyLayout(bool wide) {
        if (_isWide == wide) return;
        _isWide = wide;

        if (wide) {
            GameArea.RowDefinitions = Rows(GridLength.Star);
            GameArea.ColumnDefinitions = Columns(new GridLength(240), GridLength.Star, new GridLength(200));
            Place(PlantingList, row: 0, column: 0);
            Place(WormNote, row: 0, column: 0);
            Place(BoardArea, row: 0, column: 1);
            Place(ToolShelf, row: 0, column: 2);
        } else {
            GameArea.RowDefinitions = Rows(GridLength.Star, GridLength.Auto, GridLength.Auto);
            GameArea.ColumnDefinitions = Columns(GridLength.Star);
            Place(BoardArea, row: 0, column: 0);
            Place(PlantingList, row: 1, column: 0);
            Place(WormNote, row: 1, column: 0);
            Place(ToolShelf, row: 2, column: 0);
        }

        ArrangeToolShelf(vertical: wide);
    }

    // Phone: tools share one row, so they shrink to fit a 360px screen.
    // Desktop: tools stack in one column.
    private void ArrangeToolShelf(bool vertical) {
        View[] tools = [SeedsButton, WormButton, WaterButton, ShelfDivider, RakeButton];

        if (vertical) {
            ToolShelf.ColumnDefinitions = Columns(GridLength.Star);
            ToolShelf.RowDefinitions = Rows(GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto);
        } else {
            ToolShelf.ColumnDefinitions = Columns(GridLength.Star, GridLength.Star, GridLength.Star, GridLength.Auto, GridLength.Star);
            ToolShelf.RowDefinitions = Rows(GridLength.Auto);
        }

        for (int i = 0; i < tools.Length; i++) {
            Place(tools[i], row: vertical ? i : 0, column: vertical ? 0 : i);
        }

        // The divider is a thin vertical line in a row, a thin horizontal line in a column. -1 means "not set".
        ShelfDivider.WidthRequest = vertical ? -1 : 2;
        ShelfDivider.HeightRequest = vertical ? 2 : -1;
    }

    private static void Place(BindableObject view, int row, int column) {
        Grid.SetRow(view, row);
        Grid.SetColumn(view, column);
    }

    private static RowDefinitionCollection Rows(params GridLength[] heights) =>
        new(heights.Select(height => new RowDefinition(height)).ToArray());

    private static ColumnDefinitionCollection Columns(params GridLength[] widths) =>
        new(widths.Select(width => new ColumnDefinition(width)).ToArray());
    #endregion

    #region Event Interactions
    // Rough Garden journal.
    private async void OnJournalClicked(object? sender, EventArgs e) {
        if (_session is null) return; // Still loading.

        List<string> options = ["How to play", "Settings"];
#if DEBUG
        options.Add("Go to level (dev)");
        options.Add("Skip to next garden (dev)");
        options.Add(Session.UnlimitedTools ? "Normal tools (dev)" : "Unlimited tools (dev)");
        options.Add(_garden.ShowSolution ? "Hide solution (dev)" : "Show solution (dev)");
#endif

        string choice = await DisplayActionSheetAsync("Garden journal", "Keep gardening", "Reset all progress", [.. options]);

        switch (choice) {
            case "Reset all progress":
                await ConfirmResetAsync();
                break;
            case "How to play":
                await Navigation.PushModalAsync(new HowToPlayPage());
                break;
            case "Settings":
                await Navigation.PushModalAsync(_services.GetRequiredService<SettingsPage>());
                break;
#if DEBUG
            case "Go to level (dev)":
                await GoToLevelAsync();
                break;
            case "Skip to next garden (dev)":
                await JumpToLevelAsync(Session.Level + 1);
                break;
            case "Unlimited tools (dev)":
            case "Normal tools (dev)":
                Session.UnlimitedTools = !Session.UnlimitedTools;
                UpdateToolShelf();
                break;
            case "Show solution (dev)":
            case "Hide solution (dev)":
                _garden.ShowSolution = !_garden.ShowSolution;
                GardenView.Invalidate();
                break;
#endif
        }
    }

#if DEBUG
    // Dev: jumps straight to any level by number. The garden is grown off the main thread, since big or Super hard ones can take a moment.
    private async Task GoToLevelAsync() {
        string? answer = await DisplayPromptAsync(
            "Go to level",
            $"You're on Garden {Session.Level}. Which level?",
            accept: "Go",
            cancel: "Cancel",
            placeholder: "Level number",
            maxLength: 7,
            keyboard: Keyboard.Numeric);

        if (answer is null) return; // Cancel

        if (!int.TryParse(answer, out int level) || level < 1) {
            InfoLabel.Text = $"\"{answer}\" isn't a level number.";
            return;
        }

        await JumpToLevelAsync(level);
    }
#endif

    // Finger or mouse down: remember where the press started. Nothing changes until it lifts or moves.
    private void OnPress(object? sender, TouchEventArgs e) {
        _pressedCell = _garden.CellAt(e.Touches[0]);
        _lastPoint = e.Touches[0];
        _isDragging = false;

        if (_pressedCell is { } pressed) {
            _garden.FocusCell = pressed;
        }

        _garden.ShowFocus = false;

#if WINDOWS || MACCATALYST
        FocusGardenForKeyboard();
#endif
    }

    private void OnDrag(object? sender, TouchEventArgs e) => DragTo(e.Touches[0]);

    // Moving onto another board cell turns the press into a drag that marks or clears every cell it passes.
    // Moving into blank space doesn't: the press stays a possible tap until it reaches another cell.
    private void DragTo(PointF point) {
        if (_pressedCell is not { } start) return; // started off the board

        var path = _garden.CellsAlong(_lastPoint, point);
        bool changed = false;

        if (!_isDragging) {
            if (!path.Any(cell => cell != start)) {
                _lastPoint = point;
                return;
            }

            _isDragging = true;
            Session.BeginDrag();
            changed |= DragOver(start);
        }

        foreach (var cell in path) {
            changed |= DragOver(cell);
        }

        _lastPoint = point;

        if (changed) {
            _garden.Board = Session.Board;
            GardenView.Invalidate();
        }
    }

    // One cell of a drag, with a tick when it changes.
    private bool DragOver(Cell cell) {
        if (!Session.DragOver(cell)) return false;
        _haptics.Mark();
        return true;
    }

    // Finger or mouse up: the end of a drag, or a tap if it lifted on the cell it went down on.
    private void OnRelease(object? sender, TouchEventArgs e) {
        var pressed = _pressedCell;
        var point = e.Touches[0];

        // A fast swipe can cross cells after its last move update, so cover the stretch up to where it lifted.
        // A press that lifts on its own cell stays a tap: DragTo leaves it alone.
        if (pressed is not null) {
            DragTo(point);
        }

        bool wasDragging = _isDragging;
        FinishPress();

        if (wasDragging) return;

        var released = _garden.CellAt(point);
        if (released is null || released != pressed) return;

        OnCellTapped(released.Value);
    }

    // Clears the press. A drag in progress ends and is saved once.
    private void FinishPress() {
        if (_isDragging) {
            Session.EndDrag();
            SaveProgress();
        }

        _pressedCell = null;
        _isDragging = false;
    }

    private void OnCellTapped(Cell cell) => ShowResult(Session.Tap(cell, Environment.TickCount64));

    // Shows what a tap or key did, saves, and opens the end screen if the garden is over.
    private void ShowResult(TapResult result) {
        if (result.Outcome == TapOutcome.Ignored) return;
        CloseWormHint();

        PlayHaptic(result.Outcome);

        _garden.Board = Session.Board;
        UpdatePlantingList();
        UpdateToolShelf();
        UpdateSunMeter(animate: true);
        GardenView.Invalidate();

        SaveProgress();

        if (result.EndedGarden) {
            ShowEndOfGarden();
        }
    }

    // Mobile: A light tick for ✕, a firmer press for a plant, a buzz for a mistake.
    private void PlayHaptic(TapOutcome outcome) {
        switch (outcome) {
            case TapOutcome.MarkToggled:
                _haptics.Mark();
                break;
            case TapOutcome.Grew:
                _ = _haptics.GrewAsync();
                break;
            case TapOutcome.Wilted:
                _haptics.Wilted();
                break;
        }
    }

#if WINDOWS || MACCATALYST
    private void SetHover(Cell? cell) {
        if (_session is null) return; // Still loading.

        if (Session.Board.IsFinished || _garden.HoverCell == cell) return;

        _garden.HoverCell = cell;
        GardenView.Invalidate();
    }

    // Arrows move the focus ring; Space or X toggles ✕; Enter plants.
    private void OnGardenKey(GardenKeyboardKey key) {
        if (_session is null) return; // Still loading.

        int size = Session.Board.Size;
        var focus = _garden.FocusCell ?? new Cell(0, 0);
        _garden.ShowFocus = true;

        switch (key) {
            case GardenKeyboardKey.Up:
                focus = focus.MovedBy(-1, 0, size);
                break;
            case GardenKeyboardKey.Down:
                focus = focus.MovedBy(1, 0, size);
                break;
            case GardenKeyboardKey.Left:
                focus = focus.MovedBy(0, -1, size);
                break;
            case GardenKeyboardKey.Right:
                focus = focus.MovedBy(0, 1, size);
                break;
            case GardenKeyboardKey.Mark:
                ShowResult(Session.ToggleMark(focus));
                break;
            case GardenKeyboardKey.Plant:
                ShowResult(Session.Plant(focus));
                break;
        }

        _garden.FocusCell = focus;
        GardenView.Invalidate();
    }

    // Shows or hides the focus ring when the garden gains or loses keyboard focus.
    private void ShowKeyboardFocus(bool show) {
        _garden.ShowFocus = show;
        if (show) {
            _garden.FocusCell ??= new Cell(0, 0);
        }

        GardenView.Invalidate();
    }
#endif
    #endregion

    #region Sun meter
    private static readonly Color Ink = Color.FromArgb("#2E241B");
    private static readonly Color SunColor = Color.FromArgb("#F2B632");
    private static readonly Color SpentSunColor = Color.FromArgb("#D9CFB6");

    // The bright sun in each sun slot, left to right.
    // Spending a sun fades one out.
    private readonly List<View> _brightSuns = [];

    // Each sun slot stacks a bright sun on top of a spent one (pale, dashed outline).
    // A gentle arc: with more than one sun, the outer suns sit a little lower.
    private void BuildSunMeter(int suns) {
        SunMeter.Children.Clear();
        _brightSuns.Clear();

        for (int i = 0; i < suns; i++) {
            var spent = new Ellipse {
                Fill = new SolidColorBrush(SpentSunColor),
                Stroke = new SolidColorBrush(Ink),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 3, 3 },
            };
            var bright = new Ellipse {
                Fill = new SolidColorBrush(SunColor),
                Stroke = new SolidColorBrush(Ink),
                StrokeThickness = 2,
            };

            SunMeter.Children.Add(new Grid {
                WidthRequest = 44,
                HeightRequest = 44,
                TranslationY = suns > 1 && (i == 0 || i == suns - 1) ? 6 : 0,
                Children = { spent, bright },
            });
            _brightSuns.Add(bright);
        }
    }

    // Shows as many bright suns as the board has left.
    // The rightmost sun is spent first.
    private void UpdateSunMeter(bool animate) {
        var board = Session.Board;

        for (int i = 0; i < _brightSuns.Count; i++) {
            var bright = _brightSuns[i];
            double target = i < board.SunsLeft ? 1 : 0;
            if (bright.Opacity == target) continue;

            if (animate) {
                _ = bright.FadeToAsync(target, 300);
            } else {
                bright.Opacity = target;
            }
        }

        SemanticProperties.SetDescription(SunMeter, $"{board.SunsLeft} of {board.StartingSuns} suns left");
    }
    #endregion

    #region Tool shelf
    // Each tool's name and count; a tool works when there's one left and the garden isn't over.
    // The Rake is an unlimited tool that arrives with #49.
    private void UpdateToolShelf() {
        ShowTool(SeedsButton, "Seeds", PowerUp.Seeds);
        ShowTool(WormButton, "Worm", PowerUp.Worm);
        ShowTool(WaterButton, "Water", PowerUp.Water);
        RakeButton.IsEnabled = false;
    }

    private void ShowTool(Button button, string name, PowerUp powerUp) {
        int left = Session.PowerUps[powerUp];

        button.Text = $"{name}\n{(Session.UnlimitedTools ? "∞" : left)}";
        button.IsEnabled = Session.CanUse(powerUp);
        SemanticProperties.SetDescription(button, $"{name}, {left} left. {ToolTipProperties.GetText(button)}");
    }
    #endregion

    #region Worm
    // The hint on show, or null. It closes with Got it, or as soon as the player changes a cell.
    private WormHint? _wormHint;

    private void OnWormClicked(object? sender, EventArgs e) {
        if (_session is null) return; // Still loading.

        var hint = Session.UseWorm();
        if (hint is null) {
            InfoLabel.Text = "Your worm has nothing new to show.";
            return;
        }

        ShowWormHint(hint);
        UpdateToolShelf();
        SaveProgress(); // The spent Worm is saved, so reopening the app doesn't refill it.
    }

    // Opens the note and focuses the board on the step.
    private void ShowWormHint(WormHint hint) {
        _wormHint = hint;

        WormMessageLabel.Text = hint.Message;
        PlantingList.IsVisible = false;
        WormNote.IsVisible = true;

        _garden.HintFocus = hint.Focus;
        _garden.HintPlants = hint.Plants;
        _garden.HintRuleOuts = hint.RuleOuts;
        _garden.HintMistakes = hint.WrongMarks;
        GardenView.Invalidate();

        SemanticScreenReader.Announce(hint.Message);
    }

    // Makes the hint's changes. ShowResult then closes the note, saves, and handles the end of the garden.
    private void OnWormApplyClicked(object? sender, EventArgs e) {
        if (_wormHint is null) return;
        ShowResult(Session.ApplyHint());
    }

    // Puts the planting list back and clears the board's focus.
    private void CloseWormHint() {
        if (_wormHint is null) return;

        _wormHint = null;
        WormNote.IsVisible = false;
        PlantingList.IsVisible = true;

        _garden.HintFocus = null;
        _garden.HintPlants = null;
        _garden.HintRuleOuts = null;
        _garden.HintMistakes = null;
        GardenView.Invalidate();
    }
    #endregion
}

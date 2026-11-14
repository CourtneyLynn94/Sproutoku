# Sproutoku review guide

Sproutoku is a solo .NET 10 MAUI puzzle game for Android phones and Windows desktops.
Grow one plant in every bed (region); each row and column has exactly one plant;
plants never touch, not even diagonally. Gardens are N×N with N beds.

## Architecture
- `Sproutoku.Core`: all game rules, no MAUI references. Folder = namespace, in two groups:
  `Gardens` (`Puzzles`: what a garden is; `Solving`; `Generation`) and
  `Game` (`Session`: `GameSession`; `Boards`: what the player sees, `Board` is immutable; `Levels`; `PowerUps`; `Input`; `Saving`).
  Tests mirror these folders.
- `Sproutoku.App`: MAUI UI only, no game rules.
  - `Pages/` hold each screen as three files side by side, all in `Sproutoku.App.Pages`: the page (`GardenPage.xaml`), its code-behind (layout only) and its ViewModel (`GardenPageViewModel.cs`, CommunityToolkit.Mvvm). A control that later gets its own ViewModel keeps it next to the control the same way.
  - `Controls/` hold one control per part of the screen (`GardenView` with the drawing and touch, drag and keyboard input, `SunMeter`, `ToolShelf`, `PlantingList`, `WormNote`), which bind to the page's ViewModel.
  - ViewModels wrap `GameSession`, save, move between gardens and ask the player through the `Dialogs` service. `GardenPageViewModel` is a singleton: one game for the whole app, shared by Home, the Garden journal and Settings. Each ViewModel is split into `#region`s by part of the screen, matching the controls.
  - `Services/` (haptics, settings, storage, dialogs, preparing the next garden in the background) are registered in `MauiProgram` for dependency injection and arrive through constructors. Pages open by Shell route (`Routes`).
  - Android-only code lives in `Platforms/Android/`, Windows-only code in `Platforms/Windows/`, each as the other half of a partial class (for example `Haptics.Android.cs`, `GardenView.Keyboard.cs`), keeping the shared namespace.
- `GameSession` (Core) owns the level, board, tap meaning, progression, restarting and saving.
- Saves: `GameSave` (Core, JSON) and `GameStorage` (App, MAUI Preferences).
- Core is unit tested with xUnit. App code isn't unit tested; it's tested by hand on a phone and Windows.

## Code style (enforced by .editorconfig and `dotnet format` in CI)
- Opening braces on the same line, including `} else {`. `catch` and `finally` start on their own line.
- File-scoped namespaces, 4-space indents, `case` contents indented inside each `case`.
- Short `if (...) return;` guard lines are fine.
- `async void` is accepted for UI event handlers and for UI flows started from a tap
  (for example the end-of-garden screen), since nothing awaits them.

## Deliberate decisions: please don't flag these
- No license file, on purpose: all rights reserved.
- No Undo, Redo or Restart buttons.
- Double-tap plants, including on a ✕ cell. A single tap toggles ✕. Double-tap timing is 250 ms.
- The game never places ✕ marks automatically (no auto rule-outs after a plant). The only exceptions are the player applying a Worm hint and, later, the Water power-up revealing empty cells.
- "Reset all progress" clears only the `sproutoku.progress` Preferences container. Settings are kept on purpose.
- Dialogs, buttons and colors are placeholder UI until the real design is built.
- Starting over is only offered after losing ("Try again"). There is no last garden: levels are endless.
- Levels are endless and generated from each level's seed; level N is the same garden for every player.
- Power-ups: until the tool shed exists, every garden starts with 2 Seedlings, 2 Worms and 2 Water, refilled by a new garden or Try again. The Rake is an unlimited tool. The Unlimited tools developer option covers all of them.
- The Worm uses the player's ✕ marks that are correct (proven without peeking at the answer); ✕ marks on proven plants are called out as mistakes. A hint never locks the board: the player can hide it, keep playing and show it again for free. An open hint isn't saved.
- Hint messages and board labels use battleship names: columns A–J, rows 1–10, cells like B5.
- Haptics play as Android touch feedback (`VibrationAttributes` usage Touch), so they follow the phone's touch-vibration strength and go quiet if the player turns touch vibration off. Drag clicks closer than 35 ms apart wait their turn.
- A drag starts after a third of a cell, and one that starts along a row or column stays in it until the finger is past the middle of the next line. A press that slides off the board without reaching another cell stays a press.
- Settings live in the default Preferences container; Reset all progress never touches them.
- When it's built, the Rake clears every ✕ on the board, including ones from the Worm and Water. Only plants and wilts stay.
- Home, the garden, the Journal and the end screens don't scroll, by design; only Settings and How to play may. How large system text fits them is decided with the cozy UI (#131).

## Please do flag
- Game rules or state changes in App code that belong in `Sproutoku.Core`.
- Changes to rules or `GameSession` without unit tests.
- Anything that could corrupt or lose saved progress.
- Code that would behave differently on Android and Windows.
- UI that could block the player or leave them stuck.

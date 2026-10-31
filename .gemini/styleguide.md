# Sproutoku review guide

Sproutoku is a solo .NET 10 MAUI puzzle game for Android phones and Windows desktops.
Grow one plant in every bed (region); each row and column has exactly one plant;
plants never touch, not even diagonally. Gardens are N×N with N beds.

## Architecture
- `Sproutoku.Core`: all game rules, no MAUI references. `Board` is immutable: every change returns a new Board.
- `GameSession` (Core) owns the level, board, tap meaning, progression, restarting and saving.
- `Sproutoku.App`: MAUI UI only. Pages pass input to `GameSession` and show the results.
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
- No Undo, Redo or Restart buttons. Starting over is only offered after losing or after the last available garden.
- Double-tap plants, including on a ✕ cell. A single tap toggles ✕. Double-tap timing is 250 ms.
- A wrong plant wilts, locks the cell and costs one sun; it never goes back to empty.
- The game never places ✕ marks automatically (no auto rule-outs after a plant).
- "Reset all progress" clears only the `sproutoku.progress` Preferences container. Settings are kept on purpose.
- Dialogs, buttons and colors are placeholder UI until the real design is built.
- There are only three handmade starter gardens until the generator is built; levels will be endless later.

## Please do flag
- Game rules or state changes in App code that belong in `Sproutoku.Core`.
- Changes to rules or `GameSession` without unit tests.
- Anything that could corrupt or lose saved progress.
- Code that would behave differently on Android and Windows.
- UI that could block the player or leave them stuck.
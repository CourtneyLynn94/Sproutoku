namespace Sproutoku.App.Services;

/// <summary>
/// Questions and messages for the player, so ViewModels can ask without knowing about pages.
/// Each one shows over whatever page is on top, including a modal one like Settings.
/// Placeholder system dialogs until the cozy UI's own sheets.
/// </summary>
public sealed class Dialogs {
    /// <summary>
    /// A message with one button.
    /// </summary>
    public Task AlertAsync(string title, string message, string cancel) =>
        TopPage.DisplayAlertAsync(title, message, cancel);

    /// <summary>
    /// A yes-or-no question. True if the player chose <paramref name="accept"/>.
    /// </summary>
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        TopPage.DisplayAlertAsync(title, message, accept, cancel);

    /// <summary>
    /// A list of choices. Returns the chosen one, the cancel or destruction text, or null if the sheet was dismissed.
    /// </summary>
    public async Task<string?> ChooseAsync(string title, string cancel, string? destruction, params string[] buttons) =>
        await TopPage.DisplayActionSheetAsync(title, cancel, destruction, buttons);

    /// <summary>
    /// A typed answer, or null if the player cancelled.
    /// </summary>
    public async Task<string?> PromptAsync(
        string title,
        string message,
        string accept = "OK",
        string cancel = "Cancel",
        string? placeholder = null,
        int maxLength = -1,
        Keyboard? keyboard = null) =>
        await TopPage.DisplayPromptAsync(title, message, accept, cancel, placeholder, maxLength, keyboard);

    // The page the player is looking at: the top modal page if one is open, otherwise the shell's current page.
    private static Page TopPage {
        get {
            var root = Application.Current?.Windows.FirstOrDefault()?.Page
                ?? throw new InvalidOperationException("No page is showing yet.");

            return root.Navigation.ModalStack.LastOrDefault()
                ?? (root as Shell)?.CurrentPage
                ?? root;
        }
    }
}

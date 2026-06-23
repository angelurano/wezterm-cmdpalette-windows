using System;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace WeztermExtensionForCommandPalette;

/// <summary>
/// A commands provider that exposes WezTerm-specific commands to the Command Palette.
/// </summary>
public partial class WeztermExtensionForCommandPaletteCommandsProvider : CommandProvider
{
    private readonly WeztermExtensionForCommandPalettePage _page;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeztermExtensionForCommandPaletteCommandsProvider"/> class.
    /// Sets default display properties and icons.
    /// </summary>
    /// <param name="page">The page dependency containing WezTerm profiles.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="page"/> is null.</exception>
    public WeztermExtensionForCommandPaletteCommandsProvider(WeztermExtensionForCommandPalettePage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        DisplayName = Resources.GetString("ProfilesTitle");
        Icon = IconHelpers.FromRelativePath("Assets\\wezterm_logo.png");
    }

    /// <summary>
    /// Retrieves the top-level commands offered by this provider.
    /// </summary>
    /// <returns>An array of <see cref="ICommandItem"/> representing top-level commands.</returns>
    public override ICommandItem[] TopLevelCommands()
    {
        return [
            new CommandItem(_page)
            {
                Title = Resources.GetString("ProfilesTitle"),
                Subtitle = Resources.GetString("ProfilesSubtitle"),
                Icon = IconHelpers.FromRelativePath("Assets\\wezterm_logo.png")
            }
        ];
    }
}



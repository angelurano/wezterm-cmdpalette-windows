// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace WeztermExtensionForCommandPalette;

/// <summary>
/// A list page showing WezTerm profiles parsed from the user configuration.
/// Allows the user to select and launch a specific profile.
/// </summary>
public sealed partial class WeztermExtensionForCommandPalettePage : ListPage
{
    private readonly IWeztermConfigProvider _configProvider;
    private readonly IWeztermExecutionProvider _executionProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeztermExtensionForCommandPalettePage"/> class.
    /// Sets default page properties and dependencies.
    /// </summary>
    /// <param name="configProvider">The provider to get configuration.</param>
    /// <param name="executionProvider">The provider to launch profiles.</param>
    /// <exception cref="ArgumentNullException">Thrown when dependencies are null.</exception>
    public WeztermExtensionForCommandPalettePage(
        IWeztermConfigProvider configProvider,
        IWeztermExecutionProvider executionProvider)
    {
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _executionProvider = executionProvider ?? throw new ArgumentNullException(nameof(executionProvider));

        Icon = IconHelpers.FromRelativePath("Assets\\wezterm_logo.png");
        Title = "Wezterm Profiles";
        Name = "Wezterm";
    }

    /// <summary>
    /// Populates the list items representing the configured WezTerm profiles.
    /// </summary>
    /// <returns>An array of list items representing WezTerm profiles.</returns>
    public override IListItem[] GetItems()
    {
        var profiles = Task.Run(() => _configProvider.GetProfilesAsync()).GetAwaiter().GetResult();
        var items = new IListItem[profiles.Count];

        for (int i = 0; i < profiles.Count; i++)
        {
            var profile = profiles[i];
            var command = new AnonymousCommand(() =>
            {
                _executionProvider.LaunchProfile(profile);
            });

            command.Result = CommandResult.Dismiss();

            string subtitle;
            if (!string.IsNullOrEmpty(profile.Domain))
            {
                var cmdStr = (profile.Args != null && profile.Args.Count > 0) ? $" | Command: {string.Join(" ", profile.Args)}" : string.Empty;
                var cwdStr = !string.IsNullOrEmpty(profile.Cwd) ? $" | Directory: {profile.Cwd}" : string.Empty;
                subtitle = $"Domain: {profile.Domain}{cmdStr}{cwdStr}";
            }
            else if (profile.Args != null && profile.Args.Count > 0)
            {
                var cwdStr = !string.IsNullOrEmpty(profile.Cwd) ? $" | Directory: {profile.Cwd}" : string.Empty;
                subtitle = $"Command: {string.Join(" ", profile.Args)}{cwdStr}";
            }
            else if (!string.IsNullOrEmpty(profile.Cwd))
            {
                subtitle = $"Directory: {profile.Cwd}";
            }
            else
            {
                subtitle = "Launch default WezTerm session";
            }

            items[i] = new ListItem(command)
            {
                Title = profile.Label,
                Subtitle = subtitle,
                Icon = IconHelpers.FromRelativePath("Assets\\wezterm_logo.png")
            };
        }

        return items;
    }
}


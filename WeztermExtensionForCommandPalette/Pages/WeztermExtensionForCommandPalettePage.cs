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
    private List<WeztermProfile>? _lastProfiles;
    private IListItem[] _cachedItems = [];
    private bool _isLoading;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeztermExtensionForCommandPalettePage"/> class.
    /// Sets default page properties, dependencies, and triggers the initial background profile load.
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
        Title = Resources.GetString("ProfilesTitle");
        Name = Resources.GetString("ProfilesTitle");

        // Pre-fetch profiles in background on page initialization.
        _ = RefreshProfilesIfNeededAsync();
    }

    /// <summary>
    /// Populates the list items representing the configured WezTerm profiles.
    /// Returns cached items immediately and triggers an asynchronous refresh in the background.
    /// </summary>
    /// <returns>An array of list items representing WezTerm profiles.</returns>
    public override IListItem[] GetItems()
    {
        _ = RefreshProfilesIfNeededAsync();
        return _cachedItems;
    }

    /// <summary>
    /// Checks if configuration has changed and asynchronously updates list items in the background.
    /// Triggers <see cref="ListPage.RaiseItemsChanged"/> if changes are found.
    /// </summary>
    /// <returns>A task representing the asynchronous refresh operation.</returns>
    private async Task RefreshProfilesIfNeededAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        try
        {
            var profiles = await _configProvider.GetProfilesAsync().ConfigureAwait(false);
            if (_lastProfiles != profiles)
            {
                _lastProfiles = profiles;
                var items = new IListItem[profiles.Count];

                for (int i = 0; i < profiles.Count; i++)
                {
                    var profile = profiles[i];
                    var command = new AnonymousCommand(() =>
                    {
                        _executionProvider.LaunchProfile(profile);
                    })
                    {
                        Name = Resources.GetString("StartProfile")
                    };

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
                        subtitle = Resources.GetString("DefaultSessionSubtitle");
                    }

                    items[i] = new ListItem(command)
                    {
                        Title = profile.Label,
                        Subtitle = subtitle,
                        Icon = IconHelpers.FromRelativePath("Assets\\wezterm_logo.png")
                    };
                }

                _cachedItems = items;
                RaiseItemsChanged();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading profiles asynchronously: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }
}


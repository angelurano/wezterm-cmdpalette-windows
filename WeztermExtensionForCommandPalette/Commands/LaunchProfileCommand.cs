// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace WeztermExtensionForCommandPalette.Commands;

/// <summary>
/// A concrete command that launches a specific WezTerm profile.
/// </summary>
public sealed partial class LaunchProfileCommand : InvokableCommand
{
    private readonly IWeztermExecutionProvider _executionProvider;
    private readonly WeztermProfile _profile;
    private readonly bool _asAdmin;

    /// <summary>
    /// Initializes a new instance of the <see cref="LaunchProfileCommand"/> class.
    /// </summary>
    /// <param name="executionProvider">The execution provider dependency.</param>
    /// <param name="profile">The profile to launch.</param>
    /// <param name="asAdmin">True to launch the profile with elevated administrator privileges; otherwise false.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="executionProvider"/> or <paramref name="profile"/> is null.</exception>
    public LaunchProfileCommand(IWeztermExecutionProvider executionProvider, WeztermProfile profile, bool asAdmin = false)
    {
        _executionProvider = executionProvider ?? throw new ArgumentNullException(nameof(executionProvider));
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _asAdmin = asAdmin;

        Name = Resources.GetString(asAdmin ? "StartProfileAsAdmin" : "StartProfile");
    }

    /// <inheritdoc/>
    public override ICommandResult Invoke()
    {
        _executionProvider.LaunchProfile(_profile, _asAdmin);
        return CommandResult.Dismiss();
    }
}

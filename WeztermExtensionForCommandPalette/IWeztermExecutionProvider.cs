// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace WeztermExtensionForCommandPalette;

/// <summary>
/// Defines a provider contract for launching WezTerm processes.
/// </summary>
public interface IWeztermExecutionProvider
{
    /// <summary>
    /// Launches a specific WezTerm profile, optionally as administrator.
    /// </summary>
    /// <param name="profile">The profile to launch.</param>
    /// <param name="asAdmin">True to launch with elevated administrator privileges; otherwise false.</param>
    void LaunchProfile(WeztermProfile profile, bool asAdmin = false);
}

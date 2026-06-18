// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace WeztermExtensionForCommandPalette;

/// <summary>
/// Represents a parsed WezTerm profile configuration.
/// </summary>
public class WeztermProfile
{
    /// <summary>
    /// Gets or sets the display label of the profile.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the working directory for the profile.
    /// </summary>
    public string? Cwd { get; set; }

    /// <summary>
    /// Gets or sets the connection domain (e.g. WSL distro, SSH domain, or "local") for the profile.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Gets or sets the list of arguments to execute within the profile session.
    /// </summary>
    public List<string> Args { get; set; } = [];
}

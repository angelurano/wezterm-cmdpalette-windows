// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace WeztermExtensionForCommandPalette;

/// <summary>
/// Defines a factory contract for creating instances of <see cref="WeztermProfile"/>.
/// </summary>
public interface IWeztermProfileFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="WeztermProfile"/>.
    /// </summary>
    /// <param name="label">The display label of the profile.</param>
    /// <param name="cwd">The working directory for the profile.</param>
    /// <param name="domain">The connection domain for the profile.</param>
    /// <param name="args">The list of execution arguments.</param>
    /// <returns>A new <see cref="WeztermProfile"/> instance.</returns>
    WeztermProfile CreateProfile(string label, string? cwd, string? domain, List<string> args);
}

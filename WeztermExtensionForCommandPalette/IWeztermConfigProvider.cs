// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace WeztermExtensionForCommandPalette;

/// <summary>
/// Defines a provider contract for scanning and parsing WezTerm profiles from configuration files.
/// </summary>
public interface IWeztermConfigProvider
{
    /// <summary>
    /// Scans configuration files asynchronously and returns the list of parsed profiles.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, returning a list of <see cref="WeztermProfile"/>.</returns>
    Task<List<WeztermProfile>> GetProfilesAsync();
}

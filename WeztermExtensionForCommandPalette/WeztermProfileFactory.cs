// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace WeztermExtensionForCommandPalette;

/// <summary>
/// Default implementation of the <see cref="IWeztermProfileFactory"/> interface.
/// </summary>
public class WeztermProfileFactory : IWeztermProfileFactory
{
    /// <inheritdoc/>
    public WeztermProfile CreateProfile(string label, string? cwd, string? domain, List<string> args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(args);

        return new WeztermProfile
        {
            Label = label,
            Cwd = cwd,
            Domain = domain,
            Args = args
        };
    }
}

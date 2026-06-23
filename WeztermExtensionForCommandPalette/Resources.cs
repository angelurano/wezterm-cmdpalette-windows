// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.ApplicationModel.Resources;

namespace WeztermExtensionForCommandPalette;

/// <summary>
/// Helper class to load localized resources dynamically.
/// </summary>
public static class Resources
{
    private static readonly ResourceLoader _loader = new();

    /// <summary>
    /// Retrieves a localized string for the specified key.
    /// </summary>
    /// <param name="resourceKey">The key of the resource to retrieve.</param>
    /// <returns>The localized string if found; otherwise, the key itself.</returns>
    public static string GetString(string resourceKey)
    {
        try
        {
            return _loader.GetString(resourceKey);
        }
        catch
        {
            return resourceKey;
        }
    }
}

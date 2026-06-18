// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CommandPalette.Extensions;

namespace WeztermExtensionForCommandPalette;

/// <summary>
/// Main extension class for the WezTerm Extension for Command Palette.
/// Handles extension initialization and lifecycle management.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="WeztermExtensionForCommandPalette"/> class.
/// </remarks>
/// <param name="extensionDisposedEvent">The event to signal when the extension is disposed.</param>
/// <param name="provider">The command provider dependency.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="extensionDisposedEvent"/> or <paramref name="provider"/> is null.</exception>
[Guid("87e17ce2-a3a6-4a5a-8f35-3453c168ac2a")]
public sealed partial class WeztermExtensionForCommandPalette(
    ManualResetEvent extensionDisposedEvent,
    WeztermExtensionForCommandPaletteCommandsProvider provider) : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent = extensionDisposedEvent ?? throw new ArgumentNullException(nameof(extensionDisposedEvent));
    private readonly WeztermExtensionForCommandPaletteCommandsProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    /// <summary>
    /// Gets the provider for the specified provider type.
    /// </summary>
    /// <param name="providerType">The type of provider to get.</param>
    /// <returns>The provider instance if supported; otherwise, <c>null</c>.</returns>
    public object? GetProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.Commands => _provider,
            _ => null,
        };
    }

    /// <summary>
    /// Disposes the extension resources and signals the disposal event.
    /// </summary>
    public void Dispose() => this._extensionDisposedEvent.Set();
}

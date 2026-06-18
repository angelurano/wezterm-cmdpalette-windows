// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WeztermExtensionForCommandPalette;

/// <summary>
/// The entry point class of the extension application.
/// </summary>
public class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// Registers the process as a COM server if the appropriate arguments are passed.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    [MTAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "-RegisterProcessAsComServer", StringComparison.Ordinal))
        {
            global::Shmuelie.WinRTServer.ComServer server = new();

            using (ManualResetEvent extensionDisposedEvent = new(false))
            {
                var services = new ServiceCollection();
                
                services.AddSingleton<IWeztermProfileFactory, WeztermProfileFactory>();
                services.AddSingleton<IWeztermConfigProvider, WeztermConfigProvider>();
                services.AddSingleton<IWeztermExecutionProvider, WeztermExecutionProvider>();
                
                services.AddSingleton<WeztermExtensionForCommandPalettePage>();
                services.AddSingleton<WeztermExtensionForCommandPaletteCommandsProvider>();
                
                services.AddSingleton(extensionDisposedEvent);
                services.AddSingleton<WeztermExtensionForCommandPalette>();

                using var serviceProvider = services.BuildServiceProvider();

                var extensionInstance = serviceProvider.GetRequiredService<WeztermExtensionForCommandPalette>();
                server.RegisterClass<WeztermExtensionForCommandPalette, IExtension>(() => extensionInstance);
                server.Start();
                
                // Wait for the extension to be disposed before stopping the server.
                extensionDisposedEvent.WaitOne();
                server.Stop();
                server.UnsafeDispose();
            }
        }
        else
        {
            Console.WriteLine("Not being launched as a Extension... exiting.");
        }
    }
}

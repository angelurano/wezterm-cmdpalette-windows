// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace WeztermExtensionForCommandPalette;

/// <summary>
/// Default implementation of the <see cref="IWeztermExecutionProvider"/> interface.
/// Handles searching for the WezTerm executable and launching the terminal process safely.
/// </summary>
public partial class WeztermExecutionProvider : IWeztermExecutionProvider
{
    private readonly ILogger<WeztermExecutionProvider> _logger;
    private string? _resolvedWeztermPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeztermExecutionProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when dependencies are null.</exception>
    public WeztermExecutionProvider(ILogger<WeztermExecutionProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public void LaunchProfile(WeztermProfile profile, bool asAdmin = false)
    {
        ArgumentNullException.ThrowIfNull(profile);

        try
        {
            _ = AllowSetForegroundWindow(ASFW_ANY);
            var weztermPath = FindWeztermPath();
            var args = new List<string> { "start", "--always-new-process" };

            if (!string.IsNullOrEmpty(profile.Cwd) && profile.Cwd != "~")
            {
                args.Add("--cwd");
                args.Add(profile.Cwd);
            }

            if (!string.IsNullOrEmpty(profile.Domain))
            {
                args.Add("--domain");
                args.Add(profile.Domain);
            }

            if (profile.Args != null && profile.Args.Count > 0)
            {
                args.Add("--");
                foreach (var arg in profile.Args)
                {
                    args.Add(arg);
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = weztermPath,
                UseShellExecute = asAdmin,
                CreateNoWindow = true
            };
            if (asAdmin)
            {
                startInfo.Verb = "runas";
            }

            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    _ = AllowSetForegroundWindow(ASFW_ANY);

                    var process = Process.Start(startInfo);
                    if (process != null && !asAdmin)
                    {
                        _ = AllowSetForegroundWindow(process.Id);

                        // Wait up to 2 seconds for the main window handle to be created in background
                        int attempts = 0;
                        while (process.MainWindowHandle == IntPtr.Zero && !process.HasExited && attempts < 40)
                        {
                            System.Threading.Thread.Sleep(50);
                            process.Refresh();
                            attempts++;
                        }

                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            ForceForeground(process.MainWindowHandle);
                        }
                        else
                        {
                            // Fallback if no window handle was resolved (e.g. if it's a launcher shim)
                            _ = AllowSetForegroundWindow(ASFW_ANY);
                        }
                    }
                    else if (process != null && asAdmin)
                    {
                        // For elevated processes, just ensure focus permission is delegated generally
                        _ = AllowSetForegroundWindow(ASFW_ANY);
                    }
                }
                catch (Exception ex)
                {
                    LogStaThreadLaunchError(ex);
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();

            // Wait very briefly for the STA thread to initialize startup, then return.
            // This dismisses the Command Palette immediately for a fluid UI.
            _ = thread.Join(50);
        }
        catch (Exception ex)
        {
            LogLaunchError(ex);
        }
    }

    private string FindWeztermPath()
    {
        if (_resolvedWeztermPath != null)
        {
            return _resolvedWeztermPath;
        }

        ReadOnlySpan<string> exeNames = ["wezterm-gui.exe", "wezterm.exe"];
        var folders = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "WezTerm"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WezTerm"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WezTerm"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "apps", "wezterm", "current"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims"),
        };

        foreach (var exeName in exeNames)
        {
            foreach (var folder in folders)
            {
                var path = Path.Combine(folder, exeName);
                if (File.Exists(path))
                {
                    _resolvedWeztermPath = path;
                    return path;
                }
            }
        }

        _resolvedWeztermPath = "wezterm-gui.exe";
        return _resolvedWeztermPath;
    }


    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static partial bool AllowSetForegroundWindow(int dwProcessId);

    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)] bool fAttach);

    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static partial bool BringWindowToTop(IntPtr hWnd);

    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int ASFW_ANY = -1;
    private const int SW_SHOW = 5;

    private static void ForceForeground(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return;

        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == hWnd) return;

        uint foregroundThread = GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
        uint appThread = GetWindowThreadProcessId(hWnd, IntPtr.Zero);

        if (foregroundThread != 0 && appThread != 0 && foregroundThread != appThread)
        {
            try
            {
                _ = AttachThreadInput(foregroundThread, appThread, true);
                _ = ShowWindow(hWnd, SW_SHOW);
                _ = BringWindowToTop(hWnd);
                _ = SetForegroundWindow(hWnd);
                _ = AttachThreadInput(foregroundThread, appThread, false);
            }
            catch
            {
                _ = ShowWindow(hWnd, SW_SHOW);
                _ = BringWindowToTop(hWnd);
                _ = SetForegroundWindow(hWnd);
            }
        }
        else
        {
            _ = ShowWindow(hWnd, SW_SHOW);
            _ = BringWindowToTop(hWnd);
            _ = SetForegroundWindow(hWnd);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Error launching WezTerm in STA thread")]
    private partial void LogStaThreadLaunchError(Exception ex);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Error launching WezTerm")]
    private partial void LogLaunchError(Exception ex);
}

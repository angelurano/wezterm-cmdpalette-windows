// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace WeztermExtensionForCommandPalette;

/// <summary>
/// Default implementation of the <see cref="IWeztermExecutionProvider"/> interface.
/// Handles searching for the WezTerm executable and launching the terminal process safely.
/// </summary>
public class WeztermExecutionProvider : IWeztermExecutionProvider
{
    private string? _resolvedWeztermPath;

    /// <inheritdoc/>
    public void LaunchProfile(WeztermProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        try
        {
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

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < args.Count; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(EscapeArgument(args[i]));
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = weztermPath,
                Arguments = sb.ToString(),
                UseShellExecute = true
            };

            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    _ = AllowSetForegroundWindow(ASFW_ANY);

                    var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        _ = AllowSetForegroundWindow(process.Id);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error launching Wezterm in STA thread: {ex.Message}");
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error launching Wezterm: {ex.Message}");
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
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "apps", "wezterm", "current"),
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

        return "wezterm-gui.exe";
    }

    private static string EscapeArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
        {
            return "\"\"";
        }

        if (arg.AsSpan().IndexOfAny(' ', '\t', '"') < 0)
        {
            return arg;
        }

        var sb = new System.Text.StringBuilder(arg.Length + 5);
        sb.Append('"');
        for (int i = 0; i < arg.Length; i++)
        {
            int backslashCount = 0;
            while (i < arg.Length && arg[i] == '\\')
            {
                backslashCount++;
                i++;
            }

            if (i < arg.Length)
            {
                if (arg[i] == '"')
                {
                    sb.Append('\\', backslashCount * 2 + 1);
                    sb.Append('"');
                }
                else
                {
                    sb.Append('\\', backslashCount);
                    sb.Append(arg[i]);
                }
            }
            else
            {
                sb.Append('\\', backslashCount * 2);
            }
        }
        sb.Append('"');
        return sb.ToString();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);

    private const int ASFW_ANY = -1;
}

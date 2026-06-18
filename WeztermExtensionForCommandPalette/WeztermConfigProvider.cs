// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WeztermExtensionForCommandPalette;

/// <summary>
/// Default implementation of the <see cref="IWeztermConfigProvider"/> interface.
/// Provides configuration scanning, comment stripping, parsing, and caching.
/// </summary>
public partial class WeztermConfigProvider(IWeztermProfileFactory profileFactory) : IWeztermConfigProvider, IDisposable
{
    private readonly IWeztermProfileFactory _profileFactory = profileFactory ?? throw new ArgumentNullException(nameof(profileFactory));
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _resolvedConfigPath;
    private DateTime _lastConfigWriteTime = DateTime.MinValue;
    private List<WeztermProfile>? _cachedProfiles;

    /// <inheritdoc/>
    public async Task<List<WeztermProfile>> GetProfilesAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_resolvedConfigPath != null && _cachedProfiles != null)
            {
                try
                {
                    if (File.Exists(_resolvedConfigPath))
                    {
                        var currentWriteTime = File.GetLastWriteTime(_resolvedConfigPath);
                        if (currentWriteTime == _lastConfigWriteTime)
                        {
                            return _cachedProfiles;
                        }
                    }
                    else
                    {
                        _resolvedConfigPath = null;
                        _lastConfigWriteTime = DateTime.MinValue;
                        _cachedProfiles = null;
                    }
                }
                catch
                {
                }
            }

            var profiles = new List<WeztermProfile>();
            string? configContent = null;
            string? foundPath = null;

            // Resolve WezTerm configuration file by scanning env vars and user directories in order of precedence.
            var weztermConfigFile = Environment.GetEnvironmentVariable("WEZTERM_CONFIG_FILE");
            if (!string.IsNullOrEmpty(weztermConfigFile) && File.Exists(weztermConfigFile))
            {
                configContent = await TryReadFileAsync(weztermConfigFile).ConfigureAwait(false);
                if (configContent != null) foundPath = weztermConfigFile;
            }

            if (configContent == null)
            {
                var weztermConfigDir = Environment.GetEnvironmentVariable("WEZTERM_CONFIG_DIR");
                if (!string.IsNullOrEmpty(weztermConfigDir))
                {
                    var path = Path.Combine(weztermConfigDir, "wezterm.lua");
                    if (File.Exists(path))
                    {
                        configContent = await TryReadFileAsync(path).ConfigureAwait(false);
                        if (configContent != null) foundPath = path;
                    }
                }
            }

            if (configContent == null)
            {
                var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (!string.IsNullOrEmpty(xdgConfigHome))
                {
                    var path = Path.Combine(xdgConfigHome, "wezterm", "wezterm.lua");
                    if (File.Exists(path))
                    {
                        configContent = await TryReadFileAsync(path).ConfigureAwait(false);
                        if (configContent != null) foundPath = path;
                    }
                }
            }

            if (configContent == null)
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(userProfile))
                {
                    var path1 = Path.Combine(userProfile, ".wezterm.lua");
                    if (File.Exists(path1))
                    {
                        configContent = await TryReadFileAsync(path1).ConfigureAwait(false);
                        if (configContent != null) foundPath = path1;
                    }
                    else
                    {
                        var path2 = Path.Combine(userProfile, ".config", "wezterm", "wezterm.lua");
                        if (File.Exists(path2))
                        {
                            configContent = await TryReadFileAsync(path2).ConfigureAwait(false);
                            if (configContent != null) foundPath = path2;
                        }
                    }
                }
            }

            if (configContent == null)
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (!string.IsNullOrEmpty(appData))
                {
                    var path = Path.Combine(appData, "wezterm", "wezterm.lua");
                    if (File.Exists(path))
                    {
                        configContent = await TryReadFileAsync(path).ConfigureAwait(false);
                        if (configContent != null) foundPath = path;
                    }
                }
            }

            if (!string.IsNullOrEmpty(configContent) && foundPath != null)
            {
                try
                {
                    string stripped = StripLuaComments(configContent);
                    var launchMenuBlock = ExtractLaunchMenuBlock(stripped.AsSpan());
                    var items = ExtractItems(launchMenuBlock);

                    foreach (var itemBlock in items)
                    {
                        var labelMatch = LabelRegex().Match(itemBlock);
                        if (!labelMatch.Success) continue;

                        string label = UnescapeLuaString(labelMatch.Groups[2].Value);

                        var cwdMatch = CwdRegex().Match(itemBlock);
                        string? cwd = cwdMatch.Success ? UnescapeLuaString(cwdMatch.Groups[2].Value) : null;

                        string? domain = null;
                        var domainTableMatch = DomainTableRegex().Match(itemBlock);
                        if (domainTableMatch.Success)
                        {
                            domain = UnescapeLuaString(domainTableMatch.Groups[2].Value);
                        }
                        else
                        {
                            var domainStringMatch = DomainStringRegex().Match(itemBlock);
                            if (domainStringMatch.Success)
                            {
                                domain = UnescapeLuaString(domainStringMatch.Groups[2].Value);
                            }
                        }

                        var argsList = new List<string>();
                        var argsMatch = ArgsRegex().Match(itemBlock);
                        if (argsMatch.Success)
                        {
                            var argsContent = argsMatch.Groups[1].Value;
                            var stringMatches = ArgsStringRegex().Matches(argsContent);
                            foreach (Match m in stringMatches)
                            {
                                argsList.Add(UnescapeLuaString(m.Groups[2].Value));
                            }
                        }

                        var profile = _profileFactory.CreateProfile(label, cwd, domain, argsList);
                        profiles.Add(profile);
                    }

                    _resolvedConfigPath = foundPath;
                    _lastConfigWriteTime = File.GetLastWriteTime(foundPath);
                    _cachedProfiles = profiles;
                }
                catch
                {
                }
            }

            if (profiles.Count == 0)
            {
                profiles.Add(_profileFactory.CreateProfile("WezTerm (Default)", null, null, []));
                profiles.Add(_profileFactory.CreateProfile("PowerShell (WezTerm)", null, "local", ["pwsh.exe", "-NoLogo"]));
                profiles.Add(_profileFactory.CreateProfile("Command Prompt (WezTerm)", null, "local", ["cmd.exe"]));
            }

            return profiles;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static async Task<string?> TryReadFileAsync(string path)
    {
        try
        {
            return await File.ReadAllTextAsync(path).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    private static string StripLuaComments(string content)
    {
        var sb = new System.Text.StringBuilder(content.Length);
        foreach (var line in content.AsSpan().EnumerateLines())
        {
            var lineToAppend = line;
            int commentIndex = line.IndexOf("--".AsSpan(), StringComparison.Ordinal);
            if (commentIndex >= 0)
            {
                int doubleQuotes = 0;
                int singleQuotes = 0;
                for (int j = 0; j < commentIndex; j++)
                {
                    if (line[j] == '"') doubleQuotes++;
                    if (line[j] == '\'') singleQuotes++;
                }
                if (doubleQuotes % 2 == 0 && singleQuotes % 2 == 0)
                {
                    lineToAppend = line[..commentIndex];
                }
            }
            sb.Append(lineToAppend);
            sb.Append('\n');
        }
        return sb.ToString();
    }

    private static ReadOnlySpan<char> ExtractLaunchMenuBlock(ReadOnlySpan<char> content)
    {
        int index = content.IndexOf("launch_menu".AsSpan(), StringComparison.Ordinal);
        if (index < 0) return [];

        int openBraceIndex = content[index..].IndexOf('{');
        if (openBraceIndex < 0) return [];

        openBraceIndex += index;

        int braceCount = 1;
        int currentIndex = openBraceIndex + 1;
        while (currentIndex < content.Length && braceCount > 0)
        {
            char c = content[currentIndex];
            if (c == '{') braceCount++;
            else if (c == '}') braceCount--;
            currentIndex++;
        }

        if (braceCount == 0)
        {
            return content[openBraceIndex..currentIndex];
        }

        return [];
    }

    private static List<string> ExtractItems(ReadOnlySpan<char> block)
    {
        var items = new List<string>();
        if (block.IsEmpty || block.Length < 2) return items;

        ReadOnlySpan<char> inner = block[1..^1];

        int currentIndex = 0;
        while (currentIndex < inner.Length)
        {
            int openBraceIndex = inner[currentIndex..].IndexOf('{');
            if (openBraceIndex < 0) break;

            openBraceIndex += currentIndex;

            int braceCount = 1;
            int nextIndex = openBraceIndex + 1;
            while (nextIndex < inner.Length && braceCount > 0)
            {
                char c = inner[nextIndex];
                if (c == '{') braceCount++;
                else if (c == '}') braceCount--;
                nextIndex++;
            }

            if (braceCount == 0)
            {
                items.Add(inner[openBraceIndex..nextIndex].ToString());
                currentIndex = nextIndex;
            }
            else
            {
                break;
            }
        }

        return items;
    }

    private static string UnescapeLuaString(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (!value.Contains('\\')) return value;
        return value
            .Replace("\\\\", "\\")
            .Replace("\\\"", "\"")
            .Replace("\\'", "'")
            .Replace("\\t", "\t")
            .Replace("\\n", "\n");
    }

    [GeneratedRegex(@"label\s*=\s*([""'])((?:\\.|(?!\1).)*)\1", RegexOptions.Compiled)]
    private static partial Regex LabelRegex();

    [GeneratedRegex(@"cwd\s*=\s*([""'])((?:\\.|(?!\1).)*)\1", RegexOptions.Compiled)]
    private static partial Regex CwdRegex();

    [GeneratedRegex(@"domain\s*=\s*\{\s*DomainName\s*=\s*([""'])((?:\\.|(?!\1).)*)\1\s*\}", RegexOptions.Compiled)]
    private static partial Regex DomainTableRegex();

    [GeneratedRegex(@"domain\s*=\s*([""'])((?:\\.|(?!\1).)*)\1", RegexOptions.Compiled)]
    private static partial Regex DomainStringRegex();

    [GeneratedRegex(@"args\s*=\s*\{([^}]*)\}", RegexOptions.Compiled)]
    private static partial Regex ArgsRegex();

    [GeneratedRegex(@"([""'])((?:\\.|(?!\1).)*)\1", RegexOptions.Compiled)]
    private static partial Regex ArgsStringRegex();

    /// <summary>
    /// Disposes the resources used by the config provider, including the thread lock.
    /// </summary>
    public void Dispose()
    {
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}

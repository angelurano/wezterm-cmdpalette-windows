using FluentAssertions;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WeztermExtensionForCommandPalette.Tests;

[TestClass]
[DoNotParallelize]
public class WeztermConfigProviderTests
{
    private Mock<IWeztermProfileFactory> _mockFactory = null!;
    private WeztermConfigProvider _provider = null!;
    private string? _tempFilePath;

    [TestInitialize]
    public void SetUp()
    {
        _mockFactory = new Mock<IWeztermProfileFactory>(MockBehavior.Strict);
        _provider = new WeztermConfigProvider(_mockFactory.Object);
        // Clear environment variables before each test
        Environment.SetEnvironmentVariable("WEZTERM_CONFIG_FILE", null);
        Environment.SetEnvironmentVariable("WEZTERM_CONFIG_DIR", null);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", null);
    }

    [TestCleanup]
    public void TearDown()
    {
        _provider.Dispose();
        if (_tempFilePath != null && File.Exists(_tempFilePath))
        {
            try
            {
                File.Delete(_tempFilePath);
            }
            catch { }
        }
        Environment.SetEnvironmentVariable("WEZTERM_CONFIG_FILE", null);
    }

    [TestMethod]
    public async Task GetProfilesAsync_WhenNoConfigFileFound_ShouldReturnFallbackProfiles()
    {
        // Arrange
        _mockFactory.Setup(f => f.CreateProfile("WezTerm (Default)", null, null, It.Is<System.Collections.Generic.List<string>>(l => l.Count == 0)))
            .Returns(new WeztermProfile { Label = "WezTerm (Default)" });
        _mockFactory.Setup(f => f.CreateProfile("PowerShell (WezTerm)", null, "local", It.Is<System.Collections.Generic.List<string>>(l => l.Contains("pwsh.exe"))))
            .Returns(new WeztermProfile { Label = "PowerShell (WezTerm)", Domain = "local", Args = ["pwsh.exe", "-NoLogo"] });
        _mockFactory.Setup(f => f.CreateProfile("Command Prompt (WezTerm)", null, "local", It.Is<System.Collections.Generic.List<string>>(l => l.Contains("cmd.exe"))))
            .Returns(new WeztermProfile { Label = "Command Prompt (WezTerm)", Domain = "local", Args = ["cmd.exe"] });

        // Act
        var profiles = await _provider.GetProfilesAsync();

        // Assert
        profiles.Should().HaveCount(3);
        profiles[0].Label.Should().Be("WezTerm (Default)");
        profiles[1].Label.Should().Be("PowerShell (WezTerm)");
        profiles[2].Label.Should().Be("Command Prompt (WezTerm)");
        _mockFactory.VerifyAll();
    }

    [TestMethod]
    public async Task GetProfilesAsync_WithValidConfigFile_ShouldParseProfiles()
    {
        // Arrange
        var luaContent = @"
local wezterm = require 'wezterm'
local config = {}

config.launch_menu = {
  {
    label = 'WSL Ubuntu',
    cwd = '/home/username',
    domain = 'wsl',
    args = { 'bash', '-l' }
  },
  {
    label = 'PowerShell',
    cwd = 'C:\\Users\\username',
    domain = { DomainName = 'local' },
    args = { 'pwsh.exe' }
  }
}

return config
";
        _tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(_tempFilePath, luaContent);
        Environment.SetEnvironmentVariable("WEZTERM_CONFIG_FILE", _tempFilePath);

        var profile1 = new WeztermProfile { Label = "WSL Ubuntu", Cwd = "/home/username", Domain = "wsl", Args = ["bash", "-l"] };
        var profile2 = new WeztermProfile { Label = "PowerShell", Cwd = @"C:\Users\username", Domain = "local", Args = ["pwsh.exe"] };

        _mockFactory.Setup(f => f.CreateProfile("WSL Ubuntu", "/home/username", "wsl", It.Is<System.Collections.Generic.List<string>>(l => l.Contains("bash") && l.Contains("-l"))))
            .Returns(profile1);
        _mockFactory.Setup(f => f.CreateProfile("PowerShell", @"C:\Users\username", "local", It.Is<System.Collections.Generic.List<string>>(l => l.Contains("pwsh.exe"))))
            .Returns(profile2);

        // Act
        var profiles = await _provider.GetProfilesAsync();

        // Assert
        profiles.Should().HaveCount(2);
        profiles[0].Label.Should().Be("WSL Ubuntu");
        profiles[0].Cwd.Should().Be("/home/username");
        profiles[0].Domain.Should().Be("wsl");
        profiles[0].Args.Should().ContainInOrder("bash", "-l");
        profiles[1].Label.Should().Be("PowerShell");
        profiles[1].Cwd.Should().Be(@"C:\Users\username");
        profiles[1].Domain.Should().Be("local");
        profiles[1].Args.Should().ContainInOrder("pwsh.exe");
        _mockFactory.VerifyAll();
    }

    [TestMethod]
    public async Task GetProfilesAsync_WithComments_ShouldStripCommentsAndParse()
    {
        // Arrange
        var luaContent = @"
config.launch_menu = {
  -- This is a comment
  {
    label = 'WSL Ubuntu', -- Inline comment
    domain = 'wsl' -- Another comment
  }
}
";
        _tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(_tempFilePath, luaContent);
        Environment.SetEnvironmentVariable("WEZTERM_CONFIG_FILE", _tempFilePath);

        var profile = new WeztermProfile { Label = "WSL Ubuntu", Domain = "wsl", Args = [] };

        _mockFactory.Setup(f => f.CreateProfile("WSL Ubuntu", null, "wsl", It.IsAny<System.Collections.Generic.List<string>>()))
            .Returns(profile);

        // Act
        var profiles = await _provider.GetProfilesAsync();

        // Assert
        profiles.Should().HaveCount(1);
        profiles[0].Label.Should().Be("WSL Ubuntu");
        profiles[0].Domain.Should().Be("wsl");
        _mockFactory.VerifyAll();
    }
}

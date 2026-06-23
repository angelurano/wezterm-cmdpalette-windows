using FluentAssertions;
using System;
using System.Collections.Generic;

namespace WeztermExtensionForCommandPalette.Tests;

[TestClass]
public class WeztermProfileFactoryTests
{
    private readonly WeztermProfileFactory _factory = new();

    [TestMethod]
    public void CreateProfile_WithValidParameters_ShouldReturnProfile()
    {
        // Arrange
        var label = "Wsl Ubuntu";
        var cwd = "/home/user";
        var domain = "wsl";
        var args = new List<string> { "bash" };

        // Act
        var profile = _factory.CreateProfile(label, cwd, domain, args);

        // Assert
        profile.Should().NotBeNull();
        profile.Label.Should().Be(label);
        profile.Cwd.Should().Be(cwd);
        profile.Domain.Should().Be(domain);
        profile.Args.Should().BeEquivalentTo(args);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void CreateProfile_WithInvalidLabel_ShouldThrowArgumentException(string? invalidLabel)
    {
        // Arrange
        var args = new List<string>();

        // Act & Assert
        Action act = () => _factory.CreateProfile(invalidLabel!, null, null, args);
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void CreateProfile_WithNullArgs_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => _factory.CreateProfile("Label", null, null, null!);
        act.Should().Throw<ArgumentNullException>();
    }
}


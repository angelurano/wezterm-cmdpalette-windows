# WezTerm Extension for Windows Command Palette

A personal Windows Command Palette extension that scans, parses, and launches custom [WezTerm](https://wezterm.org/) profiles directly from the configuration.


## Features

- **Profile Discovery**: Scans and locates WezTerm profiles from `.lua` configuration files.
- **Launch Terminal**: Quickly opens a new terminal window using the configuration of the selected profile.

### Planned Features
- **WezTerm CLI Integration**: Interact directly with WezTerm to create new tabs or utilize multiplexing features to attach to existing sessions directly from the Command Palette.


## Project Structure

- `WeztermExtensionForCommandPalette/`: The main extension application.
- `WeztermExtensionForCommandPalette.Tests/`: Unit testing suite.


## Development & Usage

### Prerequisites
- .NET 9 SDK
- Windows 10/11 with Windows Command Palette (from [Microsoft PowerToys](https://github.com/microsoft/PowerToys))
- [WezTerm](https://github.com/wezterm/wezterm)

### Commands

**Build the project:**
```powershell
dotnet build
```

**Run tests:**
```powershell
dotnet test -r win-x64
```

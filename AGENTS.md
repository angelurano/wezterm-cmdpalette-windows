# AGENTS.md

This file provides context, rules, and guidelines to help AI coding agents work efficiently on the **WezTerm Extension for Command Palette** project.

## Dev environment tips
- **Target Framework**: The extension targets `.NET 9` with Windows SDK compatibility (`net9.0-windows10.0.26100.0`).
- **Central Package Management (CPM)**: Enabled via `Directory.Packages.props` in the solution root. When adding or updating NuGet package references, define the version in `Directory.Packages.props` and omit the `Version` attribute inside individual `.csproj` files to prevent build errors (like NU1008).
- **Compilation**: Run `dotnet build` to compile the solution. Note that the main project has `<PublishSingleFile>true</PublishSingleFile>` enabled.

## Testing instructions
- **Test Project**: Unit tests are located in the `WeztermExtensionForCommandPalette.Tests` project.
- **Running Tests**: Run tests from the solution root using `dotnet test -r win-x64`. Specifying the runtime identifier (`-r win-x64` or similar) is required due to the `PublishSingleFile` configuration of the main project.
- **Environment and State Isolation**: Tests that modify environment variables (e.g. `WEZTERM_CONFIG_FILE` for parsing) must use the `[DoNotParallelize]` attribute on the test class to avoid parallel execution race conditions.

## Coding guidelines

### 1. Process Spawning
- **Security & Safety**: Always spawn external processes safely. Disable shell execution (`UseShellExecute = false`), set `CreateNoWindow = true` where applicable, and populate `ProcessStartInfo.ArgumentList` directly rather than constructing an arguments string manually. This avoids argument injection vulnerabilities and removes the need for custom escaping logic.

### 2. Architecture & Dependency Injection
- **DI Container**: The project uses standard Dependency Injection (`Microsoft.Extensions.DependencyInjection`) inside `Program.cs`. Do not introduce static helper patterns for key logic.
- **Constructor Injection**: Retrieve dependencies (such as providers and factories) using primary constructor syntax where possible.
- **Profile Creation**: Always instantiate `WeztermProfile` objects using the injected `IWeztermProfileFactory` interface.

### 3. Parsing & Performance
- **Lua Configuration Parsing**: The parser in `WeztermConfigProvider` must remain high-performance and Ahead-of-Time (AOT)/Trimming compatible:
  - Use Span-based string slicing (`ReadOnlySpan<char>`) to avoid unnecessary heap allocations.
  - Use Source-Generated Regular Expressions (`[GeneratedRegex]`) rather than dynamically compiled regexes.

### 4. Native Windows Interop (P/Invoke)
- **Modern P/Invoke**: Use the newer `[LibraryImport]` source generator instead of `[DllImport]` for any Win32 API calls.
- **Window Focus Delegation**: To bypass Windows window-activation restrictions when launching WezTerm from an out-of-process COM server, delegate focus permission using the Win32 `AllowSetForegroundWindow` API on an STA thread.

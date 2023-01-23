# AutoChoco
C# Program which Automates Chocolatey Installation, Uninstallation, Backup, and more.

# Usage
**Installing Chocolatey:**

`-i` or `--install`

**Uninstalling Chocolatey:**

`-u` or `--uninstall`

**Install Essential Programs:**

`-e` or `--essentials`

**Back up Chocolatey:**

`-b` or `--backup`

**Restore from a backup:**

`-r` or `--backup-restore`


# List of Essential Programs:
## Browsers
- Firefox
- Chromium
## Media
- VLC
- MPV.NET
- OBS Studio
- FFmpeg
- YT-DLP
## Development
- Visual Studio Code
- Neovim
- Dotnet Runtime
- Dotnet Framework 4.8
- DotNet Framework 3.5
- Python 3.10
- Git
- Less
## Other
- 7zip
- OnlyOffice

# How do I run AutoChocolatey?
You can run the program using `dotnet run -- <option>`, by using [dotnet publish](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish) or you can also download a compiled binary from [releases](https://github.com/DontEatOreo/AutoChoco/releases)

# Nuget Packages
```
CliWrap
Pastel
System.CommandLine
```
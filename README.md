# AutoChoco
C# Program which Automates Installation, Uninstallation, Backup, Backup Restore and Essential Package Installation for Chocolatey

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
- 7Zip
- Chromium
- DotNet 3.5
- DotNet Runtime
- FFmpeg
- Firefox
- MPV.Net
- OBS
- OnlyOffice
- VLC
- VSCode
- yt-dlp

# How do I run AutoChocolatey?
You can run the program using `dotnet run -- <option>` or by using [dotnet publish](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish).
You can also download a compiled binary from [releases](https://github.com/DontEatOreo/AutoChoco/releases)

# Nuget Packages
```
System.CommandLine
```
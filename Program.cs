using System.CommandLine;
using System.CommandLine.Invocation;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using Pastel;

var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
var chocolateyPath = Path.Combine(programFiles, "chocolatey");
var chocolateyBackupPath = Path.Combine(programFiles, "chocolatey-backup");
var restorePackagesPath = Path.Combine(chocolateyBackupPath, "packages.txt");

string[] defaultEssentialPrograms = {
    // Browsers
    "firefox",
    "chromium",
    // Media
    "vlc",
    "mpv.net",
    "obs",
    "ffmpeg",
    "yt-dlp",
    // Development
    "vscode",
    "neovim",
    "dotnet-runtime",
    "dotnetfx",
    "dotnet3.5",
    "python310",
    "git",
    "less",
    // Other
    "7zip",
    "onlyoffice",
};

RootCommand rootCommand = new();

Option<bool> installOption = new(new[] { "-i", "--install" }, "Install Chocolatey");
Option<bool> uninstallOption = new(new[] { "-u", "--uninstall" }, "Uninstall Chocolatey");
Option<bool> essentialOption = new(new[] { "-e", "--essentials" }, "Install essential packages");
Option<bool> backupOption = new(new[] { "-b", "--backup" }, "Make a backup");
backupOption.AddValidator(_ =>
{
    if (!Directory.Exists(chocolateyBackupPath))
        return;
    Console.Write($"{"There is already a backup".Pastel(ConsoleColor.Red)}");
    Environment.Exit(1);
});
Option<bool> restoreOption = new(new[] { "-r", "--backup-restore" }, "Restore a backup");

var options = new[] { installOption, uninstallOption, essentialOption, backupOption, restoreOption };
foreach (var option in options)
    rootCommand.AddOption(option);

if (string.IsNullOrEmpty(args.ToString()))
    args = new[] { "-h" };

rootCommand.SetHandler(ChocoHandler);

async Task ChocoHandler(InvocationContext invocationContext)
{
    var install = invocationContext.ParseResult.GetValueForOption(installOption);
    var uninstall = invocationContext.ParseResult.GetValueForOption(uninstallOption);
    var essential = invocationContext.ParseResult.GetValueForOption(essentialOption);
    var backup = invocationContext.ParseResult.GetValueForOption(backupOption);
    var restore = invocationContext.ParseResult.GetValueForOption(restoreOption);

    var isAdmin = await CheckAdmin();
    if (!isAdmin)
        Environment.Exit(1);

    if (install) await InstallChoco();
    else if (uninstall) await UninstallChoco();
    else if (essential) await InstallEssentials();
    else if (backup) await BackUpChoco();
    else if (restore) await RestoreChoco();
}

Task<bool> CheckAdmin()
{
#pragma warning disable CA1416
    var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    if (principal.IsInRole(WindowsBuiltInRole.Administrator))
        return Task.FromResult(true);
#pragma warning restore CA1416

    Console.Error.WriteLine($"{"You need to run this program as administrator".Pastel(ConsoleColor.Red)}");
    return Task.FromResult(false);
}

async Task InstallChoco()
{
    const string installArgs =
        @"Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))";
    await Cli.Wrap("powershell")
        .WithArguments(installArgs)
        .WithValidation(CommandResultValidation.None)
        .ExecuteAsync();

    Console.WriteLine($"{"Chocolatey has been installed".Pastel(ConsoleColor.Green)}");

    Console.WriteLine("Do you want to install the essential packages? (y/n)");
    var key = Console.ReadKey();
    if (key.Key is ConsoleKey.Y)
        await InstallEssentials();
}

async Task InstallEssentials()
{
    await Cli.Wrap("choco")
        .WithArguments($"{string.Join(' ', defaultEssentialPrograms)} -y")
        .WithValidation(CommandResultValidation.ZeroExitCode)
        .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
        .WithStandardErrorPipe(PipeTarget.ToDelegate(line => Console.Error.WriteLine(line.Pastel(ConsoleColor.Red))))
        .ExecuteAsync();

    Console.WriteLine($"{"The Essential packages have been installed".Pastel(ConsoleColor.Green)}");

    Console.WriteLine("Do you want to install additional packages? (y/n)");
    var key = Console.ReadKey();
    if (key.Key is not ConsoleKey.Y)
        return;

    Console.WriteLine("Enter the packages you want to install separated by a space");
    var packages = Console.ReadLine()?.Split(' ');
    if (packages is null)
    {
        Console.Error.WriteLine($"{"No packages were entered".Pastel(ConsoleColor.Red)}");
        return;
    }

    await Cli.Wrap("choco")
        .WithArguments($"{packages} -y")
        .WithValidation(CommandResultValidation.None)
        .WithStandardErrorPipe(PipeTarget.ToDelegate(line => Console.Error.WriteLine(line.Pastel(ConsoleColor.Red))))
        .ExecuteAsync();
}

async Task UninstallChoco()
{
    await Cli.Wrap("choco")
        .WithArguments("uninstall all -y")
        .WithValidation(CommandResultValidation.None)
        .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
        .ExecuteAsync();

    if (Directory.Exists(chocolateyPath))
        Directory.Delete(chocolateyPath, true);

    Console.WriteLine(Directory.Exists(chocolateyPath)
        ? $"{"Chocolatey is not installed.".Pastel(ConsoleColor.Red)}"
        : $"{"Chocolatey has been removed.".Pastel(ConsoleColor.Green)}");
}

async Task BackUpChoco()
{
    if (!Directory.Exists(chocolateyPath))
    {
        Console.WriteLine($"{"Chocolatey is not installed.".Pastel(ConsoleColor.Red)}");
        return;
    }

    if (Directory.Exists(chocolateyBackupPath))
    {
        Console.WriteLine($"{"There is already a backup.".Pastel(ConsoleColor.Yellow)}");
        return;
    }

    Directory.CreateDirectory(chocolateyBackupPath);

    var files = Directory.GetFiles(chocolateyPath, "*", SearchOption.AllDirectories);
    foreach (var file in files)
    {
        var newFile = file.Replace(chocolateyPath, chocolateyBackupPath);
        Directory.CreateDirectory(Path.GetDirectoryName(newFile) ?? string.Empty);
        File.Copy(file, newFile, true);
    }

    StringBuilder packages = new();
    await Cli.Wrap("choco")
        .WithArguments("list --local-only")
        .WithStandardOutputPipe(PipeTarget.ToStringBuilder(packages))
        .ExecuteAsync();

    var lines = packages.ToString().Split(Environment.NewLine);
    var start = Array.FindIndex(lines, line => line.StartsWith("Chocolatey v"));
    var end = Array.FindIndex(lines, line => line.EndsWith("packages installed."));
    var packagesList = lines[start..end];
    packagesList = packagesList[2..];
    var regex = new Regex(@"\s");
    packagesList = packagesList.Select(line => regex.Split(line)[0]).ToArray();
    Console.WriteLine(string.Join(" ", packagesList));
    File.WriteAllText(restorePackagesPath, string.Join(" ", packagesList));

    Console.WriteLine($"{"Backup complete.".Pastel(ConsoleColor.Green)}");
}

async Task RestoreChoco()
{
    if (!Directory.Exists(chocolateyBackupPath))
    {
        Console.Error.WriteLine($"{"No backup found.".Pastel(ConsoleColor.Red)}");
        return;
    }

    var files = Directory.GetFiles(chocolateyBackupPath, "*", SearchOption.AllDirectories);
    var directories = Directory.GetDirectories(chocolateyBackupPath, "*", SearchOption.AllDirectories);
    Console.WriteLine($"{"Restoring Directory Structure...".Pastel(ConsoleColor.Gray)}");
    foreach (var directory in directories)
    {
        if (Directory.Exists(directory))
            continue;
        var newDirectory = directory.Replace(chocolateyBackupPath, chocolateyPath);
        Directory.CreateDirectory(newDirectory);
    }
    Console.WriteLine($"{"Restoring Files...".Pastel(ConsoleColor.Green)}");
    foreach (var file in files)
    {
        if (File.Exists(file))
            continue;
        var newFile = file.Replace(chocolateyBackupPath, chocolateyPath);
        File.Move(file, newFile);
    }

    if (File.Exists(restorePackagesPath))
    {
        var packages = File.ReadAllText(restorePackagesPath);
        packages = packages.Replace(Environment.NewLine, " ");
        Console.WriteLine($"{"Restoring Packages...".Pastel(ConsoleColor.Green)}");
        await Cli.Wrap("choco")
            .WithArguments($"install {packages} -y")
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();
    }

    Console.WriteLine($"{"Backup restored.".Pastel(ConsoleColor.Green)}");

    Console.WriteLine("Do you want to delete the backup? (y/n)");
    var key = Console.ReadKey();
    if (key.Key is not ConsoleKey.Y)
        return;
    Directory.Delete(chocolateyBackupPath, true);
    Console.WriteLine($"{"Backup deleted.".Pastel(ConsoleColor.Green)}");
}

await rootCommand.InvokeAsync(args);
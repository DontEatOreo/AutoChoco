using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Security.Principal;

RootCommand rootCommand = new();

Option<bool> installOption = new(new[] { "-i", "--install" }, "Install Chocolatey");
Option<bool> uninstallOption = new(new[] { "-u", "--uninstall" }, "Uninstall Chocolatey");
Option<bool> essentialOption = new(new[] { "-e", "--essentials" }, "Install essential packages");
Option<bool> backupOption = new(new[] { "-b", "--backup" }, "Make a backup");
Option<bool> restoreOption = new(new[] { "-r", "--backup-restore" }, "Restore a backup");

foreach(var option in new[] { installOption, uninstallOption, essentialOption, backupOption, restoreOption }) 
    rootCommand.AddOption(option);

rootCommand.SetHandler(async context => await ChocoHandler(context));

await rootCommand.InvokeAsync(args);

async Task ChocoHandler(InvocationContext invocationContext)
{
    #pragma warning disable CA1416
    if(!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
    #pragma warning restore CA1416
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("You need to run this program as administrator");
        Console.ResetColor();
        return;
    }
    
    var install = invocationContext.ParseResult.GetValueForOption(installOption);
    var uninstall = invocationContext.ParseResult.GetValueForOption(uninstallOption);
    var essential = invocationContext.ParseResult.GetValueForOption(essentialOption);
    var backup = invocationContext.ParseResult.GetValueForOption(backupOption);
    var restore = invocationContext.ParseResult.GetValueForOption(restoreOption);

    if (install) await InstallChoco();
    else if (uninstall) await UninstallChoco();
    else if (essential) await InstallEssentials();
    else if (backup) await BackUpChoco();
    else if (restore) await RestoreChoco();
}

async Task InstallChoco()
{
    if(Directory.Exists(@"C:\ProgramData\chocolatey"))
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Chocolatey is already installed");
        Console.ResetColor();
        return;
    }
    
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = @"powershell.exe",
            Arguments =
                @"Set-ExecutionPolicy Bypass -Scope Process -Force; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = false
        }
    };
    process.Start();
    var output = await process.StandardOutput.ReadToEndAsync();
    Console.WriteLine(output);
}

async Task InstallEssentials()
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
            Arguments =
                @"choco install 7zip chromium dotnet-runtime dotnet3.5 ffmpeg firefox mpv.net obs onlyoffice vlc vscode yt-dlp -y",
            RedirectStandardOutput = true,
        }
    };
    process.Start();
    var output = await process.StandardOutput.ReadToEndAsync();
    Console.WriteLine(output);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("The Essential packages have been installed");
    Console.ResetColor();
}

Task UninstallChoco()
{
    if (Directory.Exists(@"C:\ProgramData\chocolatey\")) Directory.Delete(@"C:\ProgramData\chocolatey\", true);
    Console.ForegroundColor = Directory.Exists(@"C:\ProgramData\chocolatey\") ? ConsoleColor.Red : ConsoleColor.Green;
    Console.WriteLine(Directory.Exists(@"C:\ProgramData\chocolatey\") ? "Chocolatey is not installed." : "Chocolatey has been removed.");
    Console.ResetColor();
    return Task.CompletedTask;
}

Task BackUpChoco()
{
    if (!Directory.Exists(@"C:\ProgramData\chocolatey\"))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Chocolatey is not installed.");
        Console.ResetColor();
        return Task.CompletedTask;
    }
    
    // check if there is an existing backup
    if (Directory.Exists(@"C:\ProgramData\chocolatey-backup\"))
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("There is already a backup.");
        Console.ResetColor();
        return Task.CompletedTask;
    }
    
    Directory.CreateDirectory(@"C:\ProgramData\chocolatey-backup\");
    
    var files = Directory.GetFiles(@"C:\ProgramData\chocolatey\", "*", SearchOption.AllDirectories);
    foreach (var file in files)
    {
        var newFile = file.Replace(@"C:\ProgramData\chocolatey\", @"C:\ProgramData\chocolatey-backup\");
        Directory.CreateDirectory(Path.GetDirectoryName(newFile) ?? string.Empty);
        File.Copy(file, newFile, true);
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Backup complete.");
    Console.ResetColor();
    return Task.CompletedTask;
}

Task RestoreChoco()
{
    if (!Directory.Exists(@"C:\ProgramData\chocolatey-backup\"))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("No backup found.");
        Console.ResetColor();
        return Task.CompletedTask;
    }
    
    // copy files from chocolatey-backup to chocolatey
    var files = Directory.GetFiles(@"C:\ProgramData\chocolatey-backup\", "*", SearchOption.AllDirectories);
    foreach (var file in files)
    {
        var newFile = file.Replace(@"C:\ProgramData\chocolatey-backup\", @"C:\ProgramData\chocolatey\");
        Directory.CreateDirectory(Path.GetDirectoryName(newFile) ?? string.Empty);
        File.Copy(file, newFile, true);
    }
    
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Backup restored complete.");
    Console.ResetColor();
    
    // Ask if a user wants to delete the backup
    Console.WriteLine("Do you want to delete the backup? (y/n)");
    var input = Console.ReadLine();
    if (input == "y") Directory.Delete(@"C:\ProgramData\chocolatey-backup\", true);
    Console.ForegroundColor = input == "y" ? ConsoleColor.Green : ConsoleColor.Yellow;
    Console.WriteLine(input == "y" ? "Backup deleted." : "Backup not deleted.");
    Console.ResetColor();
    
    return Task.CompletedTask;
}
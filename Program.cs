namespace AutoChoco
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Security.Principal;
    using System.Threading;

    internal static class Program
    {
        private static async Task Main() => await ChocoTasks.ChocoCaller();
    }
    internal static class ChocoTasks
    {
        public static async Task ChocoCaller()
        {
            if (Environment.GetCommandLineArgs().Length == 1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("You need to run the program with arguments");
                Console.ForegroundColor = ConsoleColor.Gray;
                Thread.Sleep(2500);
                Environment.Exit(0);
            }
            await ChocoInstallTask();
            await ChocoInstallEssential();
            await ChocoRemoveInstallation();
            await ChocoBackUpTask();
            await ChocoRestoreBackup();
            await ChocoHelpTask();
        }
        private static async Task ChocoAdminCheck()
        {
            if (Environment.UserInteractive)
            {
                if (!await Task.Run(() =>
#pragma warning disable CA1416
                    new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)))
#pragma warning restore CA1416
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("This program needs to be run as administrator");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Thread.Sleep(5000);
                    Environment.Exit(1);
                }
            }
            await Task.Delay(0);
        }
        private static async Task ChocoInstallTask()
        {
            await ChocoAdminCheck();
            if (Environment.GetCommandLineArgs().Contains("-i") ||
                Environment.GetCommandLineArgs().Contains("--install"))
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
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
        }
        private static async Task ChocoInstallEssential()
        {
            await ChocoAdminCheck();
            if (Environment.GetCommandLineArgs().Contains("-e") ||
                Environment.GetCommandLineArgs().Contains("--essential"))
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
                        Arguments =
                            @"choco install firefox chromium brave onlyoffice ffmpeg obs vlc 7zip openjdk dotnet-5.0-runtime dotnet-runtime notepadplusplus vscode -y",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = false
                    }
                };
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                Console.WriteLine(output);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("The Essential packages have been installed");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
        private static async Task ChocoRemoveInstallation()
        {
            if (Environment.GetCommandLineArgs().Contains("-r") ||
                Environment.GetCommandLineArgs().Contains("--remove"))
            {
                if (Directory.Exists(@"C:\ProgramData\chocolatey\"))
                {
                    Directory.Delete(@"C:\ProgramData\chocolatey\", true);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Chocolatey has been removed.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Chocolatey is not installed.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
            await Task.Delay(0);
        }
        private static async Task ChocoBackUpTask()
        {
            if (Environment.GetCommandLineArgs().Contains("-b") ||
                Environment.GetCommandLineArgs().Contains("--backup"))
            {
                if (Directory.Exists(@"C:\ProgramData\chocolatey\"))
                {
                    Directory.CreateDirectory(@"C:\ProgramData\backup\chocolatey\");
                    var files = Directory.GetFiles(@"C:\ProgramData\chocolatey\");
                    foreach (var file in files)
                    {
                        File.Copy(file, @"C:\ProgramData\backup\chocolatey\" + Path.GetFileName(file), true);
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Backup complete.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Chocolatey is not installed.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
            await Task.Delay(0);
        }

        private static async Task ChocoRestoreBackup()
        {
            if (Environment.GetCommandLineArgs().Contains("-br") ||
                Environment.GetCommandLineArgs().Contains("--backup-restore"))
            {
                if (Directory.Exists(@"C:\ProgramData\backup\chocolatey\"))
                {
                    var files = Directory.GetFiles(@"C:\ProgramData\backup\chocolatey\");
                    foreach (var file in files)
                    {
                        File.Copy(file, @"C:\ProgramData\chocolatey\" + Path.GetFileName(file), true);
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Backup restored complete.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Chocolatey is not installed.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                await Task.Delay(0);
            }
        }

        private static async Task ChocoHelpTask()
        {
            if (Environment.GetCommandLineArgs().Contains("-h") ||
                Environment.GetCommandLineArgs().Contains("--help"))
            {
                Console.WriteLine("This program will install Chocolatey on your computer.\n" +
                                  "To install Chocolatey, run the program with the argument -i or --install\n" +
                                  "To remove Chocolatey, run the program with the argument -r or --remove\n" +
                                  "To see this help text, run the program with the argument -h or --help\n" +
                                  "To install Essential packages, run the program with the argument -e or --essential\n" +
                                  "To make a backup, run the program with the argument -b or --backup\n" +
                                  "To restore a backup, run the program with the argument -br or --backup-restore");
            }
            await Task.Delay(0);
        }
    }
}

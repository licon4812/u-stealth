using System;
using System.Collections.Generic;
using System.Diagnostics;
using Spectre.Console;
using System.Text.Json.Serialization;
using System.Security.Principal;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("UStealth.Tests")]
namespace UStealth.DriveHelper
{
    internal class Program
    {
        static int Main(string[] args)
        {
            // Elevation check at the very beginning of interactive mode
            if (args.Length < 1)
            {
#if !DEBUG
                if (!IsRunningAsAdministrator())
                {
                    AnsiConsole.MarkupLine("[yellow]This tool requires administrator privileges. Relaunching with elevation...[/]");
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = Environment.ProcessPath ?? Environment.ProcessPath,
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                        Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Failed to elevate: {ex.Message}[/]");
                    }
                    return 123; // Exit current process
                }
#endif

                while (true)
                {
                    // Interactive Spectre.Console menu
                    var command = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[green]Select a command[/]")
                            .AddChoices("list drives","hide / unhide","read boot", "help", "exit")
                    );

                    if (command == "exit")
                        break;

                    string device = null;
                    string hexData = null;

                    if (command is "hide / unhide" or "read boot")
                    {
                        // List drives and let user select
                        var drives = GetDrivesForPrompt();
                        if (drives.Count == 0)
                        {
                            AnsiConsole.MarkupLine("[red]No drives found.[/]");
                            continue;
                        }
                        device = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("[green]Select a device[/]")
                                .AddChoices(drives)
                        );
                        // Extract deviceId from label
                        device = device.Split(' ')[0];
                    }

                    // Call the appropriate method
                    int result = 0;
                    switch (command)
                    {
                        case "hide / unhide":
                            result = ToggleBoot(device);
                            break;
                        case "read boot":
                            result = ReadBoot(device);
                            break;
                        case "help":
                            Help();
                            continue; // Skip the "Command finished" message
                        case "list drives":
                            // Interactive Spectre.Console table view
                            var drives = new List<DriveInfoDisplay>();
#if WINDOWS
                            drives = Windows.GetDrives();
#else
                            throw new NotImplementedException();
#endif
                            try
                            {
                                // Print as Spectre.Console table
                                var table = new Table();
                                table.AddColumn("SystemDrive");
                                table.AddColumn("DeviceID");
                                table.AddColumn("Model");
                                table.AddColumn("Interface");
                                table.AddColumn("MediaType");
                                table.AddColumn("Size");
                                table.AddColumn("VolumeLabel");
                                table.AddColumn("Format");
                                table.AddColumn("DriveLetter");
                                table.AddColumn("Status");
                                foreach (var d in drives)
                                {
                                    table.AddRow(
                                        d.IsSystemDrive.ToString() ?? "",
                                        d.DeviceID ?? "",
                                        d.Model ?? "",
                                        d.Interface ?? "",
                                        d.MediaType ?? "",
                                        FormatSize(d.Size),
                                        d.VolumeLabel ?? "",
                                        d.Format ?? "",
                                        d.DriveLetter ?? "",
                                        d.Status ?? ""
                                    );
                                }
                                AnsiConsole.Write(table);
                                // skip the rest of the loop
                                continue;
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                                continue;
                            }
                        default:
                            AnsiConsole.MarkupLine($"[red]Unknown command: {command}[/]");
                            continue;
                    }
                    AnsiConsole.MarkupLine($"[grey]Command finished with code {result}[/]");
                    if (!AnsiConsole.Confirm("Do you want to perform another action?", true))
                        break;
                }
                return 0;
            }

            string commandArg = args[0].ToLowerInvariant();
            string deviceArg = args.Length > 1 ? args[1] : null;

            try
            {
                switch (commandArg)
                {
                    case "toggleboot":
                        return ToggleBoot(deviceArg);
                    case "readboot":
                        return ReadBoot(deviceArg);
                    case "listdrives":
                        return ListDrives();
                    case "help":
                        Help();
                        return 0;
                    default:
                        Console.Error.WriteLine($"Unknown command: {commandArg}");
                        return 102;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 110;
            }
        }

        private static int ListDrives()
        {
#if WINDOWS
            return Windows.ListDrives();
#else
            throw new NotImplementedException();
#endif
        }

        private static int ReadBoot(string? deviceArg)
        {
#if WINDOWS
            return Windows.ReadBoot(deviceArg);
#else
            throw new NotImplementedException();
#endif
        }

        private static int ToggleBoot(string? device)
        {
#if WINDOWS
            return Windows.ToggleBoot(device);
#else
            throw new NotImplementedException();
#endif
        }

        private static List<string> GetDrivesForPrompt()
        {
#if WINDOWS
            return Windows.GetDrivesForPrompt();
#else
            throw new NotImplementedException();
#endif
        }

        // Helper to check for admin rights
        private static bool IsRunningAsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void Help()
        {
            AnsiConsole.MarkupLine("[blue]UStealth Drive Helper[/]");
            AnsiConsole.MarkupLine("A command-line tool to manage drive boot sectors.");
            AnsiConsole.MarkupLine("Commands:");
            AnsiConsole.MarkupLine(" - [green]list drives[/]: List all connected drives with details.");
            AnsiConsole.MarkupLine(" - [green]hide / unhide[/]: Toggle the boot sector signature of a selected drive.");
            AnsiConsole.MarkupLine(" - [green]read boot[/]: Read and display the boot sector of a selected drive in hex format.");
            AnsiConsole.MarkupLine(" - [green]help[/]: Show this help information.");
            AnsiConsole.MarkupLine(" - [green]exit[/]: Exit the application.");
        }


        public static string FormatSize(string sizeStr)
        {
            if (!long.TryParse(sizeStr, out long size)) return sizeStr;
            return size switch
            {
                > 999999999999 => $"{size / 1_000_000_000_000.0:F1} TB",
                > 999999999 => $"{size / 1_000_000_000.0:F1} GB",
                > 999999 => $"{size / 1_000_000.0:F1} MB",
                > 999 => $"{size / 1_000.0:F1} KB",
                _ => size.ToString()
            };
        }


        // Replace the use of 'dynamic' with a strongly-typed class for drive info
        // Make DriveInfoDisplay public so it can be used by the source generator
        public class DriveInfoDisplay
        {
            public bool IsSystemDrive { get; set; }
            public string DeviceID { get; set; }
            public string Model { get; set; }
            public string Interface { get; set; }
            public string MediaType { get; set; }
            public string Size { get; set; }
            public string VolumeLabel { get; set; }
            public string Format { get; set; }
            public string DriveLetter { get; set; }
            public string Status { get; set; }
        }
    }

    [JsonSerializable(typeof(List<Program.DriveInfoDisplay>))]
    internal partial class DriveInfoDisplayJsonContext : JsonSerializerContext { }
}

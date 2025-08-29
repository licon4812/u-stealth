using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Mime;
using Spectre.Console;
using System.Text.Json.Serialization;


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
#if WINDOWS
                    AnsiConsole.MarkupLine("[yellow]This tool requires administrator privileges. Relaunching with elevation...[/]");
                    Windows.ElevateToAdministrator();
#else
                    if (Linux.IsCliMode())
                    {
                        AnsiConsole.MarkupLine("[yellow]This tool requires administrator privileges[/]");
                        AnsiConsole.MarkupLine("[red]please rerun this process as an administrator or root[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[yellow]This tool requires administrator privileges. Relaunching with elevation...[/]");
                        Linux.ElevateToAdministrator();
                    }
#endif
                    return 123; // Exit current process
                }
#endif

                while (true)
                {

                    // Interactive Spectre.Console menu
                    AnsiConsole.MarkupLine($"[blue]Welcome to U-Stealth CLI v{GetApplicationVersion()}[/]");
                    var command = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[green]Select a command[/]")
                            .AddChoices("list drives", "hide / unhide", "read boot", "version" , "help", "exit")
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
                        case "version":
                            AnsiConsole.MarkupLine($"[green] U-Stealth CLI v{GetApplicationVersion()}[/]");
                            result = 0;
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
                            drives = Linux.GetDrives();
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
                                        Markup.Escape(d.IsSystemDrive.ToString() ?? ""),
                                        Markup.Escape(d.DeviceID ?? ""),
                                        Markup.Escape(d.Model ?? ""),
                                        Markup.Escape(d.Interface ?? ""),
                                        Markup.Escape(d.MediaType ?? ""),
                                        Markup.Escape(FormatSize(d.Size)),
                                        Markup.Escape(d.VolumeLabel ?? ""),
                                        Markup.Escape(d.Format ?? ""),
                                        Markup.Escape(d.DriveLetter ?? ""),
                                        Markup.Escape(d.Status ?? "")
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
                    case "version":
                        AnsiConsole.MarkupLine($"[green] U-Stealth CLI v{GetApplicationVersion()}[/]");
                        return 0;
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

        /// <summary>
        /// Command to list drives in JSON format
        /// </summary>
        /// <returns></returns>
        private static int ListDrives()
        {
#if WINDOWS
            return Windows.ListDrives();
#else
            return Linux.ListDrives();
#endif
        }

        /// <summary>
        /// Command to read and display the boot sector in hex format
        /// </summary>
        /// <param name="deviceArg"></param>
        /// <returns></returns>
        private static int ReadBoot(string? deviceArg)
        {
#if WINDOWS
            return Windows.ReadBoot(deviceArg);
#else
            return Linux.ReadBoot(deviceArg);
#endif
        }

        /// <summary>
        /// Command to toggle the boot sector signature
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private static int ToggleBoot(string? device)
        {
#if WINDOWS
            return Windows.ToggleBoot(device);
#else
            return Linux.ToggleBoot(device);
#endif
        }

        /// <summary>
        /// Helper to get drives for Spectre.Console prompt
        /// </summary>
        /// <returns></returns>
        private static List<string> GetDrivesForPrompt()
        {
#if WINDOWS
            return Windows.GetDrivesForPrompt();
#else
            return Linux.GetDrivesForPrompt();
#endif
        }

        /// <summary>
        /// Helper to check if running as administrator
        /// </summary>
        /// <returns></returns>
        private static bool IsRunningAsAdministrator()
        {
#if WINDOWS
            return Windows.IsRunningAdministrator();
#else
            return Linux.IsRunningAdministrator();
#endif
        }

        private static void Help()
        {
            AnsiConsole.MarkupLine("[blue]UStealth Drive Helper[/]");
            AnsiConsole.MarkupLine("A command-line tool to manage drive boot sectors.");
            AnsiConsole.MarkupLine("Commands:");
            AnsiConsole.MarkupLine(" - [green]list drives[/]: List all connected drives with details.");
            AnsiConsole.MarkupLine(" - [green]hide / unhide[/]: Toggle the boot sector signature of a selected drive.");
            AnsiConsole.MarkupLine(" - [green]read boot[/]: Read and display the boot sector of a selected drive in hex format.");
            AnsiConsole.MarkupLine(" - [green]version[/]: Display the application version.");
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

        public static string GetApplicationVersion()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? version.ToString() : "unknown";
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

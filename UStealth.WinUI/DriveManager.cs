using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UStealth.WinUI
{
    public class DriveManager
    {
        private static string HelperPath => Path.Combine(AppContext.BaseDirectory, "UStealth.DriveHelper.exe");

        public List<Models.DriveInfoModel> GetDriveList()
        {
            var drives = new List<Models.DriveInfoModel>();
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = HelperPath,
                    Arguments = "listdrives",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Unable to start drive helper: {HelperPath}");
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                int exitCode = process.ExitCode;
                if (exitCode != 0)
                    throw new InvalidOperationException($"Drive helper exited with code {exitCode}: {error.Trim()}");

                if (!string.IsNullOrWhiteSpace(output))
                {
                    var driveList = JsonSerializer.Deserialize(output, DriveInfoModelJsonContext.Default.ListDriveInfoModel);
                    if (driveList != null)
                        drives.AddRange(driveList);
                }
                // Now get boot status for each drive
                foreach (var drive in drives)
                {
                    drive.Status = GetBootStatusFromHelper(drive.DeviceID);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load drives: {ex}");
                throw;
            }
            return drives;
        }

        private string GetBootStatusFromHelper(string device)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = HelperPath,
                    Arguments = $"readboot \"{device}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Unable to start drive helper: {HelperPath}");
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                int exitCode = process.ExitCode;
                output = output?.Trim();
                if (exitCode == 0 && !string.IsNullOrWhiteSpace(output) && output.Length >= 1024)
                {
                    // output is hex string, last two bytes are 510, 511
                    // 510 = AA, 511 = AB or AA
                    string lastByte = output.Substring(output.Length - 2, 2);
                    if (lastByte.Equals("AA", StringComparison.OrdinalIgnoreCase))
                        return "NORMAL";
                    if (lastByte.Equals("AB", StringComparison.OrdinalIgnoreCase))
                        return "HIDDEN";
                    return "*UNKNOWN*";
                }
            }
            catch { }
            return "*UNKNOWN*";
        }
    }

    // Add a partial context class for source generation
    [JsonSerializable(typeof(List<Models.DriveInfoModel>))]
    internal partial class DriveInfoModelJsonContext : JsonSerializerContext
    {
    }
}
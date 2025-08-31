using TUnit;
using UStealth.DriveHelper;
using System.Reflection;
using System.Text.Json;
using System.Collections.Generic;

namespace UStealth.Tests
{
    public class DriveHelperTests
    {
        [Test]
        public void Basic()
        {
            Console.WriteLine("This is a basic test");
        }

        [Test]
        [MethodDataSource(nameof(FormatSizesData))]
        public async Task FormatSize_ReturnsCorrectlyFormattedString(string actual, string expected)
        {
            await Assert.That(actual).IsEqualTo(Program.FormatSize(expected));
        }

        [Test]
        public async Task ListDrives_IntegrationTest()
        {
            var drives = InvokeListDrives();
            await Assert.That(drives).IsNotNull();
            await Assert.That(drives.Count).IsGreaterThan(0);
            foreach (var d in drives)
            {
                Console.WriteLine($"Drive: {d.DeviceID}, Model: {d.Model}, Status: {d.Status}");
            }
        }

        [Test]
        public async Task CannotToggleSystemDrive()
        {
            var drives = InvokeListDrives();
            var systemDrive = drives.Find(d => d.IsSystemDrive);
            await Assert.That(systemDrive).IsNotNull();
            // Simulate the logic that would prevent toggling the system drive
            var canToggle = systemDrive is { IsSystemDrive: false };
            await Assert.That(canToggle).IsFalse();
            // Attempt to toggle and expect a failure code (non-zero)
            if (systemDrive != null)
            {
                var result = InvokeToggle(systemDrive.DeviceID);
                await Assert.That(result).IsEqualTo(4);
            }
        }

        private List<Program.DriveInfoDisplay> InvokeListDrives()
        {
            var method = typeof(Program).GetMethod("ListDrives", BindingFlags.Static | BindingFlags.NonPublic);
            var originalOut = Console.Out;
            try
            {
                using var sw = new System.IO.StringWriter();
                Console.SetOut(sw);
                method.Invoke(null, null);
                Console.Out.Flush();
                var output = sw.ToString();
                // Find the JSON output (last line)
                var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                string json = lines.Length > 0 ? lines[^1] : string.Empty;
                if (string.IsNullOrWhiteSpace(json)) return [];
                var drives = JsonSerializer.Deserialize<List<Program.DriveInfoDisplay>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return drives ?? [];
            }
            catch(Exception exception)
            {
                Console.SetOut(originalOut);
                Console.WriteLine(exception.Message);
                throw;
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        private int InvokeToggle(string device)
        {
            var method = typeof(Program).GetMethod("ToggleBoot", BindingFlags.Static | BindingFlags.NonPublic);
            return (int)method.Invoke(null, [device]);
        }

        public static IEnumerable<Func<(string, string)>> FormatSizesData()
        {
            yield return () => ("1.0 TB", 1_000_000_000_000L.ToString());
            yield return () => ("1.0 GB", 1_000_000_000L.ToString());
            yield return () => ("1.0 MB", 1_000_000L.ToString());
            yield return () => ("1.0 KB", 1_000L.ToString());
            yield return () => ("999", "999");
            yield return () => ("not a number", "not a number");
        }
    }
}

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
            var drives = Program.GetDrives();
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
            var drives = Program.GetDrives().ToList();
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

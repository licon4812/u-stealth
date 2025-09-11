using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UStealth.DriveHelper.Models
{
    public partial class DriveInfoDisplay
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

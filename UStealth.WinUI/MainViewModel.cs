using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace UStealth.WinUI
{
    public partial class MainViewModel : INotifyPropertyChanged
    {
        private readonly DriveManager _driveManager = new();
        public ObservableCollection<DriveInfoModel> Drives { get; } = new();
        private DriveInfoModel _selectedDrive;
        public DriveInfoModel SelectedDrive
        {
            get => _selectedDrive;
            set { _selectedDrive = value; OnPropertyChanged(); }
        }

        public MainViewModel()
        {
            LoadDrives();
        }

        public void LoadDrives()
        {
            Drives.Clear();
            var drives = _driveManager.GetDriveList();
            if (drives != null)
            {
                foreach (var d in drives.OrderBy(x => x.DriveLetter))
                    Drives.Add(d);
            }
        }

        public async Task<string> ToggleSelectedDriveAsync()
        {
            if (SelectedDrive == null)
                return null;
            if (SelectedDrive.SystemDrive == "*SYSTEM*")
                return "You cannot make changes to the System drive!|Impossible!";
            if (SelectedDrive.Status == "*UNKNOWN*")
                return "You cannot make changes to an unknown boot sector type!|Impossible!";
            var (hidden, result) = _driveManager.ToggleBoot(SelectedDrive);
            if (result == 99)
            {
                if (hidden == true)
                {
                    LoadDrives();
                    return "Partition was hidden. You will only be able to access this partition with Wii USB loaders that support it. Be warned that Windows may ask if you want to format the drive when you insert it next time since it is hidden. The obvious answer to that is NO unless you want to lose the data on it.|Done";
                }
                else if (hidden == false)
                {
                    LoadDrives();
                    return "Partition was unhidden successfully. You can now access this partition from anywhere.|Done";
                }
            }
            else if (result == 1)
            {
                return "Unable to get handle on or read the drive.|Error";
            }
            else if (result == 2)
            {
                return "Unable to lock the device. Check that it is not in use and try again.|Device locked";
            }
            else if (result == 3)
            {
                return "On verify, it appears that nothing has changed. Somehow I was unable to toggle the boot sector.|Verify";
            }
            else
            {
                return "Unknown boot signature found on the drive, for safety's sake, nothing was done.|Hmmm...";
            }
            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

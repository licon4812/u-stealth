using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UStealth.WinUI
{
    [WinRT.GeneratedBindableCustomPropertyAttribute]
    public class DriveInfoModel : INotifyPropertyChanged
    {
        private string _systemDrive;
        private string _driveLetter;
        private string _interface;
        private string _model;
        private string _mediaType;
        private string _size;
        private string _status;
        private string _deviceID;

        public string SystemDrive
        {
            get => _systemDrive;
            set { _systemDrive = value; OnPropertyChanged(nameof(SystemDrive)); }
        }
        public string DriveLetter
        {
            get => _driveLetter;
            set { _driveLetter = value; OnPropertyChanged(nameof(DriveLetter)); }
        }
        public string Interface
        {
            get => _interface;
            set { _interface = value; OnPropertyChanged(nameof(Interface)); }
        }
        public string Model
        {
            get => _model;
            set { _model = value; OnPropertyChanged(nameof(Model)); }
        }
        public string MediaType
        {
            get => _mediaType;
            set { _mediaType = value; OnPropertyChanged(nameof(MediaType)); }
        }
        public string Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(nameof(Size)); }
        }
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }
        public string DeviceID
        {
            get => _deviceID;
            set { _deviceID = value; OnPropertyChanged(nameof(DeviceID)); }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
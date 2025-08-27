using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UStealth.WinUI
{
    [WinRT.GeneratedBindableCustomPropertyAttribute]
    public partial class DriveInfoModel : INotifyPropertyChanged
    {
        private bool _isSystemDrive;
        private string _driveLetter;
        private string _interface;
        private string _model;
        private string _mediaType;
        private string _size;
        private string _status;
        private string _deviceID;
        private string _volLabel;
        private string _format;

        public bool IsSystemDrive
        {
            get => _isSystemDrive;
            set { _isSystemDrive = value; OnPropertyChanged(nameof(IsSystemDrive)); }
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

        public string VolumeLabel 
        {
            get => _volLabel;
            set { _volLabel = value; OnPropertyChanged(nameof(VolumeLabel)); }
        }

        public string Format
        {
            get => _format;
            set { _format = value; OnPropertyChanged(nameof(Format)); }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
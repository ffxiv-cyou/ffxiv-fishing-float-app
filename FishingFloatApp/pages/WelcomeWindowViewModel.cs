using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FishingFloatApp
{
    class WelcomeWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void notifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        Config Config { get; }
        public WelcomeWindowViewModel(Config cfg)
        {
            Config = cfg;
        }
        public bool MemoryScanMode
        {
            get => Config.MemoryScanMode ?? false;
            set
            {
                Config.MemoryScanMode = value;
                Config.Save();
                notifyPropertyChanged();
                notifyPropertyChanged(nameof(PcapHint));
                notifyPropertyChanged(nameof(MemoryHint));
            }
        }

        public Visibility PcapHint => MemoryScanMode ? Visibility.Collapsed : Visibility.Visible;
        public Visibility MemoryHint => MemoryScanMode ? Visibility.Visible : Visibility.Collapsed;
    }
}

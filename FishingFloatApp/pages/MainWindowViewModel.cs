using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FishingFloatApp
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        Config Config { get; }
        public MainWindowViewModel(Config cfg) 
        { 
            Config = cfg;
            transparent = !cfg.FirstRun;
        }

        public string Title { get; set; } = "Fishing Float App";

        public event PropertyChangedEventHandler? PropertyChanged;

        public string BackgroundColor
        {
            get => transparent ? "Transparent" : "White";
        }

        bool transparent { get; set; } = true;
        public bool Transparent
        {
            get => transparent;
            set
            {
                transparent = value;
                notifyPropertyChanged();
                notifyPropertyChanged("BackgroundColor");
                notifyPropertyChanged(nameof(ShowingUri));
            }
        }

        public Uri ShowingUri
        {
            get
            {
                if (Config.FirstRun && !transparent)
                {
                    return new Uri(Config.BaseURL + "#/preview");
                }
                else
                {
                    return new Uri(Config.BaseURL);
                }
            }
        }

        public int Width
        {
            get => Config.Width;
            set
            {
                if (Config.Width == value)
                    return;

                Config.Width = value;
                notifyPropertyChanged();
            }
        }

        public int Height
        {
            get => Config.Height;
            set
            {
                if (Config.Height == value)
                    return;

                Config.Height = value;
                notifyPropertyChanged();
            }
        }

        public int Left
        {
            get => Config.Left;
            set
            {
                if (Config.Left == value)
                    return;

                Config.Left = value;
                notifyPropertyChanged();
            }
        }


        public int Top
        {
            get => Config.Top;
            set
            {
                if (Config.Top == value)
                    return;

                Config.Top = value;
                notifyPropertyChanged();
            }
        }
        

        void notifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            Config.Save();
        }
    }
}
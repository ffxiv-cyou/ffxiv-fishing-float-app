using FishingFloatApp.Overlay;
using System.Windows;

namespace FishingFloatApp.pages
{
    /// <summary>
    /// CheckUpdate.xaml 的交互逻辑
    /// </summary>
    public partial class CheckUpdate : Window
    {
        CheckUpdateViewModel ViewModel { get; }

        public CheckUpdate(CheckUpdateViewModel vm)
        {
            InitializeComponent();

            ViewModel = vm;
            DataContext = vm;
        }

        private void OpenDownloadUrl(object sender, RoutedEventArgs e)
        {
            OpenBrowserWorker.OpenUrl(ViewModel.URL);
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class CheckUpdateViewModel
    {
        public string CurrentVersion { get; }

        Updater.Release newVersion { get; }

        public string Version => newVersion.tag_name;

        public string URL => newVersion.html_url;

        public string Name => newVersion.name;

        public string Content => newVersion.body;


        public CheckUpdateViewModel(string currentVersion, Updater.Release newVersion) 
        { 
            CurrentVersion = currentVersion;
            this.newVersion = newVersion;
        }
    }
}

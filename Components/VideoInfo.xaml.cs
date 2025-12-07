using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Chapter_Splitter.Components
{
    /// <summary>
    /// Interaction logic for VideoInfo.xaml
    /// </summary>
    public partial class VideoInfo : UserControl
    {
        public VideoInfo()
        {
            InitializeComponent();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Just restart the application for simplicity, this avoid additional events
            var MainModule = Process.GetCurrentProcess().MainModule;

            if (MainModule == null)
                return;

            Process.Start(MainModule.FileName);
            Application.Current.Shutdown();
        }
    }
}

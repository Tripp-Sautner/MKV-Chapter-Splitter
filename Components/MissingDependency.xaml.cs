using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chapter_Splitter.Components
{
    /// <summary>
    /// Interaction logic for MissingDependency.xaml
    /// </summary>
    public partial class MissingDependency : UserControl
    {
        public MissingDependency()
        {
            InitializeComponent();
        }

        private void LinkClick_ffmpeg(object sender, RoutedEventArgs e)
        {
            OpenWebLink("https://www.ffmpeg.org/download.html#build-windows");
        }

        private void LinkClick_readme(object sender, MouseButtonEventArgs e)
        {
            OpenWebLink("https://github.com/Tripp-Sautner/MKV-Chapter-Splitter#mkv-chapter-splitter");
        }

        private static void OpenWebLink(string URL)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = URL,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
    }
}

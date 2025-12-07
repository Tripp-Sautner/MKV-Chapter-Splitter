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
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://www.ffmpeg.org/download.html#build-windows",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }

        private void LinkClick_readme(object sender, MouseButtonEventArgs e)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://www.google.com/",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
    }
}

using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Chapter_Splitter
{
    /// <summary>
    /// Interaction logic for TestingPage.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<(string chapterTitle, double startTimeSec, double endTimeSec, double secondsPerUnit)> chapters = [];
        List<(string EpisodeName, int StartChapter, int EndChapter)> episodes = [];
        private List<string> episodeNames = [];
        string SelectedMKVDirectory = "";
        string SelectedMKVFullPath = "";
        bool useNvenc = true;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += Page_Loaded;
            this.MissingDependencyControl.BrowseButton.Click += FolderBrowse_FFmpeg;
            this.FileDropControl.ChooseMkv.MouseUp += Click_MkvSelect;


            // File-Wide Control Events
            EpisodeEditorControl.EpisodeCount.LostFocus += ChapterEntry_LostFocus;
            EpisodeEditorControl.ChapterPrefixInput.LostFocus += ChapterEntry_LostFocus;
            EpisodeEditorControl.EpisodeNumber.LostFocus += ChapterEntry_LostFocus;
            EpisodeEditorControl.SeasonNumber.LostFocus += ChapterEntry_LostFocus;
            EpisodeEditorControl.EpisodeSelector.SelectionChanged += EpisodeSelector_SelectionChanged;
            EpisodeEditorControl.PrevEpisode.Click += PrevNextEpisode_Click;
            EpisodeEditorControl.NextEpisode.Click += PrevNextEpisode_Click;

            // Per-Episode Control Events
            EpisodeEditorControl.CopyChapterCount.Click += Click_Autofill;
            EpisodeEditorControl.ChapterStartIndex.LostFocus += ChapterIndex_LostFocus;
            EpisodeEditorControl.ChapterEndIndex.LostFocus += ChapterIndex_LostFocus;
            EpisodeEditorControl.SplitButton.Click += SplitButton_Click;

            // Custom Quality Control Events
            EpisodeEditorControl.rBNearPerfect.Click += Update_QualityControls;
            EpisodeEditorControl.rBHigh.Click += Update_QualityControls;
            EpisodeEditorControl.rBStandard.Click += Update_QualityControls;
            EpisodeEditorControl.rBCustom.Click += Update_QualityControls;
            EpisodeEditorControl.UseNvencCheck.Click += Click_QualityNvenc;
        }

        private void SplitButton_Click(object sender, RoutedEventArgs e)
        {
            //ffmpeg -i input.mkv - ss[START_TIME] - to[END_TIME] -c:v libx264 -crf 0 -preset veryslow -c:a copy output.mkv
            string plannedOutput = "";
            var FFMpegExe = $"{Properties.Settings.Default.FFmpegBinPath}\\ffmpeg.exe";

            string[] plannedCommands = new string[episodes.Count];
            var i = 0;
            foreach (var (EpisodeName, StartChapter, EndChapter) in episodes)
            {
                plannedOutput += $"\nEpisode: {EpisodeName}.mkv, \n" +
                    $"Chapters: {StartChapter} to {EndChapter}\n" +
                    $"Time {chapters[StartChapter - 1].startTimeSec} to {chapters[EndChapter - 1].endTimeSec}\n";

                // ffmpeg -ss [START_TIME] -i input.mkv -to [END_TIME] -c copy "output_cut_[N].mkv"
                var qualityValue = EpisodeEditorControl.customQuality.Text;
                plannedCommands[i++] =
                    $"{FFMpegExe} " +
                    $"-i \"{SelectedMKVFullPath}\" " +
                    $"-ss {chapters[StartChapter - 1].startTimeSec} " +
                    $"-to {chapters[EndChapter - 1].endTimeSec} " +
                    (useNvenc ? $"-c:v h264_nvenc -qp {qualityValue} " : $"-c:v libx264 -crf {qualityValue} -preset veryslow ") +
                    $"-c:a copy \"{System.IO.Path.Combine(SelectedMKVDirectory, EpisodeName)}.mkv\"";
            }
            Console.WriteLine(plannedCommands);

            var Output = MessageBox.Show($"Turning into episodes:\n{plannedOutput}", "Planned Output", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (Output == MessageBoxResult.Yes)
            {
                foreach (var commandToRun in plannedCommands)
                {
                    //var commandToRun = plannedCommands[1];
                    // Move this to CLI tools later
                    ProcessStartInfo startInfo = new()
                    {
                        FileName = "cmd.exe", // Or "powershell.exe" for PowerShell
                        Arguments = $"/K {commandToRun}",
                        UseShellExecute = true, // Essential for showing the window
                        CreateNoWindow = false, // TODO probably make the windows hide and have an actual loading bar.
                        WindowStyle = ProcessWindowStyle.Normal // Ensure the window is visible
                    };

                    try
                    {
                        // Start the process, which opens the new CLI window.
                        Process.Start(startInfo);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred: {ex.Message}");
                    }
                }
            }
            else return;

            Output = MessageBox.Show($"The application is running.\nDo you want to open the output directory?", "Finished Running Commands", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (Output == MessageBoxResult.Yes)
                Process.Start("explorer.exe", (string)SelectedMKVDirectory);

        }

        private void EpisodeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check bounds for prev/next buttons
            if (EpisodeEditorControl.EpisodeSelector.SelectedIndex ==
            EpisodeEditorControl.EpisodeSelector.Items.Count - 1)
                EpisodeEditorControl.NextEpisode.IsEnabled = false;
            else
                EpisodeEditorControl.NextEpisode.IsEnabled = true;

            if (EpisodeEditorControl.EpisodeSelector.SelectedIndex == 0)
                EpisodeEditorControl.PrevEpisode.IsEnabled = false;
            else
                EpisodeEditorControl.PrevEpisode.IsEnabled = true;

            UpdateChapterIndices();
        }

        private void ChapterIndex_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (int.Parse(EpisodeEditorControl.ChapterStartIndex.Text) < 0)
                    throw new Exception("Chapter start index cannot be less than 0.");
                else if (int.Parse(EpisodeEditorControl.ChapterEndIndex.Text) > chapters.Count)
                    throw new Exception("Chapter end index cannot be greater than total chapter count.");
                else
                    episodes[EpisodeEditorControl.EpisodeSelector.SelectedIndex] =
                        (episodes[EpisodeEditorControl.EpisodeSelector.SelectedIndex].EpisodeName,
                        int.Parse(EpisodeEditorControl.ChapterStartIndex.Text),
                        int.Parse(EpisodeEditorControl.ChapterEndIndex.Text));
            }
            catch (Exception)
            {
                MessageBox.Show("Invalid chapter index entered. Please ensure the start index is >= 0 and the end index is <= total chapter count.",
                    "Invalid Chapter Index", MessageBoxButton.OK, MessageBoxImage.Error);
                ((TextBox)sender).Text = "0";
            }

            UpdateChapterIndices();
        }

        private void UpdateChapterIndices()
        {
            // Update Chapter Indices
            if (EpisodeEditorControl.EpisodeSelector.SelectedIndex == -1)
                return;

            var (_, StartChapter, EndChapter) = episodes[EpisodeEditorControl.EpisodeSelector.SelectedIndex];
            EpisodeEditorControl.ChapterStartIndex.Text = StartChapter.ToString();
            EpisodeEditorControl.ChapterEndIndex.Text = EndChapter.ToString();
            EpisodeEditorControl.ChapterCount.Content =
                (EndChapter - StartChapter + 1).ToString();

            // Calculate Duration
            double totalDurationSec = 0;
            for (int i = StartChapter - 1; i < EndChapter; i++)
                totalDurationSec += (chapters[i].endTimeSec - chapters[i].startTimeSec);

            TimeSpan timeTotal = TimeSpan.FromSeconds(totalDurationSec);

            EpisodeEditorControl.EpisodeLength.Content =
                $"{(long)timeTotal.TotalHours:D2}:{timeTotal.Minutes:D2}:{timeTotal.Seconds:D2}";
        }

        private void Click_Autofill(object sender, RoutedEventArgs e)
        {
            // This will look at the active episode and base truth off of that.
            // Meaning the chapter count will be applied from start and end index 
            // of the selected episode to all other episodes in sequence.
            //
            // Warning will throw when potentially out of range.

            var (_, StartChapter, EndChapter) = episodes[EpisodeEditorControl.EpisodeSelector.SelectedIndex];
            int chapterCount = EndChapter - StartChapter + 1;

            var autoEpisodeList = new List<(string EpisodeName, int StartChapter, int EndChapter)>(episodes);
            for (int i = EpisodeEditorControl.EpisodeSelector.SelectedIndex + 1;
                i < autoEpisodeList.Count; i++)
            {
                autoEpisodeList[i] = (autoEpisodeList[i].EpisodeName,
                    autoEpisodeList[i - 1].EndChapter + 1,
                    autoEpisodeList[i - 1].EndChapter + chapterCount);
            }
            for (int i = EpisodeEditorControl.EpisodeSelector.SelectedIndex - 1;
                i >= 0; i--)
            {
                autoEpisodeList[i] = (autoEpisodeList[i].EpisodeName,
                    autoEpisodeList[i + 1].StartChapter - chapterCount,
                    autoEpisodeList[i + 1].StartChapter - 1);
            }

            // Validate Ranges
            foreach (var episode in autoEpisodeList)
            {
                if (episode.StartChapter < 1 || episode.EndChapter > chapters.Count)
                {
                    MessageBox.Show("Auto-fill operation would result in out-of-bounds chapter indices. Operation aborted.",
                        "Auto-Fill Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Apply Changes
            episodes = autoEpisodeList;
        }

        private void PrevNextEpisode_Click(object sender, RoutedEventArgs e)
        {
            string Name = ((Button)sender).Name;
            if (Name == nameof(EpisodeEditorControl.NextEpisode))
            {
                EpisodeEditorControl.EpisodeSelector.SelectedIndex += 1;
            }
            else if (Name == nameof(EpisodeEditorControl.PrevEpisode))
            {
                EpisodeEditorControl.EpisodeSelector.SelectedIndex -= 1;
            }
        }

        private void ChapterEntry_LostFocus(object sender, RoutedEventArgs e)
        {
            // Episode Count Changed
            string Name = ((TextBox)sender).Name;
            if (Name == nameof(EpisodeEditorControl.EpisodeCount))
            {

                var newCount = int.Parse(EpisodeEditorControl.EpisodeCount.Text);
                if (episodeNames.Count == newCount)
                    return;

                if (episodeNames.Count > newCount)
                {
                    episodeNames.RemoveRange(newCount, (episodeNames.Count - newCount));
                    episodes.RemoveRange(newCount, (episodeNames.Count - newCount));
                    EpisodeEditorControl.EpisodeSelector.Items.Refresh();
                }

                if (episodeNames.Count < newCount)
                {
                    // Setup Episode Name Prefix
                    int currIndexCap = newCount - episodeNames.Count + 1;
                    for (int i = 1; i < currIndexCap; i++)
                    {
                        string currEpisodeNumber = $"{int.Parse(EpisodeEditorControl.EpisodeNumber.Text) + i}";
                        string newEpisodeName = GetEpisodeName(EpisodeEditorControl.SeasonNumber.Text, currEpisodeNumber);
                        episodes.Add((newEpisodeName, 1, chapters.Count));
                        episodeNames.Add(newEpisodeName);
                    }
                }
            }
            // Episode Name Scheme Changed
            if (Name == nameof(EpisodeEditorControl.ChapterPrefixInput) ||
                Name == nameof(EpisodeEditorControl.SeasonNumber) ||
                Name == nameof(EpisodeEditorControl.EpisodeNumber))
            {

                var temp = new List<string>(episodeNames);
                for (int i = 0; i < temp.Count; i++)
                {
                    string currEpisodeNumber = $"{int.Parse(EpisodeEditorControl.EpisodeNumber.Text) + i}";
                    string newEpisodeName = GetEpisodeName(EpisodeEditorControl.SeasonNumber.Text, currEpisodeNumber);
                    episodes[i] = (newEpisodeName, episodes[i].StartChapter, episodes[i].EndChapter);
                    episodeNames[i] = newEpisodeName;
                }
            }

            var lastSelected = EpisodeEditorControl.EpisodeSelector.SelectedIndex;
            EpisodeEditorControl.EpisodeSelector.Items.Refresh();
            EpisodeEditorControl.EpisodeSelector.SelectedIndex = -1;
            EpisodeEditorControl.EpisodeSelector.SelectedIndex = lastSelected;

            //// Episode Chapter Range Changed
            //if (Name == nameof(EpisodeChapterStart) || Name == nameof(EpisodeChapterEnd))
            //{
            //    var temp = episodes[EpisodeSelector.SelectedIndex];
            //    episodes[EpisodeSelector.SelectedIndex] =
            //        (temp.EpisodeName, int.Parse(EpisodeChapterStart.Text), int.Parse(EpisodeChapterEnd.Text));
            //}
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!HelperFunctions.CheckFFmpegAvailability(Properties.Settings.Default.FFmpegBinPath))
                MissingDependencyControl.Visibility = Visibility.Visible;
        }

        private void FolderBrowse_FFmpeg(object _sender, EventArgs _eventArgs)
        {
            var ffmpegFolderDialog = new OpenFolderDialog()
            {
                Title = "Select the folder containing ffmpeg.exe, ffprobe.exe, and ffplay.exe",
            };

            bool? result = ffmpegFolderDialog.ShowDialog();

            if (result == true)
            {
                this.MissingDependencyControl.FFmpegPathInput.Text = ffmpegFolderDialog.FolderName;
            }

            // Now run checks
            if (HelperFunctions.CheckFFmpegAvailability(ffmpegFolderDialog.FolderName))
            {
                // If checks pass
                Properties.Settings.Default.FFmpegBinPath = ffmpegFolderDialog.FolderName;
                Properties.Settings.Default.Save();
                this.MissingDependencyControl.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Show("Could not find ffmpeg.exe, ffprobe.exe, and ffplay.exe in this folder." +
                    "Please select another folder.",
                    "Missing Items", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Click_ClearSettings(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();
            MessageBox.Show("Settings have been cleared. Application will now restart.",
                "Settings Reset", MessageBoxButton.OK, MessageBoxImage.Information);
            var MainModule = Process.GetCurrentProcess().MainModule;

            if (MainModule == null)
                return;

            Process.Start(MainModule.FileName);
            Application.Current.Shutdown();
        }

        private void Click_MkvSelect(object sender, RoutedEventArgs e)
        {
            var MkvDialog = new OpenFileDialog()
            {
                Filter = "MKV Files (*.mkv)|*.mkv",
                Title = "Select an MKV File",
            };
            if (MkvDialog.ShowDialog() != true)
                return;

            string SelectedMkvFile = MkvDialog.FileName;
            if (string.IsNullOrEmpty(SelectedMkvFile))
                return;

            if (!HelperFunctions.CheckIfMkvFile(SelectedMkvFile))
                return;

            // Get Chapters, Length, and overall info
            Setup_VideoInfo(MkvDialog.FileName);

            // Setup the Episode Editor
            Setup_VideoTools(MkvDialog.FileName);

            SelectedMKVDirectory = System.IO.Path.GetDirectoryName(SelectedMkvFile);
            SelectedMKVFullPath = SelectedMkvFile;
        }

        private void Setup_VideoInfo(string MkvFilePath)
        {
            // Transition from File Drop to Video Info Control
            FileDropControl.Visibility = Visibility.Hidden;
            VideoInfoControl.Visibility = Visibility.Visible;

            var Title = System.IO.Path.GetFileName(MkvFilePath);
            var VideoLength = "..."; // ffProbe TBD
            var VideoChapters = "..."; // ffProbe TBD

            // ffprobe check (Length and Chapters)
            var FFProbeExe = $"{Properties.Settings.Default.FFmpegBinPath}\\ffprobe.exe";
            var FFMpegExe = $"{Properties.Settings.Default.FFmpegBinPath}\\ffmpeg.exe";

            // Check Length
            string lengthCommand = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{MkvFilePath}\"";
            TimeSpan timeTotal = TimeSpan.FromSeconds(double.Parse(CliTools.RunAndReturnOutput(FFProbeExe, lengthCommand)));
            VideoLength = $"{(long)timeTotal.TotalHours:D2}:{timeTotal.Minutes:D2}:{timeTotal.Seconds:D2}";

            // Check Chapters
            // Generate the ffmpeg meta data to identify chapters into a file
            string chapterCommand = $"-i \"{MkvFilePath}\" -f ffmetadata -"; // Redirects info to null, just get chaps
            string generatedCommand = $"\"{FFMpegExe}\" {chapterCommand}";

            // Parse the generated chapter file and store the meta data
            string output = CliTools.RunAndReturnOutput(FFMpegExe, chapterCommand);
            string[] chapterData = output.Split('\n');

            chapters = [];

            for (int i = 0; i < chapterData.Length; i++)
            {
                if (chapterData[i].StartsWith("[CHAPTER]"))
                {
                    // convert timestamp to timespan given the time base.
                    // 1/1000000000
                    string timeBase = chapterData[i + 1].Replace("TIMEBASE=", "");
                    string[] parts = timeBase.Split("/");
                    double secondsPerUnit = double.Parse(parts[0]) / long.Parse(parts[1]);
                    double startTime = Math.Round((long.Parse(chapterData[i + 2].Replace("START=", "")) * secondsPerUnit), 4);
                    double endTime = Math.Round((long.Parse(chapterData[i + 3].Replace("END=", "")) * secondsPerUnit), 4);
                    string title = chapterData[i + 4].Replace("title=", "");

                    chapters.Add(new(title, startTime, endTime, secondsPerUnit));
                    i += 4; // Skip to next chapter entry
                }
            }
            VideoChapters = $"{chapters.Count}";

            VideoInfoControl.TitleBlock.Text = Title;
            VideoInfoControl.LengthBlock.Text = VideoLength;
            VideoInfoControl.ChapterBlock.Text = VideoChapters;

            // Open folder containing the MKV file
            VideoInfoControl.FolderButton.Click += (s, e) =>
            {
                var folderPath = System.IO.Path.GetDirectoryName(MkvFilePath);
                if (System.IO.Directory.Exists(folderPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", folderPath);
                }
            };

            // Open the MKV file
            VideoInfoControl.FileButton.Click += (s, e) =>
            {
                if (System.IO.File.Exists(MkvFilePath))
                {
                    ProcessStartInfo psi = new()
                    {
                        FileName = MkvFilePath,
                        UseShellExecute = true // This is crucial for using default apps in modern .NET
                    };
                    Process.Start(psi);
                }
            };

            // Setup the Quality Controls
            Update_QualityControls(null, null);
        }

        private void Setup_VideoTools(string MkvFilePath)
        {
            // Setup Episode Name Prefix
            var Title = System.IO.Path.GetFileName(MkvFilePath);
            EpisodeEditorControl.ChapterPrefixInput.Text =
                System.IO.Path.GetFileNameWithoutExtension((string)Title);


            EpisodeEditorControl.ChapterModifiers.IsEnabled = true;

            string EpisodeName = GetEpisodeName(
                EpisodeEditorControl.SeasonNumber.Text,
                EpisodeEditorControl.EpisodeNumber.Text);
            EpisodeEditorControl.EpisodeSelector.Text = $"{EpisodeName}";

            episodes.Add((EpisodeName, 1, chapters.Count));
            episodeNames.Add(EpisodeName);

            EpisodeEditorControl.EpisodeSelector.ItemsSource = episodeNames;
            EpisodeEditorControl.EpisodeSelector.SelectedIndex = 0;
        }

        private string GetEpisodeName(string SeasonNumber, string EpisodeNumber)
        {
            string prefix = EpisodeEditorControl.ChapterPrefixInput.Text;
            string episodeName = $"{prefix} S{SeasonNumber.PadLeft(2, '0')}E{EpisodeNumber.PadLeft(2, '0')}";
            return episodeName;
        }

        private void Update_QualityControls(object? sender, RoutedEventArgs? e)
        {
            var NearPerfect_DefaultCRFValue = "14";
            var NearPerfect_DefaultQPValue = "15";

            var High_DefaultCRFValue = "18";
            var High_DefaultQPValue = "20";

            var Standard_DefaultCRFValue = "22";
            var Standard_DefaultQPValue = "25";

            if (EpisodeEditorControl.rBNearPerfect.IsChecked == true)
            {
                EpisodeEditorControl.customQuality.Text = useNvenc ? NearPerfect_DefaultQPValue : NearPerfect_DefaultCRFValue;
            }
            else if (EpisodeEditorControl.rBHigh.IsChecked == true)
            {
                EpisodeEditorControl.customQuality.Text = useNvenc ? High_DefaultQPValue : High_DefaultCRFValue;
            }
            else if (EpisodeEditorControl.rBStandard.IsChecked == true)
            {
                EpisodeEditorControl.customQuality.Text = useNvenc ? Standard_DefaultQPValue : Standard_DefaultCRFValue;
            }
            EpisodeEditorControl.customQuality.IsEnabled = EpisodeEditorControl.rBCustom.IsChecked == true;
        }

        private void Click_QualityNvenc(object sender, RoutedEventArgs e)
        {
            useNvenc = EpisodeEditorControl.UseNvencCheck.IsChecked == true;
            Update_QualityControls(sender, e);
        }
    }
}


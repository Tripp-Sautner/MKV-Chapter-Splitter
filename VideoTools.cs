namespace Chapter_Splitter
{
    static class HelperFunctions
    {
        public static bool CheckFFmpegAvailability(string ffmpegPath)
        {
            if (!System.IO.Directory.Exists(ffmpegPath))
                return false;

            // TODO add version checks, etc.

            var files = System.IO.Directory.GetFiles(ffmpegPath);

            bool ffmpeg = false, ffprobe = false, ffplay = false;

            foreach (var item in files)
            {
                if (item.EndsWith("ffmpeg.exe"))
                    ffmpeg = true;
                if (item.EndsWith("ffprobe.exe"))
                    ffprobe = true;
                if (item.EndsWith("ffplay.exe"))
                    ffplay = true;
            }

            return ffmpeg && ffprobe && ffplay;
        }
        public static bool CheckIfMkvFile(string filePath)
        {
            return System.IO.Path.Exists(filePath) &&
                filePath.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase);
        }
    }

    class VideoTools(string videoFilePath)
    {
        private string VideoFilePath { get; } = videoFilePath;
    }
}

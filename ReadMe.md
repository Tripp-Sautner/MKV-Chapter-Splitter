# MKV Chapter Splitter

0. Select FFMPEG "bin" path, (this will need to be downloaded and unzipped first)
1. Select the MKV
2. Update Episodes in File Count
3. Update the File Name Prefix
4. Update Season and Episode Start Number (If applicable)
5. (Ensure you are on the first episode) Update the Chapter Start and End indexes
6. Click Autofill From Here to take the Episode Count and auto-fill the rest of the chapters with the same number
7. Click Split Into episodes to start the process

# Requirements
- FFMPEG https://www.ffmpeg.org/download.html

# Notes
- This will open a ffmpeg per episode to split
- After all is done you will see the terminal sitting there with nothing running, you can then close it

# Re-Encoding Notes
Re-Encoding is required since most MKV files will not "split" at the desired timestamp due to how these files are saved. They split on keystone frames which can be 1-2+ seconds ahead or behind your split time. The fix is to re-encode exactly to your desired timestamp, this creates a new issue of data size. Files will balloon in size if the encoding is lossless so there are a few options.

| Quality Level | CPU `-crf` | NVENC `-qp` | Desc |
|-|-|-|-|
|Lossless|0|0|Causes file sizes to expand 4.5 times...|
|Near Perfect|14|15|Near-perfect.|
|High|18|20|Rare artifacts.|
|Standard|22|25|Occasional visual compression.|

- Generally lower = better, but lower = bigger file size


# Workflow
```mermaid
---
config:
      theme: redux
---
flowchart TD
        A(["Multi-Epsode Video (MakeMkv etc..)"])
        A --> B{"MKV Chapter Splitter"}
        G(["FFMPEG (path to ffprobe/ffmpeg)"])
        G --> B
        B --> C["Split Video Into Segments"]
        B --> D["Segments per Episode"]
        B --> E["Name of Show, Season, Start Episode"]
        C --> F
        D --> F
        E --> F["Final Episodes Organized"]
```

# Whats happening

1. Given the names the user wants this runs the following ffprobe command to get chapter data
`ffprobe -i \"{MkvFilePath}\" -f ffmetadata -"
- This gets all the chapter data and reads it into the system
2. With the chapter data we just need the start and end time of each episode, this comes from the UI
- The user selects Chapters and this gives us the time stamps
3. Now with the timestamps we run the following command
```c#
                plannedCommands[i++] =
                    $"{FFMpegExe} " +
                    $"-i \"{SelectedMKVFullPath}\" " +
                    $"-ss {chapters[episode.StartChapter - 1].startTimeSec} " +
                    $"-to {chapters[episode.EndChapter - 1].endTimeSec} " +
                    (useNvenc ? $"-c:v h264_nvenc -qp 0 -crf 0 " : "-c:v libx264 -crf 0 -preset veryslow") +
                    $"-c:a copy \"{System.IO.Path.Combine(SelectedMKVDirectory, episode.EpisodeName)}.mkv\"";

```
- Depending if the user has "Settings > Use Nvenc" enabled we either use h264_nvenc or libx264 to reencode each episode
4. This Generates a CLI for each episode made 

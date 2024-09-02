using Microsoft.Extensions.Logging;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace EARDA
{
    public static class FileManager
    {
        public static readonly string Path = $"{AppDomain.CurrentDomain.BaseDirectory}Data";

        public struct Video
        {
            public string Title;
            public string Uploader;
            public string Url;
            public string Path;

            public ulong id;
        }

        public static async Task<Video?> DownloadVideo(ulong id, string url)
        {
            YoutubeDL ytdlp = new()
            {
                OutputFolder = Path,
                OutputFileTemplate = $"{id}.%(ext)s",
            };

            RunResult<VideoData> result = await ytdlp.RunVideoDataFetch(url);

            VideoData video = result.Data;

            string title = video.Title;
            string uploader = video.Uploader;

            double downloaedProgress = 0.0f;
            double lastLoggedProgress = 0.0f;

            Progress<DownloadProgress> progress = new(p =>
            {
                downloaedProgress = p.Progress;

                if (Math.Floor(downloaedProgress * 10) / 10 >= lastLoggedProgress + 0.1)
                {
                    lastLoggedProgress = Math.Floor(downloaedProgress * 10) / 10;

                    Program.WriteLog(LogLevel.Information, $"Downloading '{title}'... Progress: {downloaedProgress:P1}", new EventId(201, "File Manager"));
                }
            });

            OptionSet options = new()
            {
                Format = "bestvideo[width>=420][ext=webm][filesize_approx<15MiB]+bestaudio[ext=webm][filesize_approx<10MiB]/bestvideo[width>=420][ext=mp4][filesize_approx<15MiB]+bestaudio[ext=m4a][filesize_approx<10MiB]",
                NoContinue = true,
            };

            RunResult<string> downloadResult;

            try
            {
                downloadResult = await ytdlp.RunVideoDownload(url, progress: progress, overrideOptions: options);
            }
            catch (Exception ex)
            {
                Program.WriteLog(LogLevel.Warning, ex.Message, new EventId(201, "File Manager"));

                return null;
            }

            string path = downloadResult.Data;

            return new Video
            {
                Title = title,
                Uploader = uploader,
                Url = url,
                Path = path,
                id = id
            };
        }

        public static async Task DeleteVideo(string path)
        {
            await Task.Run(() =>
            {
                FileInfo fileInfo = new(path);

                try
                {
                    fileInfo.Delete();
                }
                catch (Exception e)
                {
                    Program.WriteLog(LogLevel.Error, $"{e.Message}", new EventId(201, "File Manager"));
                }
            });
        }

        public static bool FileSizeCheck(FileInfo fileInfo)
        {
            long fileSizeInBytes = fileInfo.Length;
            long fileSizeInMegabytes = fileSizeInBytes / (1024 * 1024);

            if (fileSizeInMegabytes < 25) // 25 MB in bytes
            {
                //The file is under 25 MB.
                return true;
            }
            else
            {
                //The file is 25 MB or larger.
                return false;
            }
        }
    }
}
